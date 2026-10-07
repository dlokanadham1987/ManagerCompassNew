using ManagerCompass.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ManagerCompass.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DataExplorerController : ControllerBase
{
    private readonly IDataExplorerService _service;
    public DataExplorerController(IDataExplorerService service) => _service = service;

    /// <summary>The 4 real sample-dataset files and their row counts.</summary>
    [HttpGet("datasets")]
    public IActionResult GetDatasets() => Ok(_service.GetDatasets());

    /// <summary>Real column headers for one dataset, so the UI can offer a column picker.</summary>
    [HttpGet("{dataset}/columns")]
    public IActionResult GetColumns(string dataset)
    {
        try { return Ok(_service.GetColumns(dataset)); }
        catch (Exception ex) { return NotFound(new { error = ex.Message }); }
    }

    /// <summary>
    /// A real multi-column grid — but only the "direct identifier" columns (e.g. Employee
    /// Number/Name/Email) that stayed row-aligned in the sponsor's export. Safe to combine
    /// because they're the same synthetic ID, not an independently-shuffled attribute.
    /// </summary>
    [HttpGet("{dataset}/identifiers")]
    public IActionResult GetIdentifierGrid(string dataset, [FromQuery] int page = 1, [FromQuery] int pageSize = 25)
    {
        // Any other query string key is treated as "filter this column" — e.g. ?Employee Number=ANON-005230.
        // Safe here because every filterable column already belongs to the same validated identifier set.
        var reserved = new HashSet<string> { "page", "pageSize" };
        var filters = Request.Query
            .Where(kv => !reserved.Contains(kv.Key) && !string.IsNullOrWhiteSpace(kv.Value))
            .ToDictionary(kv => kv.Key, kv => kv.Value.ToString());

        try { return Ok(_service.GetIdentifierGrid(dataset, Math.Max(1, page), Math.Clamp(pageSize, 1, 200), filters)); }
        catch (Exception ex) { return NotFound(new { error = ex.Message }); }
    }

    /// <summary>Attribute columns for a dataset — everything that was independently shuffled and must stay single-column-only.</summary>
    [HttpGet("{dataset}/attribute-columns")]
    public IActionResult GetAttributeColumns(string dataset)
    {
        try { return Ok(_service.GetAttributeColumns(dataset)); }
        catch (Exception ex) { return NotFound(new { error = ex.Message }); }
    }

    /// <summary>
    /// One page of raw values from a single named column — never a full row, since this
    /// dataset's row-level combinations were intentionally shuffled and aren't valid to show.
    /// An optional search term filters within this one column only; it can never surface any
    /// other column's value for the "same" row, because no such relationship exists here.
    /// </summary>
    [HttpGet("{dataset}/{column}")]
    public IActionResult GetColumnPage(string dataset, string column, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, [FromQuery] string? search = null)
    {
        try { return Ok(_service.GetColumnPage(dataset, column, Math.Max(1, page), Math.Clamp(pageSize, 1, 200), search)); }
        catch (Exception ex) { return NotFound(new { error = ex.Message }); }
    }
}
