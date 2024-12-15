using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SpotifyPlaylistQueryMod.Models.Entities;
using SpotifyPlaylistQueryMod.Data;
using System.Data;
using SpotifyPlaylistQueryMod.Background.Configuration;
using Microsoft.Extensions.Options;

namespace SpotifyPlaylistQueryMod.Background.Managers;

public class SourcePlaylistStateManager
{
    private readonly ApplicationDbContext context;
    private readonly IOptions<BackgroundProcessingOptions> options;

    public SourcePlaylistStateManager(ApplicationDbContext context, IOptions<BackgroundProcessingOptions> options)
    {
        this.context = context;
        this.options = options;
    }

    public Task CompleteProcessingAsync(string id, string snapshotId, CancellationToken cancel) =>
        CompleteProcessingAsync(id, snapshotId, DateTimeOffset.UtcNow + options.Value.PlaylistNextCheckOffset, cancel);

    private async Task CompleteProcessingAsync(string id, string snapshotId, DateTimeOffset nextCheck, CancellationToken cancel)
    {
        using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancel);
        SourcePlaylist? playlist = await context.SourcePlaylists.FindAsync([id], cancel);
        if (playlist == null) return;

        playlist.SnapshotId = snapshotId;
        playlist.IsProcessing = false;
        playlist.NextCheck = playlist.NextCheck == DateTimeOffset.MinValue ? DateTimeOffset.UtcNow : nextCheck;

        await context.SaveChangesAsync(cancel);
        await transaction.CommitAsync(cancel); //TODO: Rollback
    }

    #region Bulk Methods
    public Task<int> ExecuteManualCheckReplaceUpdateAsync(CancellationToken cancel = default)
    {
        var now = DateTimeOffset.UtcNow;
        return context.SourcePlaylists
            .Where(p => p.NextCheck == DateTimeOffset.MinValue)
            .Where(p => !p.IsProcessing)
            .ExecuteUpdateAsync(e =>
                e.SetProperty(p => p.NextCheck, now),
                cancel
            );
    }

    public Task<int> ExceuteIsProcessingUpdateAsync(IEnumerable<string> playlists, bool value = true, CancellationToken cancel = default)
    {
        if (!playlists.Any()) return Task.FromResult(0);

        return context.SourcePlaylists
            .Where(p => playlists.Contains(p.Id))
            .Where(p => p.IsProcessing != value)
            .ExecuteUpdateAsync(p =>
                p.SetProperty(p => p.IsProcessing, value),
                cancel
            );
    }

    public Task<int> ExecuteIsPrivateUpdateAsync(IReadOnlyList<string> playlists, bool isPrivate, CancellationToken cancel)
    {
        if (playlists.Count == 0) return Task.FromResult(0);

        return context.SourcePlaylists
            .Where(p => playlists.Contains(p.Id))
            .Where(p => p.IsPrivate != isPrivate)
            .ExecuteUpdateAsync(p =>
                p.SetProperty(p => p.IsPrivate, isPrivate),
                cancel
            );
    }

    public Task<int> ExecuteNextCheckUpdateAsync(IEnumerable<string> playlists, CancellationToken cancel) =>
        ExecuteNextCheckUpdateAsync(playlists, DateTimeOffset.UtcNow + options.Value.PlaylistNextCheckOffset, cancel);

    private Task<int> ExecuteNextCheckUpdateAsync(IEnumerable<string> playlists, DateTimeOffset nextCheck, CancellationToken cancel)
    {
        if (!playlists.Any()) return Task.FromResult(0);

        return context.SourcePlaylists
            .Where(p => playlists.Contains(p.Id))
            .ExecuteUpdateAsync(
                e => e
                .SetProperty(p => p.NextCheck, nextCheck),
                cancel
            );
    }
    #endregion
}