﻿using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Polly;
using SpotifyPlaylistQueryMod.Data;
using SpotifyPlaylistQueryMod.Managers.Attributes;
using SpotifyPlaylistQueryMod.Models.Entities;
using SpotifyPlaylistQueryMod.Utils;
using SpotifyPlaylistQueryMod.Shared.Enums;

namespace SpotifyPlaylistQueryMod.Background.Managers;

public class QueryStateManager
{
    private readonly ApplicationDbContext context;
    public QueryStateManager(ApplicationDbContext context) => this.context = context;

    public async Task FinishProcessingAsync(PlaylistQueryState from, CancellationToken cancel)
    {
        PlaylistQueryInfo? queryInfo = await context.QueriesInfo.FindAsync([from.Id], cancel);
        if (queryInfo == null || queryInfo.IsSuperseded) return;

        var sourceIdChanged = queryInfo.SourceId != from.Info.SourceId;
        var targetIdChanged = queryInfo.TargetId != from.Info.TargetId;

        if (sourceIdChanged && targetIdChanged) return;

        // Isolate only PlaylistQueryState
        using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancel);
        PlaylistQueryState? queryState = await context.QueriesState
            .IgnoreAutoIncludes()
            .SingleAsync(s => s.Id == from.Id, cancel);

        if (!sourceIdChanged)
        {
            queryState.Status = from.Status & ~PlaylistQueryExecutionStatus.NoWriteAccess;
            if (!queryState.Status.HasFlag(PlaylistQueryExecutionStatus.NoReadAccess)) queryState.LastRunSnapshotId = from.LastRunSnapshotId;
        }
        if (!targetIdChanged)
        {
            queryState.Status = from.Status & ~PlaylistQueryExecutionStatus.NoReadAccess;
            if (!queryState.Status.HasFlag(PlaylistQueryExecutionStatus.NoWriteAccess)) queryState.InputType = from.InputType;
        }

        await context.SaveChangesAsync(cancel);
        await transaction.CommitAsync(cancel); //TODO: Rollback
    }

    #region Bulk Methods
    public Task<int> ExecuteReadStatusUpdateAsync(IReadOnlyList<string> playlists, bool allowRead, CancellationToken cancel = default)
    {
        if (playlists.Count == 0) return Task.FromResult(0);

        var queries = context.QueriesState
            .Where(s => playlists.Contains(s.Info.SourceId))
            .Join(context.SourcePlaylists,
                s => s.Info.SourceId,
                p => p.Id,
                (s, p) => new { QueryState = s, p.OwnerId }
            )
            .Where(j => j.QueryState.Info.UserId != j.OwnerId)
            .Select(joined => joined.QueryState);

        return ExecuteReadStatusUpdateAsync(queries, allowRead, cancel);
    }

    public static Task<int> ExecuteReadStatusUpdateAsync(IQueryable<PlaylistQueryState> queries, bool allowRead, CancellationToken cancel = default)
    {
        return queries
            .Where(p => p.Status.HasFlag(PlaylistQueryExecutionStatus.NoReadAccess) == allowRead)
            .ExecuteUpdateAsync(
                DbExpressions.StatusUpdateAsyncLambdaExpression(
                    allowRead ? DbExpressions.UpdateReadAccessExpression : DbExpressions.BlockReadAccessExpression
                ),
                cancel
            );
    }
    #endregion
}