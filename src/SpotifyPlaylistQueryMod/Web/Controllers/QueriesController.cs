using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpotifyPlaylistQueryMod.Managers;
using SpotifyPlaylistQueryMod.Mappings;
using SpotifyPlaylistQueryMod.Models.Entities;
using SpotifyPlaylistQueryMod.Web.Extensions;
using SpotifyPlaylistQueryMod.Shared.Enums;
using SpotifyPlaylistQueryMod.Shared.API.DTO;

namespace SpotifyPlaylistQueryMod.Web.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public sealed class QueriesController : ControllerBase
{
    private readonly QueriesManager queriesManager;
    public string CurrentUserId => User.GetCurrentUserId();

    public QueriesController(QueriesManager queriesManager) => this.queriesManager = queriesManager;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PlaylistQueryDTO>>> GetQueries() =>
        await queriesManager.GetForUserAsDTOsAsync(CurrentUserId);

    [HttpGet("{id}")]
    public async Task<ActionResult<PlaylistQueryDTO>> GetQuery(int id)
    {
        PlaylistQueryState? query = await queriesManager.FindStateForUserAsync(id, CurrentUserId);
        if (query == null) return NotFound();
        return query.ToDTO();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutQuery(int id, UpdatePlaylistQueryDTO queryDTO)
    {
        try
        {
            bool result = await queriesManager.UpdateFromDTOAsync(id, CurrentUserId, queryDTO);
            if (!result) return NotFound();
        }
        catch (DBConcurrencyException)
        {
            return Conflict();
        }
        catch //TODO
        {
            return BadRequest();
        }
        return NoContent();
    }

    [HttpPost]
    public async Task<ActionResult<PlaylistQueryDTO>> PostQuery(CreatePlaylistQueryDTO queryDTO)
    {
        try
        {
            PlaylistQueryState state = await queriesManager.CreateFromDTOAsync(CurrentUserId, queryDTO);
            return CreatedAtAction(nameof(GetQuery), new { state.Id }, state.ToDTO());
        }
        catch //TODO
        {
            return BadRequest();
        }
    }

    [HttpPost("{id}/reset")]
    public async Task<IActionResult> ResetState(int id)
    {
        await queriesManager.ResetPlaylistQueryStateAsync(id);
        return NoContent();
    }

    [HttpPost("{id}/schedule")]
    public async Task<IActionResult> TriggerExecution(int id)
    {
        await queriesManager.TriggerNextCheckAsync(id);
        return Accepted();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePlaylistQuery(int id)
    {
        bool result = await queriesManager.RemoveAsync(id, CurrentUserId);
        if (!result) return NotFound();
        return NoContent();
    }
}
