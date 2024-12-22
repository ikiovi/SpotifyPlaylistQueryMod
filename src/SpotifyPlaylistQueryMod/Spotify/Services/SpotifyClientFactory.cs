using SpotifyAPI.Web;
using SpotifyPlaylistQueryMod.Spotify.Utils;

namespace SpotifyPlaylistQueryMod.Spotify.Services;

public sealed class SpotifyClientFactory : ISpotifyClientFactory
{
    private readonly SpotifyTokenManager tokenManager;
    private readonly SpotifyClientConfig clientConfig;
    private readonly ISpotifyClient globalClient;

    public ISpotifyClient GlobalClient => globalClient;

    public SpotifyClientFactory(SpotifyTokenManager tokenManager, SpotifyClientConfig clientConfig, ISpotifyClient globalClient)
    {
        this.tokenManager = tokenManager;
        this.clientConfig = clientConfig;
        this.globalClient = globalClient;
    }

    public SpotifyClientConfig WithUserBasedAuthenticator(string userId)
    {
        var authenticator = new UserBasedAuthorizationCodeAuthenticator(tokenManager, userId);
        return clientConfig.WithAuthenticator(authenticator);
    }

    public ISpotifyClient CreateClient(string userId) =>
        new SpotifyClient(WithUserBasedAuthenticator(userId));


    public IPlaylistsClient CreatePlaylistClient(string userId) =>
        new PlaylistsClient(WithUserBasedAuthenticator(userId).BuildAPIConnector());
}