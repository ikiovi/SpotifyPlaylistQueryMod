using MemoryPack;
using SpotifyPlaylistQueryMod.Shared.API;

namespace SpotifyPlaylistQueryMod.Models;

[MemoryPackable]
public sealed partial class ChangedTracks : IChangedTracks<TrackInfo>
{
    public required IReadOnlyList<TrackInfo> Added { get; init; }
    public required IReadOnlyList<TrackInfo> Removed { get; init; }
}