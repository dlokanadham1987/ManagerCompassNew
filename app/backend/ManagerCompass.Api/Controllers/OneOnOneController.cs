using System.Globalization;
using System.Text;
using ManagerCompass.Api.Models;
using ManagerCompass.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ManagerCompass.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OneOnOneController : ControllerBase
{
    private const int MaxEntries = 150;
    private const int MaxRangeDays = 366;
    private const int MaxTextLength = 2000;

    private const string SystemPrompt = """
        You help a people manager prepare for their next 1:1 meeting with one direct report.
        You are given that person's notes for a stated period (it may be a couple of weeks or up to a year): post-meeting notes, action items, and the manager's own comments.
        Write a short summary using ONLY the material provided — do not invent facts. For longer periods, group themes over time and mention how things changed.
        Format: 1) "Themes" — 2-4 bullets; 2) "Open items" — unfinished action items, if any; 3) "Suggested talking points" — 2-3 bullets.
        Keep it under 220 words, in plain language.
        You only summarize. Never recommend or imply a decision about pay, performance ratings, promotion, discipline, or employment status;
        if the notes touch those topics, say only that the manager should take it to their HR business partner.
        """;

    private readonly IAiClient _claude;
    private readonly ITeamNotesService _teamNotes;

    private readonly ILogger<OneOnOneController> _logger;

    public OneOnOneController(IAiClient claude, ITeamNotesService teamNotes, ILogger<OneOnOneController> logger)
    {
        _claude = claude;
        _teamNotes = teamNotes;
        _logger = logger;
    }

    [HttpPost("summarize")]
    public async Task<IActionResult> Summarize([FromBody] OneOnOneSummaryRequest request, CancellationToken ct)
    {
        if (!TryResolveRange(request, out var startDay, out var endDay))
            return BadRequest($"Choose a valid date range (start on or before end, at most {MaxRangeDays} days).");

        var start = startDay.ToDateTime(TimeOnly.MinValue);
        var end = endDay.ToDateTime(TimeOnly.MaxValue);

        // When a long range has more entries than we send, keep the most recent ones.
        var notes = request.PostNotes
            .Where(n => TryParseDate(n.Date, out var d) && d >= start && d <= end)
            .OrderByDescending(n => n.Date).Take(MaxEntries).Reverse()
            .ToList();
        var actions = request.ActionItems
            .Where(a => TryParseDate(a.MeetingDate, out var d) && d >= start && d <= end)
            .OrderByDescending(a => a.MeetingDate).Take(MaxEntries).Reverse()
            .ToList();

        var memberId = _teamNotes.GetTeams()
            .SelectMany(t => t.Members)
            .FirstOrDefault(m => string.Equals(m.Name, request.ReportName.Trim(), StringComparison.OrdinalIgnoreCase))?.Id;
        var endUtc = end.ToUniversalTime();
        var comments = memberId is null
            ? new List<TeamComment>()
            : _teamNotes.GetComments("member", memberId, start.ToUniversalTime())
                .Where(c => c.CreatedAt <= endUtc)
                .Take(MaxEntries)
                .OrderBy(c => c.UpdatedAt)
                .ToList();

        var response = new OneOnOneSummaryResponse
        {
            PeriodStart = startDay.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            PeriodEnd = endDay.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
        };

        if (notes.Count == 0 && actions.Count == 0 && comments.Count == 0)
        {
            response.Summary = $"No notes, action items, or comments for {request.ReportName} between {response.PeriodStart} and {response.PeriodEnd}.";
            return Ok(response);
        }

        var prompt = new StringBuilder();
        prompt.AppendLine($"Direct report: {request.ReportName}");
        prompt.AppendLine($"Period: {response.PeriodStart} to {response.PeriodEnd}");
        prompt.AppendLine().AppendLine("Post-meeting notes:");
        foreach (var n in notes) prompt.AppendLine($"- [{n.Date}] {Trim(n.Text)}");
        if (notes.Count == 0) prompt.AppendLine("- (none)");
        prompt.AppendLine().AppendLine("Action items:");
        foreach (var a in actions) prompt.AppendLine($"- [{(a.Done ? "done" : "open")}] {Trim(a.Text)}");
        if (actions.Count == 0) prompt.AppendLine("- (none)");
        prompt.AppendLine().AppendLine("Manager comments:");
        foreach (var c in comments) prompt.AppendLine($"- [{c.UpdatedAt:yyyy-MM-dd}] {Trim(c.Title)}: {Trim(c.Description)}");
        if (comments.Count == 0) prompt.AppendLine("- (none)");

        try
        {
            response.Summary = (await _claude.CompleteAsync(SystemPrompt, prompt.ToString(), 500, ct)).Trim();
            response.UsedModel = true;
            return Ok(response);
        }
        catch (AiNotConfiguredException)
        {
            return StatusCode(503, "AI is not configured: add ANTHROPIC_API_KEY or OPENAI_API_KEY to the API's .env file and restart the API.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "AI summary request failed");
            return StatusCode(502, "The AI service could not be reached. Please try again.");
        }
    }

    // Uses From/To when both are given, else the last Days days ending today.
    private static bool TryResolveRange(OneOnOneSummaryRequest request, out DateOnly start, out DateOnly end)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        if (DateOnly.TryParse(request.From, CultureInfo.InvariantCulture, out start) &&
            DateOnly.TryParse(request.To, CultureInfo.InvariantCulture, out end))
            return start <= end && end.DayNumber - start.DayNumber <= MaxRangeDays;

        end = today;
        start = today.AddDays(-Math.Clamp(request.Days, 1, MaxRangeDays));
        return true;
    }

    private static bool TryParseDate(string value, out DateTime date) =>
        DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    private static string Trim(string text) =>
        text.Length <= MaxTextLength ? text : text[..MaxTextLength];
}
