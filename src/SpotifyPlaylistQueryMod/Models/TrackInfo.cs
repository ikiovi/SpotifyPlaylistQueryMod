using System.Diagnostics.CodeAnalysis;
using MemoryPack;

namespace SpotifyPlaylistQueryMod.Models;

[MemoryPackable]
public sealed partial class TrackInfo : ITrackInfo
{
    public required string TrackId { get; init; }
    public required string? AddedBy { get; init; }
    public required DateTime AddedAt { get; init; }

    [MemoryPackConstructor]
    public TrackInfo() { }

    [SetsRequiredMembers]
    public TrackInfo(string trackId, string addedBy)
    {
        TrackId = trackId;
        AddedBy = addedBy;
        AddedAt = DateTime.MinValue;
    }

    [SetsRequiredMembers]
    public TrackInfo(ITrackInfo trackInfo)
    {
        TrackId = trackInfo.TrackId;
        AddedAt = trackInfo.AddedAt;
        AddedBy = trackInfo.AddedBy;
    }
}
