using System.ComponentModel.DataAnnotations;

namespace SpotifyPlaylistQueryMod.Spotify.Configuration;

public sealed record SpotifyClientOptions
{
    public const string SectionName = "SpotifyOptions";
    [Required]
    [MinLength(32, ErrorMessage = "ClientId can't be empty.")]
    public required string ClientId { get; init; }
    [Required]
    [MinLength(32, ErrorMessage = "ClientSecret can't be empty.")]
    public required string ClientSecret { get; init; }
}
