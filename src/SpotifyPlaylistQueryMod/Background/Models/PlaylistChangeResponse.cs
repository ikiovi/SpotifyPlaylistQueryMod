using SpotifyPlaylistQueryMod.Models;
using SpotifyPlaylistQueryMod.Shared.API;

namespace SpotifyPlaylistQueryMod.Background.Models;

public class PlaylistChangeResponse
{
    private readonly Dictionary<string, ITrackInfo> metadata;
    public required PlaylistChangeResponseDTO OriginalResponse { get; init; }
    public required string TargetId { get; init; }
    public required string UserId { get; init; }

    public IEnumerable<ITrackInfo> Add => OriginalResponse.Add.Select(GetTrackInfo);
    public IEnumerable<ITrackInfo> Remove => OriginalResponse.Remove.Select(GetTrackInfo);

    public PlaylistChangeResponse(Dictionary<string, ITrackInfo> metadata) => this.metadata = metadata;

    public ITrackInfo GetTrackInfo(string trackId) =>
        metadata.TryGetValue(trackId, out var track) ? track : new TrackInfo(trackId, UserId);
}
