using SpotifyPlaylistQueryMod.Shared.Enums;
using SpotifyPlaylistQueryMod.Shared.API;
using System.Text.Json.Serialization;

namespace SpotifyPlaylistQueryMod.Background.Models;

internal class PlaylistChangeRequestDTO : IPlaylistChangeRequest<IBasicTrackInfo>
{
    [JsonRequired]
    public required int QueryId { get; init; }
    [JsonRequired]
    public required PlaylistQueryInputType InputType { get; init; }
    public IReadOnlyList<IBasicTrackInfo>? ModifiedSourcePlaylist { get; set; }
    public IReadOnlyList<IBasicTrackInfo>? OriginalSourcePlaylist { get; set; }
    public IReadOnlyList<IBasicTrackInfo>? CurrentTargetPlaylist { get; set; }
    public IChangedTracks<IBasicTrackInfo>? ChangedTracks { get; set; }
}
