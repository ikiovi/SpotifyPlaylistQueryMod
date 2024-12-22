using System.Data;
using Microsoft.EntityFrameworkCore;
using SpotifyPlaylistQueryMod.Models.Entities;
using SpotifyPlaylistQueryMod.Data;
using SpotifyPlaylistQueryMod.Background.Configuration;
using Microsoft.Extensions.Options;
using SpotifyPlaylistQueryMod.Models;

namespace SpotifyPlaylistQueryMod.Background.Managers;

public class SourcePlaylistStateManager
{
    private readonly ApplicationDbContext context;
    private readonly IOptions<BackgroundProcessingOptions> options;
    private readonly ILogger<SourcePlaylistStateManager> logger;

    public SourcePlaylistStateManager(ApplicationDbContext context, IOptions<BackgroundProcessingOptions> options, ILogger<SourcePlaylistStateManager> logger)
    {
        this.context = context;
        this.options = options;
        this.logger = logger;
    }

    public async Task CompleteProcessingAsync(string id, CancellationToken cancel)
    {
        using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancel);
        SourcePlaylist? playlist = await context.SourcePlaylists.FindAsync([id], cancel);
        if (playlist == null) return;

        playlist.IsProcessing = false;

        await context.SaveChangesAsync(cancel);
        await transaction.CommitAsync(cancel); //TODO: Rollback
    }

    public Task<int> ExecuteNextCheckUpdateAsync(IEnumerable<string> playlists, CancellationToken cancel)
    {
        return ExecuteNextCheckUpdateAsync(playlists, DateTimeOffset.UtcNow + options.Value.PlaylistNextCheckOffset, cancel);
    }

    public async Task PreparePlaylistsAsync(IReadOnlyDictionary<string, IPlaylistInfo> playlists, CancellationToken cancel)
    {
        if (playlists.Count == 0) return;

        var now = DateTimeOffset.UtcNow;
        var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancel);

        var sourcePlaylists = await context.SourcePlaylists
            .Where(p => playlists.Keys.Contains(p.Id))
            .ToListAsync(cancel);

        foreach (SourcePlaylist p in sourcePlaylists)
        {
            p.SnapshotId = playlists[p.Id].SnapshotId;
            p.IsProcessing = true;
            p.NextCheck = now + options.Value.PlaylistNextCheckOffset;
        }

        await context.SaveChangesAsync(cancel);
        await transaction.CommitAsync(cancel);
    }

    public async Task FinishEmptyPlaylistsAsync(IEnumerable<string> playlists, CancellationToken cancel)
    {
        await ExecuteNextCheckUpdateAsync(playlists, cancel);
        await ExceuteIsProcessingUpdateAsync(playlists, false, cancel);
    }

    #region Bulk Methods
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

    public Task<int> ExecuteIsPrivateUpdateAsync(ICollection<string> playlists, bool isPrivate, CancellationToken cancel)
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