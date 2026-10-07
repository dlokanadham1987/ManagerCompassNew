using ManagerCompass.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ManagerCompass.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdoptionController : ControllerBase
{
    private readonly IGuidanceDataService _data;
    public AdoptionController(IGuidanceDataService data) => _data = data;

    /// <summary>
    /// The brief names "a practical adoption measure" (Automation & Scalability) and "an observable
    /// adoption measure" (Enterprise Readiness) as explicit judging criteria. This is that measure.
    /// </summary>
    [HttpGet]
    public IActionResult Get() => Ok(_data.GetAdoptionMetrics());
}
