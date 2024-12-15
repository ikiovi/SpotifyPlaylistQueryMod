using SpotifyAPI.Web;

namespace SpotifyPlaylistQueryMod.Spotify.Services;

public interface ISpotifyClientFactory
{
    public ISpotifyClient GlobalClient { get; }
    public ISpotifyClient CreateClient(string userId);
    public IPlaylistsClient CreatePlaylistClient(string userId);
}