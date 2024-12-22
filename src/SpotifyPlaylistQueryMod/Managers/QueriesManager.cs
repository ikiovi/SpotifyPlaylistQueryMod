using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using Mono.TextTemplating.CodeCompilation;
using Polly;
using Polly.Retry;
using SpotifyAPI.Web;
using SpotifyPlaylistQueryMod.Data;
using SpotifyPlaylistQueryMod.Managers.Attributes;
using SpotifyPlaylistQueryMod.Mappings;
using SpotifyPlaylistQueryMod.Models.Entities;
using SpotifyPlaylistQueryMod.Utils;
using SpotifyPlaylistQueryMod.Shared.API;

namespace SpotifyPlaylistQueryMod.Managers;

public sealed class QueriesManager
{
    private readonly ApplicationDbContext context;
    private readonly PlaylistsManager playlistsManager;
    private readonly IAsyncPolicy retryPolicy;
    public QueriesManager(ApplicationDbContext context, PlaylistsManager playlistsManager, [DbRetryPolicy] IAsyncPolicy retryPolicy)
    {
        this.context = context;
        this.retryPolicy = retryPolicy;
        this.playlistsManager = playlistsManager;
    }

    public async Task<PlaylistQueryState> CreateFromDTOAsync(string userId, CreatePlaylistQueryDTO queryDTO, CancellationToken cancel = default)
    {
        PlaylistQueryInfo query = queryDTO.ToPlaylistQueryInfo(userId);
        using var transaction = await context.Database.BeginTransactionAsync(cancel);

        await CreateDestinationPlaylistAsync(query, cancel);
        await CreateSourcePlaylistAsync(query, cancel);

        EntityEntry<PlaylistQueryInfo> queryEntry = await context.AddAsync(query, cancel);
        PropertyEntry<PlaylistQueryInfo, int> idProperty = queryEntry.Property(q => q.Id);
        idProperty.IsTemporary = true;

        var state = new PlaylistQueryState { Id = idProperty.CurrentValue, Info = queryEntry.Entity };
        state.ResetInputType();

        await context.AddAsync(state, cancel);
        await context.SaveChangesAsync(cancel);

        //TODO: Rollback
        await transaction.CommitAsync(cancel);
        return state;
    }

    private Task<SourcePlaylist> CreateSourcePlaylistAsync(PlaylistQueryInfo query, CancellationToken cancel)
    {
        return playlistsManager.CreateSourcePlaylistAsync(query.SourceId, query.UserId, cancel);
    }

    private async Task<DestinationPlaylist?> CreateDestinationPlaylistAsync(PlaylistQueryInfo query, CancellationToken cancel)
    {
        if (query.TargetId == null) return null;

        DestinationPlaylist target = await playlistsManager.CreateDestinationPlaylistAsync(query.TargetId, query.UserId, cancel);
        target.EnsureCanSuperseedBy(query.UserId);
        target?.ActiveQuery?.Supersede();
        await context.SaveChangesAsync(cancel);
        return target;
    }

    public async Task<bool> UpdateFromDTOAsync(int id, string userId, UpdatePlaylistQueryDTO queryDTO, CancellationToken cancel = default)
    {
        PlaylistQueryInfo? query = await FindInfoForUserAsync(id, userId, cancel);

        if (query == null) return false;
        if (query == queryDTO.ToPlaylistQueryInfo(userId, id)) return true;

        bool sourceIdChanged = query.SourceId != queryDTO.SourceId;
        bool targetIdChanged = query.TargetId != queryDTO.TargetId;

        if (targetIdChanged) await CreateDestinationPlaylistAsync(query, cancel);
        if (sourceIdChanged) await CreateSourcePlaylistAsync(query, cancel);

        query.SourceId = queryDTO.SourceId;
        query.TargetId = queryDTO.TargetId;
        query.IsPaused = queryDTO.IsPaused;
        query.Query = queryDTO.Query;
        query.Version += Convert.ToUInt16(sourceIdChanged && targetIdChanged);

        // Throws if Version changes. 
        // In other cases where parallelism occurs, data may be overwritten in unexpected ways, but this is user's responsibility (^_^).
        await context.SaveChangesAsync(cancel);

        if (!sourceIdChanged && !targetIdChanged) return true;
        await ResetPlaylistQueryStateAsync(id, sourceIdChanged, targetIdChanged, cancel).WithRetryPolicy(retryPolicy);
        return true;
    }

    public Task ResetPlaylistQueryStateAsync(int id, CancellationToken cancel = default)
    {
        return retryPolicy.ExecuteAsync(async () =>
        {
            PlaylistQueryState? query = await context.QueriesState
            .IgnoreAutoIncludes()
            .SingleAsync(s => s.Id == id, cancel);

            query.ResetInputType();
            query.LastRunSnapshotId = null;

            await context.SaveChangesAsync(cancel);
        });
    }

    public Task TriggerNextCheckAsync(int id, CancellationToken cancel = default)
    {
        return retryPolicy.ExecuteAsync(async () =>
        {
            SourcePlaylist playlist = await context.QueriesInfo
           .Where(q => q.Id == id)
           .Join(context.SourcePlaylists,
               q => q.SourceId,
               p => p.Id,
               (_, p) => p
           )
           .SingleAsync(cancel);

            if (playlist.IsProcessing || playlist.NextCheck < DateTimeOffset.UtcNow) return;

            playlist.NextCheck = DateTimeOffset.UtcNow;

            await context.SaveChangesAsync(cancel);
        });
    }

    private async Task ResetPlaylistQueryStateAsync(int id, bool targetIdChanged, bool sourceIdChanged, CancellationToken cancel = default)
    {
        PlaylistQueryState? query = await context.QueriesState
            .IgnoreAutoIncludes()
            .SingleAsync(s => s.Id == id, cancel);

        if (targetIdChanged)
        {
            query.ClearWriteStatus();
            query.ResetInputType();
        }
        if (sourceIdChanged)
        {
            query.ClearReadStatus();
            query.LastRunSnapshotId = null;
        }

        await context.SaveChangesAsync(cancel);
    }

    public Task<PlaylistQueryInfo?> FindInfoForUserAsync(int id, string userId, CancellationToken cancel = default) =>
        context.QueriesInfo.FirstOrDefaultAsync(q => q.Id == id && q.UserId == userId, cancel);

    public Task<PlaylistQueryState?> FindStateForUserAsync(int id, string userId, CancellationToken cancel = default) =>
        context.QueriesState.FirstOrDefaultAsync(s => s.Id == id && s.Info.UserId == userId, cancel);

    public async Task<List<PlaylistQueryDTO>> GetForUserAsDTOsAsync(string userId, CancellationToken cancel = default) =>
        await context.QueriesInfo
            .Where(q => q.UserId == userId)
            .Join(context.QueriesState,
                q => q.Id,
                s => s.Id,
                (q, s) => new { Query = q, s.Status }
                )
            .Select(q => q.Query.ToDTO(q.Status))
            .ToListAsync(cancel);


    public async Task<bool> RemoveAsync(int id, string userId, CancellationToken cancel = default)
    {
        PlaylistQueryState? query = await FindStateForUserAsync(id, userId, cancel);
        if (query == null) return false;

        using var transaction = await context.Database.BeginTransactionAsync(cancel);
        context.QueriesInfo.Remove(query.Info);
        context.QueriesState.Remove(query);
        await context.SaveChangesAsync(cancel);
        await transaction.CommitAsync(cancel); //TODO: Rollback

        return true;
    }
}