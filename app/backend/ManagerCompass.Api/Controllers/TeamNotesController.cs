using ManagerCompass.Api.Models;
using ManagerCompass.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ManagerCompass.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TeamNotesController : ControllerBase
{
    private readonly ITeamNotesService _notes;
    public TeamNotesController(ITeamNotesService notes) => _notes = notes;

    /// <summary>Teams and their members (hard-coded for now).</summary>
    [HttpGet("teams")]
    public IActionResult GetTeams() => Ok(_notes.GetTeams());

    /// <summary>Comments, newest first. Optional filters: targetType ("team"/"member"), targetId, since (UTC timestamp).</summary>
    [HttpGet("comments")]
    public IActionResult GetComments([FromQuery] string? targetType, [FromQuery] string? targetId, [FromQuery] DateTime? since)
        => Ok(_notes.GetComments(targetType, targetId, since?.ToUniversalTime()));

    [HttpPost("comments")]
    public IActionResult Create([FromBody] TeamCommentInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Title)) return BadRequest("Title is required.");
        var created = _notes.Create(input);
        return created is null ? BadRequest("Unknown team or member.") : Ok(created);
    }

    [HttpPut("comments/{id}")]
    public IActionResult Update(string id, [FromBody] TeamCommentInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Title)) return BadRequest("Title is required.");
        var updated = _notes.Update(id, input);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("comments/{id}")]
    public IActionResult Delete(string id) => _notes.Delete(id) ? NoContent() : NotFound();
}
