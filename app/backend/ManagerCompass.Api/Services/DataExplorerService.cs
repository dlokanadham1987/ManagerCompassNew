using ManagerCompass.Api.Models;

namespace ManagerCompass.Api.Services;

public class DataExplorerService : IDataExplorerService
{
    private readonly string? _folderPath;
    private readonly SampleHrDatasetReader _reader = new();
    private readonly ILogger<DataExplorerService> _logger;

    private static readonly Dictionary<string, (string File, string Sheet, string Label)> DatasetMap = new()
    {
        ["active_fte"] = ("active_fte.xlsx", "Shuffled FTEs", "Active FTEs"),
        ["requisitions"] = ("requisitions.xlsx", "Shuffled Requisitions", "Requisitions"),
        ["exits"] = ("exits.xlsx", "Shuffled Exits", "Exits"),
        ["contractors"] = ("contractors_interns_fact_consultants.xlsx", "Shuffled Contractors", "Contractors, Interns & FaCT")
    };

    // Per each dataset's own "Field Guide" tab: these specific columns were kept together
    // (not independently shuffled) because they're all the same synthetic ID expressed a few
    // ways. Verified empirically too — e.g. active_fte row 1 has Employee Number "ANON-005230",
    // Employee Name "Employee 005230", Email "employee005230@anonymous.invalid" — same ID, every time.
    private static readonly Dictionary<string, string[]> IdentifierColumnsMap = new()
    {
        ["active_fte"] = new[] { "Employee Number", "Employee Name", "Preferred Name", "Email Address", "Supervisor Name", "L2", "L3", "L4", "L5", "Functional Leader", "Director", "VP", "SVP", "EVP" },
        ["exits"] = new[] { "Employee Number", "Employee Name", "Preferred Name", "Email Address", "Supervisor Name", "L2", "L3", "L4", "L5", "Functional Leader", "Director", "VP", "SVP", "EVP" },
        ["contractors"] = new[] { "Employee Number", "Employee Name", "Preferred Name", "Email Address", "Supervisor Name", "L2", "L3", "L4", "L5", "Functional Leader", "Director", "VP", "SVP", "EVP" },
        ["requisitions"] = new[] { "Requsition Number", "Job Code", "External ID", "Hiring Manager : Email Address", "Recruiter Email", "Current Approver : Email Address" }
    };

    public DataExplorerService(IConfiguration configuration, ILogger<DataExplorerService> logger)
    {
        _logger = logger;
        var path = SampleHrDatasetReader.ResolveConfiguredPath(configuration);
        _folderPath = path is not null && Directory.Exists(path) ? path : null;
        if (_folderPath is null)
            _logger.LogWarning("Data Explorer: sample HR dataset folder not found at {Path}", path);
    }

    private (string File, string Sheet, string Label) Resolve(string datasetKey)
    {
        if (_folderPath is null) throw new InvalidOperationException("Sample HR dataset folder is not available on this machine.");
        if (!DatasetMap.TryGetValue(datasetKey, out var entry)) throw new ArgumentException($"Unknown dataset '{datasetKey}'");
        return entry;
    }

    public List<DatasetInfo> GetDatasets()
    {
        if (_folderPath is null) return new();
        return DatasetMap.Select(kv =>
        {
            var filePath = Path.Combine(_folderPath, kv.Value.File);
            return new DatasetInfo
            {
                Key = kv.Key,
                Label = kv.Value.Label,
                FileName = kv.Value.File,
                RowCount = _reader.GetRowCount(filePath, kv.Value.Sheet)
            };
        }).ToList();
    }

    public List<string> GetColumns(string datasetKey)
    {
        var (file, sheet, _) = Resolve(datasetKey);
        var filePath = Path.Combine(_folderPath!, file);
        return _reader.GetHeaders(filePath, sheet);
    }

    public List<string> GetIdentifierColumns(string datasetKey)
    {
        var all = GetColumns(datasetKey);
        var idSet = IdentifierColumnsMap.GetValueOrDefault(datasetKey, Array.Empty<string>());
        return all.Where(c => idSet.Contains(c, StringComparer.OrdinalIgnoreCase)).ToList();
    }

    public List<string> GetAttributeColumns(string datasetKey)
    {
        var all = GetColumns(datasetKey);
        var idSet = IdentifierColumnsMap.GetValueOrDefault(datasetKey, Array.Empty<string>());
        return all.Where(c => !idSet.Contains(c, StringComparer.OrdinalIgnoreCase)).ToList();
    }

    public IdentifierGridResult GetIdentifierGrid(string datasetKey, int page, int pageSize, Dictionary<string, string>? columnFilters = null)
    {
        var (file, sheet, _) = Resolve(datasetKey);
        var filePath = Path.Combine(_folderPath!, file);
        var columns = GetIdentifierColumns(datasetKey);
        var (rows, totalRows) = _reader.GetIdentifierRows(filePath, sheet, columns, page, pageSize, columnFilters);
        return new IdentifierGridResult
        {
            Dataset = datasetKey,
            Columns = columns,
            TotalRows = totalRows,
            Page = page,
            PageSize = pageSize,
            TotalPages = Math.Max(1, (int)Math.Ceiling(totalRows / (double)pageSize)),
            Rows = rows
        };
    }

    public PagedColumnResult GetColumnPage(string datasetKey, string column, int page, int pageSize, string? search = null)
    {
        var (file, sheet, _) = Resolve(datasetKey);
        var filePath = Path.Combine(_folderPath!, file);
        var (values, totalRows) = _reader.GetColumnPage(filePath, sheet, column, page, pageSize, search);
        return new PagedColumnResult
        {
            Dataset = datasetKey,
            Column = column,
            TotalRows = totalRows,
            Page = page,
            PageSize = pageSize,
            TotalPages = Math.Max(1, (int)Math.Ceiling(totalRows / (double)pageSize)),
            Values = values
        };
    }
}
