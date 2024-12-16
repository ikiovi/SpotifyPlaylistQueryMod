using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpotifyPlaylistQueryMod.Managers;
using SpotifyPlaylistQueryMod.Mappings;
using SpotifyPlaylistQueryMod.Shared.API;
using SpotifyPlaylistQueryMod.Web.Extensions;

namespace SpotifyPlaylistQueryMod.Web.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UsersManager usersManager;
    public string CurrentUserId => User.GetCurrentUserId();

    public UsersController(UsersManager usersManager) => this.usersManager = usersManager;

    [HttpGet("me")]
    public async Task<ActionResult<UserDTO>> GetMe() =>
        (await usersManager.GetAuthenticatedUserAsync(CurrentUserId)).ToDTO();

    [HttpPatch("collaboration")]
    public async Task<IActionResult> SetCollaborationStatus(UserCollaborationStatusUpdateDTO dto)
    {
        try
        {
            await usersManager.UpdateCollaborationStatusAsync(CurrentUserId, dto.IsCollaborationEnabled);
            return NoContent();
        }
        catch (DBConcurrencyException)
        {
            return Conflict();
        }
    }
}
