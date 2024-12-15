using System.Diagnostics.CodeAnalysis;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Http;

namespace SpotifyPlaylistQueryMod.Spotify.Exceptions.Base;

public class SpotifyAuthenticationFailureException : APIException
{
    public required string UserId { get; init; }
    [SetsRequiredMembers]
    public SpotifyAuthenticationFailureException(string message, string userId) : base(message) => UserId = userId;
    public SpotifyAuthenticationFailureException(IResponse response) : base(response) { }
    public SpotifyAuthenticationFailureException(string message, Exception innerException) : base(message, innerException) { }
}