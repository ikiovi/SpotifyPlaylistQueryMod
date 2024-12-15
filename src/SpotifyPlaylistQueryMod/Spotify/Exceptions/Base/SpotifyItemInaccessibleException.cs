using SpotifyAPI.Web;
using SpotifyAPI.Web.Http;

namespace SpotifyPlaylistQueryMod.Spotify.Exceptions.Base;

public class SpotifyItemInaccessibleException : APIException
{
    public required string ItemId { get; init; }
    public SpotifyItemInaccessibleException(string message) : base(message) { }
    public SpotifyItemInaccessibleException(IResponse response) : base(response) { }
    public SpotifyItemInaccessibleException(string message, Exception innerException) : base(message, innerException) { }
}