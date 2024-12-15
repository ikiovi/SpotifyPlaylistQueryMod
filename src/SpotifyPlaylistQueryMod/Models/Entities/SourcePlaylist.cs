using System.Diagnostics.CodeAnalysis;

namespace SpotifyPlaylistQueryMod.Models.Entities;

public sealed record SourcePlaylist : IPlaylistInfo
{
    public SourcePlaylist()
    {
        NextCheck = DateTimeOffset.UtcNow;
    }

    [SetsRequiredMembers]
    public SourcePlaylist(IPlaylistInfo playlistInfo) : this()
    {
        Id = playlistInfo.Id;
        SnapshotId = playlistInfo.SnapshotId;
        OwnerId = playlistInfo.OwnerId;
        IsPrivate = playlistInfo.IsPrivate;
    }

    public required string Id { get; init; }
    public required string SnapshotId { get; set; }
    public required string OwnerId { get; init; }
    public bool IsPrivate { get; set; }
    public bool IsProcessing { get; set; }
    public DateTimeOffset NextCheck { get; set; }
}