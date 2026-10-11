using System.Text.Json;
using ManagerCompass.Api.Models;

namespace ManagerCompass.Api.Services;

public interface ITeamNotesService
{
    IReadOnlyList<Team> GetTeams();
    IReadOnlyList<TeamComment> GetComments(string? targetType, string? targetId, DateTime? since);
    TeamComment? Create(TeamCommentInput input);
    TeamComment? Update(string id, TeamCommentInput input);
    bool Delete(string id);
}

/// <summary>
/// Teams are hard-coded for now (same three people seeded in the 1:1 tab); comments persist to a
/// JSON file under App_Data. Swap this for a database-backed implementation later without touching the controller.
/// </summary>
public class TeamNotesService : ITeamNotesService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private static readonly IReadOnlyList<Team> Teams = new List<Team>
    {
        new()
        {
            Id = "mc07",
            Name = "MC-07 Team",
            Members = new List<TeamMember>
            {
                new() { Id = "saritha", Name = "Saritha" },
                new() { Id = "rishika", Name = "Rishika" },
                new() { Id = "srija", Name = "Srija" }
            }
        },
        new()
        {
            Id = "supertechies",
            Name = "SuperTechies Team",
            Members = new List<TeamMember>
            {
                new() { Id = "john", Name = "John" },
                new() { Id = "bob", Name = "Bob" }
            }
        }
    };

    private readonly string _filePath;
    private readonly object _lock = new();
    private List<TeamComment> _comments;

    public TeamNotesService(IWebHostEnvironment env)
    {
        var dir = Path.Combine(env.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "team-comments.json");
        _comments = Load();
    }

    public IReadOnlyList<Team> GetTeams() => Teams;

    public IReadOnlyList<TeamComment> GetComments(string? targetType, string? targetId, DateTime? since)
    {
        lock (_lock)
        {
            return _comments
                .Where(c => string.IsNullOrEmpty(targetType) || c.TargetType == targetType)
                .Where(c => string.IsNullOrEmpty(targetId) || c.TargetId == targetId)
                .Where(c => since is null || c.CreatedAt >= since || c.UpdatedAt >= since)
                .OrderByDescending(c => c.UpdatedAt)
                .ToList();
        }
    }

    public TeamComment? Create(TeamCommentInput input)
    {
        if (!IsValidTarget(input)) return null;
        var now = DateTime.UtcNow;
        var comment = new TeamComment
        {
            Id = Guid.NewGuid().ToString("N"),
            TargetType = input.TargetType,
            TargetId = input.TargetId,
            Title = input.Title.Trim(),
            Description = input.Description.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };
        lock (_lock)
        {
            _comments.Add(comment);
            Save();
        }
        return comment;
    }

    public TeamComment? Update(string id, TeamCommentInput input)
    {
        lock (_lock)
        {
            var existing = _comments.FirstOrDefault(c => c.Id == id);
            if (existing is null) return null;
            existing.Title = input.Title.Trim();
            existing.Description = input.Description.Trim();
            existing.UpdatedAt = DateTime.UtcNow;
            Save();
            return existing;
        }
    }

    public bool Delete(string id)
    {
        lock (_lock)
        {
            var removed = _comments.RemoveAll(c => c.Id == id) > 0;
            if (removed) Save();
            return removed;
        }
    }

    private static bool IsValidTarget(TeamCommentInput input) =>
        input.TargetType switch
        {
            "team" => Teams.Any(t => t.Id == input.TargetId),
            "member" => Teams.Any(t => t.Members.Any(m => m.Id == input.TargetId)),
            _ => false
        };

    private List<TeamComment> Load()
    {
        try
        {
            if (File.Exists(_filePath))
                return JsonSerializer.Deserialize<List<TeamComment>>(File.ReadAllText(_filePath)) ?? new();
        }
        catch (JsonException)
        {
            // corrupt file — start empty rather than failing API startup
        }
        return new();
    }

    private void Save() => File.WriteAllText(_filePath, JsonSerializer.Serialize(_comments, JsonOptions));
}
