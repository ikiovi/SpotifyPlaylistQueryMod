using MemoryPack;
using System.Diagnostics.CodeAnalysis;

namespace SpotifyPlaylistQueryMod.Models;

[MemoryPackable]
public sealed partial class PlaylistInfo : IPlaylistInfo
{
    public required string Id { get; init; }
    public required string SnapshotId { get; set; }
    public required string OwnerId { get; init; }
    public required bool IsPrivate { get; set; }

    [MemoryPackConstructor]
    public PlaylistInfo() { }

    [SetsRequiredMembers]
    public PlaylistInfo(IPlaylistInfo playlist)
    {
        Id = playlist.Id;
        SnapshotId = playlist.SnapshotId;
        OwnerId = playlist.OwnerId;
        IsPrivate = playlist.IsPrivate;
    }
}
