using ClosedXML.Excel;
using ManagerCompass.Api.Models;

namespace ManagerCompass.Api.Services;

/// <summary>
/// Reads the sponsor-provided 10_Sample HR Dataset export live from disk and computes the
/// People Snapshot from it. The dataset's own "Field Guide" sheet in each workbook states that
/// direct identifiers are synthetic and every other column was independently shuffled per row —
/// so this reader only ever counts/groups a single column at a time. It never reads two columns
/// from the same row together, because that combination would not be a real relationship.
/// </summary>
public class SampleHrDatasetReader
{
    private const int HeaderRow = 3; // row 1 = note, row 2 = blank, row 3 = column headers
    private const int FirstDataRow = 4;

    /// <summary>
    /// Resolves "SampleHrDataset:Path" from config. An absolute path (e.g. a teammate's own
    /// OneDrive location) is used as-is; a relative path (e.g. "..\Resources\Sample HR Dataset",
    /// the copy checked into the repo) is resolved against the process's working directory,
    /// which is this project's folder when run via `dotnet run` from ManagerCompass.Api.
    /// </summary>
    public static string? ResolveConfiguredPath(IConfiguration configuration)
    {
        var configured = configuration["SampleHrDataset:Path"];
        if (string.IsNullOrWhiteSpace(configured)) return null;
        return Path.IsPathRooted(configured)
            ? configured
            : Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), configured));
    }

    public Snapshot Read(string folderPath)
    {
        var activeFte = CountColumn(Path.Combine(folderPath, "active_fte.xlsx"), "Shuffled FTEs", "Hier. Level");
        var regions = CountColumn(Path.Combine(folderPath, "active_fte.xlsx"), "Shuffled FTEs", "Region");
        var directReports = CountColumn(Path.Combine(folderPath, "active_fte.xlsx"), "Shuffled FTEs", "Direct Reports");
        var requisitions = CountColumn(Path.Combine(folderPath, "requisitions.xlsx"), "Shuffled Requisitions", "Status");
        var exits = CountColumn(Path.Combine(folderPath, "exits.xlsx"), "Shuffled Exits", "Term Reason");
        var attritionType = CountColumn(Path.Combine(folderPath, "exits.xlsx"), "Shuffled Exits", "Attrition Type");
        var tenureBands = CountColumn(Path.Combine(folderPath, "exits.xlsx"), "Shuffled Exits", "Tenure Bands");
        var contractors = CountColumn(Path.Combine(folderPath, "contractors_interns_fact_consultants.xlsx"), "Shuffled Contractors", null);

        var levelOrder = new[] { "L1", "L2", "L3", "L4", "L5", "L6", "L7", "L8", "L9" };
        var levelCounts = levelOrder.Select(l => activeFte.Counts.GetValueOrDefault(l, 0)).ToList();

        var exitTotal = exits.TotalRows;
        var topReasons = exits.Counts
            .Where(kv => kv.Key != "(blank)")
            .OrderByDescending(kv => kv.Value)
            .Take(4)
            .ToList();

        var reasonColors = new[] { "#7C5C99", "#B85C72", "#2E8C9C", "#C1633C" };
        var attritionByReason = topReasons.Select((kv, i) => new AttritionReason
        {
            Label = kv.Key,
            Percent = Math.Round(kv.Value * 100.0 / exitTotal, 1),
            Color = reasonColors[i % reasonColors.Length]
        }).ToList();

        var regionColors = new[] { "#2C7A78", "#2E8C9C", "#E4693F", "#7C5C99" };
        var headcountByRegion = regions.Counts
            .Where(kv => kv.Key != "(blank)")
            .OrderByDescending(kv => kv.Value)
            .Take(4)
            .Select((kv, i) => new AttritionReason
            {
                Label = kv.Key,
                Percent = Math.Round(kv.Value * 100.0 / regions.TotalRows, 1),
                Color = regionColors[i % regionColors.Length]
            }).ToList();

        var statusColors = new Dictionary<string, string>
        {
            ["Approved"] = "#2C7A78",
            ["Pending Approval"] = "#E4693F",
            ["Hold"] = "#C1633C"
        };
        var requisitionStatus = requisitions.Counts
            .OrderByDescending(kv => kv.Value)
            .Select(kv => new RequisitionStatusRow
            {
                Status = kv.Key,
                Count = kv.Value,
                Color = statusColors.GetValueOrDefault(kv.Key, "#8A9096")
            })
            .ToList();

        var attritionRatePct = Math.Round(exitTotal * 100.0 / (activeFte.TotalRows + exitTotal), 1);

        // Direct Reports is blank for individual contributors and a real number for managers —
        // count of non-blank rows is the manager count, and the average of those numbers is a
        // real, single-column-safe span-of-control figure (no cross-row join involved).
        var managerCount = directReports.TotalRows - directReports.Counts.GetValueOrDefault("(blank)", 0);
        var totalDirectReports = directReports.Counts
            .Where(kv => kv.Key != "(blank)")
            .Sum(kv => int.Parse(kv.Key) * kv.Value);
        var avgSpanOfControl = managerCount > 0 ? Math.Round(totalDirectReports / (double)managerCount, 1) : 0;

        var attritionTypeColors = new Dictionary<string, string> { ["Regrettable"] = "#C8402F", ["Non-Regrettable"] = "#2C7A78" };
        var attritionByType = attritionType.Counts
            .Where(kv => kv.Key != "(blank)")
            .OrderByDescending(kv => kv.Value)
            .Select(kv => new AttritionReason
            {
                Label = kv.Key,
                Percent = Math.Round(kv.Value * 100.0 / exitTotal, 1),
                Color = attritionTypeColors.GetValueOrDefault(kv.Key, "#8A9096")
            }).ToList();

        // "Within first year" = the four Tenure Bands buckets under 1 year, summed within this
        // one column — still a single-column aggregate, just grouping labels of the same column.
        var firstYearBuckets = new[] { "30 Days", "60 Days", "6 Months", "6 Mo to 1 Yr" };
        var earlyTenureCount = tenureBands.Counts.Where(kv => firstYearBuckets.Contains(kv.Key)).Sum(kv => kv.Value);
        var earlyTenureExitPct = Math.Round(earlyTenureCount * 100.0 / exitTotal, 1);

        return new Snapshot
        {
            ScopeLabel = "Sample HR Dataset — company-wide",
            SourceNote = $"Real counts read live from {folderPath} at app startup. Only single-column totals are shown — the dataset's row-level combinations were intentionally shuffled for privacy and are not valid to reconstruct.",
            Kpis = new()
            {
                new SnapshotKpi { Label = "Active headcount", Value = activeFte.TotalRows.ToString("N0"), SubText = "active_fte.xlsx, live read", Color = "#2C7A78" },
                new SnapshotKpi { Label = "Open requisitions", Value = requisitions.TotalRows.ToString("N0"), SubText = string.Join(" · ", requisitionStatus.Select(r => $"{r.Count} {r.Status.ToLowerInvariant()}")), Color = "#2E8C9C" },
                new SnapshotKpi { Label = "Exits, sample", Value = exitTotal.ToString("N0"), SubText = $"~{attritionRatePct}% of headcount+exits (estimate)", Color = "#6E8C52" },
                new SnapshotKpi { Label = "Contingent workforce", Value = contractors.TotalRows.ToString("N0"), SubText = "Contractors, interns & FaCT consultants", Color = "#7C5C99" },
                new SnapshotKpi { Label = "Engagement", Value = "—", SubText = "Illustrative only — no source column in sample dataset", Color = "#E4693F" },
                new SnapshotKpi { Label = "Span of control", Value = avgSpanOfControl.ToString("0.0"), SubText = $"{managerCount:N0} managers · active_fte.xlsx", Color = "#1B6E6B" },
            },
            LevelLabels = levelOrder.ToList(),
            LevelCounts = levelCounts,
            EngagementTrend = new() { 75, 71, 69, 72 },
            AttritionByReason = attritionByReason,
            AttritionByType = attritionByType,
            EarlyTenureExitPct = earlyTenureExitPct,
            HeadcountByRegion = headcountByRegion,
            RequisitionStatus = requisitionStatus
        };
    }

    private (Dictionary<string, int> Counts, int TotalRows) CountColumn(string filePath, string sheetName, string? columnName)
    {
        using var workbook = new XLWorkbook(filePath);
        var sheet = workbook.Worksheet(sheetName);
        var columnIndex = columnName is null ? (int?)null : FindColumnIndex(sheet, columnName);
        var lastRow = sheet.LastRowUsed()!.RowNumber();

        var counts = new Dictionary<string, int>();
        int totalRows = 0;
        for (int r = FirstDataRow; r <= lastRow; r++)
        {
            totalRows++;
            if (columnIndex is null) continue;
            var value = sheet.Cell(r, columnIndex.Value).GetString().Trim();
            if (string.IsNullOrEmpty(value)) value = "(blank)";
            counts[value] = counts.GetValueOrDefault(value, 0) + 1;
        }

        return (counts, totalRows);
    }

    private static int? FindColumnIndex(IXLWorksheet sheet, string columnName)
    {
        foreach (var cell in sheet.Row(HeaderRow).CellsUsed())
        {
            if (string.Equals(cell.GetString().Trim(), columnName, StringComparison.OrdinalIgnoreCase))
                return cell.Address.ColumnNumber;
        }
        return null;
    }

    /// <summary>Real column headers for a dataset file — used to populate the Data Explorer's column picker.</summary>
    public List<string> GetHeaders(string filePath, string sheetName)
    {
        using var workbook = new XLWorkbook(filePath);
        var sheet = workbook.Worksheet(sheetName);
        return sheet.Row(HeaderRow).CellsUsed()
            .Select(c => c.GetString().Trim())
            .Where(h => h.Length > 0)
            .ToList();
    }

    public int GetRowCount(string filePath, string sheetName)
    {
        using var workbook = new XLWorkbook(filePath);
        var sheet = workbook.Worksheet(sheetName);
        return sheet.LastRowUsed()!.RowNumber() - FirstDataRow + 1;
    }

    /// <summary>
    /// Returns one page of raw values from a single named column, in original row order,
    /// optionally filtered to values containing <paramref name="search"/>. Deliberately never
    /// returns more than one column at a time — see the class remarks. A search term only ever
    /// matches within this one column's own values; it cannot surface any other column's data
    /// for the "same" row, because no such relationship exists in this dataset.
    /// </summary>
    public (List<string> Values, int TotalRows) GetColumnPage(string filePath, string sheetName, string columnName, int page, int pageSize, string? search = null)
    {
        using var workbook = new XLWorkbook(filePath);
        var sheet = workbook.Worksheet(sheetName);
        var columnIndex = FindColumnIndex(sheet, columnName)
            ?? throw new ArgumentException($"Column '{columnName}' not found in {sheetName}");
        var lastRow = sheet.LastRowUsed()!.RowNumber();

        if (string.IsNullOrWhiteSpace(search))
        {
            var totalRows = lastRow - FirstDataRow + 1;
            var startRow = FirstDataRow + (page - 1) * pageSize;
            var endRow = Math.Min(startRow + pageSize - 1, lastRow);
            var values = new List<string>();
            for (int r = startRow; r <= endRow; r++) values.Add(CellText(sheet, r, columnIndex));
            return (values, totalRows);
        }

        // Filtered search scans every row in this one column only, then paginates the matches.
        var matches = new List<string>();
        for (int r = FirstDataRow; r <= lastRow; r++)
        {
            var value = CellText(sheet, r, columnIndex);
            if (value.Contains(search, StringComparison.OrdinalIgnoreCase)) matches.Add(value);
        }
        var page1 = matches.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return (page1, matches.Count);
    }

    private static string CellText(IXLWorksheet sheet, int row, int col)
    {
        var value = sheet.Cell(row, col).GetString().Trim();
        return string.IsNullOrEmpty(value) ? "(blank)" : value;
    }

    /// <summary>
    /// Returns real multi-column rows, but ONLY for the caller-supplied identifier columns —
    /// the small set each dataset's Field Guide names as "direct identifiers" that were kept
    /// together (not independently shuffled), e.g. Employee Number/Name/Email all reference the
    /// same synthetic ID. Never call this with an attribute column (Gender, Job, Department, ...)
    /// — those were shuffled independently and combining them here would fabricate a relationship.
    /// Optional per-column filters (contains, case-insensitive, ANDed together) are safe here
    /// because every filtered column already belongs to this same validated identifier set.
    /// </summary>
    public (List<List<string>> Rows, int TotalRows) GetIdentifierRows(string filePath, string sheetName, List<string> identifierColumns, int page, int pageSize, Dictionary<string, string>? columnFilters = null)
    {
        using var workbook = new XLWorkbook(filePath);
        var sheet = workbook.Worksheet(sheetName);
        var columnIndexes = identifierColumns
            .Select(name => FindColumnIndex(sheet, name) ?? throw new ArgumentException($"Column '{name}' not found in {sheetName}"))
            .ToList();
        var lastRow = sheet.LastRowUsed()!.RowNumber();

        var activeFilters = (columnFilters ?? new())
            .Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
            .Select(kv => (Index: identifierColumns.FindIndex(c => string.Equals(c, kv.Key, StringComparison.OrdinalIgnoreCase)), Text: kv.Value))
            .Where(f => f.Index >= 0)
            .ToList();

        if (activeFilters.Count == 0)
        {
            var totalRows = lastRow - FirstDataRow + 1;
            var startRow = FirstDataRow + (page - 1) * pageSize;
            var endRow = Math.Min(startRow + pageSize - 1, lastRow);
            var rows = new List<List<string>>();
            for (int r = startRow; r <= endRow; r++)
                rows.Add(columnIndexes.Select(c => CellText(sheet, r, c)).ToList());
            return (rows, totalRows);
        }

        // A filter is active: scan every row (all filtered columns already belong to the same
        // validated identifier set, so matching several of them together is still accurate).
        var matches = new List<List<string>>();
        for (int r = FirstDataRow; r <= lastRow; r++)
        {
            var rowValues = columnIndexes.Select(c => CellText(sheet, r, c)).ToList();
            var isMatch = activeFilters.All(f => rowValues[f.Index].Contains(f.Text, StringComparison.OrdinalIgnoreCase));
            if (isMatch) matches.Add(rowValues);
        }
        var page1 = matches.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return (page1, matches.Count);
    }
}
