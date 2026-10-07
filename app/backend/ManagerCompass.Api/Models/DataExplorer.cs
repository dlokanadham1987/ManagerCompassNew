namespace ManagerCompass.Api.Models;

public class DatasetInfo
{
    public string Key { get; set; } = "";
    public string Label { get; set; } = "";
    public string FileName { get; set; } = "";
    public int RowCount { get; set; }
}

public class PagedColumnResult
{
    public string Dataset { get; set; } = "";
    public string Column { get; set; } = "";
    public int TotalRows { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public List<string> Values { get; set; } = new();
}

/// <summary>
/// A real multi-column grid, restricted to one dataset's "direct identifier" columns — the only
/// columns that stayed row-aligned (not independently shuffled), so combining them is accurate.
/// </summary>
public class IdentifierGridResult
{
    public string Dataset { get; set; } = "";
    public List<string> Columns { get; set; } = new();
    public int TotalRows { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public List<List<string>> Rows { get; set; } = new();
}
