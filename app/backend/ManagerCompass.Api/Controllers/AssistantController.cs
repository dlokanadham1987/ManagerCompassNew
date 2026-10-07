using ManagerCompass.Api.Models;
using ManagerCompass.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ManagerCompass.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AssistantController : ControllerBase
{
    private readonly IGuidanceDataService _data;
    public AssistantController(IGuidanceDataService data) => _data = data;

    /// <summary>
    /// Classifies a manager's free-text question (or a forced topic key from a tile/chip click)
    /// into a grounded topic answer, a guardrail refusal, or a clarifying question.
    /// Never accepts or returns individual employee data.
    /// </summary>
    [HttpPost("ask")]
    public IActionResult Ask([FromBody] AskRequest request) => Ok(_data.Ask(request));
}
