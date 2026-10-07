namespace ManagerCompass.Api.Models;

public class SnapshotKpi
{
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";
    public string SubText { get; set; } = "";
    public string Color { get; set; } = "";
}

public class AttritionReason
{
    public string Label { get; set; } = "";
    public double Percent { get; set; }
    public string Color { get; set; } = "";
}

public class RequisitionStatusRow
{
    public string Status { get; set; } = "";
    public int Count { get; set; }
    public string Color { get; set; } = "";
}

/// <summary>
/// Non-sensitive, aggregated people-signal snapshot. Real single-column aggregates are computed
/// from the sponsor-provided sample HR dataset (10_Sample HR Dataset) — headcount, requisition
/// status, and exit-reason counts. That dataset's direct identifiers are synthetic and every other
/// column was independently shuffled per-row for privacy, so only per-column totals/distributions
/// are valid; row-level combinations (e.g. "this requisition's title and status together") are not
/// and are intentionally not shown. Engagement has no source column in the sample export and stays
/// illustrative, labeled as such.
/// </summary>
public class Snapshot
{
    public string ScopeLabel { get; set; } = "";
    public string SourceNote { get; set; } = "";
    public DateTime LoadedAtUtc { get; set; }
    public List<SnapshotKpi> Kpis { get; set; } = new();
    public List<int> LevelCounts { get; set; } = new();
    public List<string> LevelLabels { get; set; } = new();
    public List<AttritionReason> AttritionByReason { get; set; } = new();
    public List<AttritionReason> AttritionByType { get; set; } = new();
    public double EarlyTenureExitPct { get; set; }
    public List<AttritionReason> HeadcountByRegion { get; set; } = new();
    public List<RequisitionStatusRow> RequisitionStatus { get; set; } = new();
    public List<int> EngagementTrend { get; set; } = new();
}
