using System.Security.Claims;

namespace SpotifyPlaylistQueryMod.Web.Extensions;

public static class PrincipalExtensions
{
    public static string GetCurrentUserId(this ClaimsPrincipal principal)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) throw new InvalidOperationException();
        return userId;
    }
}
