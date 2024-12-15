using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Http;
using SpotifyPlaylistQueryMod.Spotify.Exceptions.Base;

namespace SpotifyPlaylistQueryMod.Spotify.Exceptions;

/// <summary>
/// <a href="https://developer.spotify.com/documentation/web-api/concepts/api-calls#response-error">Spotify documentation</a>, 
/// <a href="https://tools.ietf.org/html/rfc6749#section-5.2">RFC 6749</a> 
/// </summary>
public sealed class SpotifyInvalidGrantException : SpotifyAuthenticationFailureException
{
    // 
    public const string Error = "invalid_grant";

    public SpotifyInvalidGrantException(IResponse response) : base(response) { }
    public SpotifyInvalidGrantException(string message, Exception innerException) : base(message, innerException) { }

    public static bool CanCreate(APIException exception) =>
       exception.Response?.StatusCode == System.Net.HttpStatusCode.BadRequest && exception.Message == Error;
}