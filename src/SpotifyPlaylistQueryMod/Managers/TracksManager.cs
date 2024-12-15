using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SpotifyPlaylistQueryMod.Data;
using SpotifyPlaylistQueryMod.Models;
using SpotifyPlaylistQueryMod.Shared.API;
using SpotifyPlaylistQueryMod.Models.Entities;

namespace SpotifyPlaylistQueryMod.Managers;

public sealed class TracksManager
{
    private readonly ApplicationDbContext context;

    public TracksManager(ApplicationDbContext context) => this.context = context;

    public async Task ApplyChangedTracksAsync(string playlistId, IChangedTracks<ITrackInfo> changedTracks, CancellationToken cancel)
    {
        if (!changedTracks.HasChanges) return;

        IEnumerable<Track> added = changedTracks.Added.Select(t => new Track(t, playlistId));
        IEnumerable<string> removed = changedTracks.Removed.Select(t => t.TrackId);

        using var transaction = await context.Database.BeginTransactionAsync(cancel);
        if (changedTracks.Removed.Count > 0)
        {
            await context.Tracks
                .Where(t => t.SourcePlaylistId == playlistId)
                .Where(t => removed.Contains(t.TrackId))
                .ExecuteDeleteAsync(cancel);
        }
        if (changedTracks.Added.Count > 0)
        {
            context.Tracks.AddRange(added);
            await context.SaveChangesAsync(cancel);
        }
        await transaction.CommitAsync(cancel); //TODO: Rollback
    }
}
