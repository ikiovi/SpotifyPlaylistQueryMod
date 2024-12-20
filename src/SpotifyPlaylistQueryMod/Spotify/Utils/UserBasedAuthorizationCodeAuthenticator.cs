using Microsoft.Net.Http.Headers;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Http;
using SpotifyPlaylistQueryMod.Spotify.Services;

namespace SpotifyPlaylistQueryMod.Spotify.Utils;

internal sealed class UserBasedAuthorizationCodeAuthenticator : IAuthenticator
{
    private readonly SpotifyTokenManager tokenManager;
    private readonly string userId;
    private readonly string tokenType = "Bearer";
    public UserBasedAuthorizationCodeAuthenticator(SpotifyTokenManager tokenManager, string userId)
    {
        this.tokenManager = tokenManager;
        this.userId = userId;
    }
    public async Task Apply(IRequest request, IAPIConnector apiConnector)
    {
        var accessToken = await tokenManager.GetUserAccessTokenAsync(userId);
        request.Headers[HeaderNames.Authorization] = $"{tokenType} {accessToken}";
    }
}