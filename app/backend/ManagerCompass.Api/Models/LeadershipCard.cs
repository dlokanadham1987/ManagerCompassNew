namespace ManagerCompass.Api.Models;

public class LeadershipItem
{
    public string Text { get; set; } = "";
    public string? Source { get; set; }
    public string Color { get; set; } = "";
}

/// <summary>Evidence-linked, "baseball-card"-style operating-review summary for one team.</summary>
public class LeadershipCard
{
    public string TeamName { get; set; } = "";
    public string Manager { get; set; } = "";
    public string ReviewedWith { get; set; } = "";
    public string Period { get; set; } = "";
    public string CardNumber { get; set; } = "";
    /// <summary>Computed from RisksFlagged and the Engagement stat below — not a fixed label.</summary>
    public string PulseLabel { get; set; } = "";
    public string PulseColor { get; set; } = "";
    public string PulseNote { get; set; } = "";
    public List<SnapshotKpi> Stats { get; set; } = new();
    public List<LeadershipItem> RisksFlagged { get; set; } = new();
    public List<LeadershipItem> ActionsTaken { get; set; } = new();
    public List<LeadershipItem> NextSteps { get; set; } = new();
    public string EvidenceNote { get; set; } = "";
    public string HumanReviewNote { get; set; } = "";
}
