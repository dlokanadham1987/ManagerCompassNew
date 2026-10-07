using ManagerCompass.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ManagerCompass.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SnapshotController : ControllerBase
{
    private readonly IGuidanceDataService _data;
    public SnapshotController(IGuidanceDataService data) => _data = data;

    /// <summary>Non-sensitive, aggregated people-signal snapshot for the People Snapshot view.</summary>
    [HttpGet]
    public IActionResult Get() => Ok(_data.GetSnapshot());
}
