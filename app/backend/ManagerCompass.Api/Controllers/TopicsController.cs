using ManagerCompass.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ManagerCompass.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TopicsController : ControllerBase
{
    private readonly IGuidanceDataService _data;
    public TopicsController(IGuidanceDataService data) => _data = data;

    /// <summary>All 9 guidance categories — used by the Assistant topic grid, Playbooks view, and Escalation Directory.</summary>
    [HttpGet]
    public IActionResult GetAll() => Ok(_data.GetTopics());

    /// <summary>One topic by key, e.g. "engagement".</summary>
    [HttpGet("{key}")]
    public IActionResult GetOne(string key)
    {
        var topic = _data.GetTopic(key);
        return topic is null ? NotFound() : Ok(topic);
    }
}
