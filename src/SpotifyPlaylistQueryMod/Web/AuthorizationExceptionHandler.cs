using AspNet.Security.OAuth.Spotify;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Diagnostics;
using SpotifyPlaylistQueryMod.Spotify.Exceptions.Base;
using SpotifyPlaylistQueryMod.Web.Extensions;

namespace SpotifyPlaylistQueryMod.Web;

internal class AuthorizationExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not SpotifyAuthenticationFailureException { UserId: string userId }) return false;
        if (httpContext.User.Identity?.IsAuthenticated == false) return true;
        if (httpContext.User.GetCurrentUserId() != userId) return false;
        await httpContext.SignOutAsync(SpotifyAuthenticationDefaults.AuthenticationScheme);
        return true;
    }
}