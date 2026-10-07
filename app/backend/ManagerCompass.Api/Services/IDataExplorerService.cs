using ManagerCompass.Api.Models;

namespace ManagerCompass.Api.Services;

/// <summary>
/// Lets the UI browse the real sample HR dataset one column at a time — never a full row —
/// since the dataset's own documentation says row-level combinations were intentionally
/// shuffled for privacy and are not valid to reconstruct.
/// </summary>
public interface IDataExplorerService
{
    List<DatasetInfo> GetDatasets();
    List<string> GetColumns(string datasetKey);
    PagedColumnResult GetColumnPage(string datasetKey, string column, int page, int pageSize, string? search = null);

    /// <summary>The dataset's "direct identifier" columns — the ones safe to combine because they were never independently shuffled.</summary>
    List<string> GetIdentifierColumns(string datasetKey);

    /// <summary>Attribute columns (everything not an identifier) — these stay single-column-only.</summary>
    List<string> GetAttributeColumns(string datasetKey);

    IdentifierGridResult GetIdentifierGrid(string datasetKey, int page, int pageSize, Dictionary<string, string>? columnFilters = null);
}
