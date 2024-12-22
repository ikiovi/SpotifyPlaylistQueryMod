using Microsoft.AspNetCore.Mvc.ViewFeatures;
using SpotifyPlaylistQueryMod.Models.Entities;
using SpotifyPlaylistQueryMod.Shared.Enums;

namespace SpotifyPlaylistQueryMod.Background.Models;

public sealed record ProcessingQueryState : PlaylistQueryState
{
    public PlaylistQueryState OldState { get; }
    public ProcessingQueryState(PlaylistQueryState original, string newSnapshotId) : base(original)
    {
        OldState = original;
        LastRunSnapshotId = newSnapshotId;
    }

    public bool IsSourceReadable => !Status.HasFlag(PlaylistQueryExecutionStatus.NoReadAccess);
    public bool IsTargetWritable => !Status.HasFlag(PlaylistQueryExecutionStatus.NoWriteAccess);
}