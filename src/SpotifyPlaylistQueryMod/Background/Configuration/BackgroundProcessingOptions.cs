using System.ComponentModel.DataAnnotations;

namespace SpotifyPlaylistQueryMod.Background.Configuration;

public record BackgroundProcessingOptions
{
    public const string SectionName = "ProcessingOptions";

    [Required]
    public required TimeSpan WatchInterval { get; set; }
    [Required]
    public required TimeSpan PlaylistNextCheckOffset { get; set; }
}
