namespace ManagerCompass.Api.Models;

public class SummaryPostNote
{
    public string Date { get; set; } = ""; // yyyy-MM-dd
    public string Text { get; set; } = "";
}

public class SummaryActionItem
{
    public string Text { get; set; } = "";
    public bool Done { get; set; }
    public string MeetingDate { get; set; } = ""; // the meeting's startsAt, used as the item's timestamp
}

/// <summary>
/// 1:1 notes live in the browser's localStorage, so the client sends them here; the server applies the
/// time window and adds Team Notes comments before anything goes to the model.
/// </summary>
public class OneOnOneSummaryRequest
{
    public string ReportName { get; set; } = "";
    /// <summary>Inclusive date range, yyyy-MM-dd. If either is missing, the last <see cref="Days"/> days are used.</summary>
    public string? From { get; set; }
    public string? To { get; set; }
    public int Days { get; set; } = 14;
    public List<SummaryPostNote> PostNotes { get; set; } = new();
    public List<SummaryActionItem> ActionItems { get; set; } = new();
}

public class OneOnOneSummaryResponse
{
    public string Summary { get; set; } = "";
    public string PeriodStart { get; set; } = ""; // yyyy-MM-dd
    public string PeriodEnd { get; set; } = "";
    public bool UsedModel { get; set; }
}
