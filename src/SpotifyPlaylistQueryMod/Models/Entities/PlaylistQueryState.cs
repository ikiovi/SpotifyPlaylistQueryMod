using SpotifyPlaylistQueryMod.Shared.Enums;

namespace SpotifyPlaylistQueryMod.Models.Entities;

public sealed record class PlaylistQueryState
{
    public int Id { get; init; }
    public string? LastRunSnapshotId { get; set; }
    public PlaylistQueryExecutionStatus Status { get; set; }
    public PlaylistQueryInputType InputType { get; set; } = PlaylistQueryInputType.ModifiedSourcePlaylist;
    public PlaylistQueryInfo Info { get; set; } = default!;

    public void ResetInputType() =>
        InputType = PlaylistQueryInputType.ModifiedSourcePlaylist | (Info?.TargetId == null ? 0 : PlaylistQueryInputType.CurrentTargetPlaylist);

    public void SetSourceStatusUnreadable() =>
        Status = Status | PlaylistQueryExecutionStatus.Blocked | PlaylistQueryExecutionStatus.NoReadAccess;
    public void SetTargetStatusUnwritable()
    {
        if (Info?.TargetId == null) throw new InvalidOperationException();
        Status = Status | PlaylistQueryExecutionStatus.Blocked | PlaylistQueryExecutionStatus.NoWriteAccess;
    }

    public void ClearSourceReadStatus() =>
        Status = UpdateExecutionStatus(Status & ~PlaylistQueryExecutionStatus.NoReadAccess);
    public void ClearTargetWriteStatus() =>
        Status = UpdateExecutionStatus(Status & ~PlaylistQueryExecutionStatus.NoWriteAccess);

    public static PlaylistQueryExecutionStatus UpdateExecutionStatus(PlaylistQueryExecutionStatus status) =>
        status.HasFlag(PlaylistQueryExecutionStatus.NoReadAccess) || status.HasFlag(PlaylistQueryExecutionStatus.NoWriteAccess) ?
            status : status & ~PlaylistQueryExecutionStatus.Blocked;
}