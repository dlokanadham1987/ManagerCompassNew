using ManagerCompass.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ManagerCompass.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeadershipController : ControllerBase
{
    private readonly IGuidanceDataService _data;
    public LeadershipController(IGuidanceDataService data) => _data = data;

    /// <summary>Evidence-linked, baseball-card-style operating-review summary.</summary>
    [HttpGet]
    public IActionResult Get() => Ok(_data.GetLeadershipCard());
}
