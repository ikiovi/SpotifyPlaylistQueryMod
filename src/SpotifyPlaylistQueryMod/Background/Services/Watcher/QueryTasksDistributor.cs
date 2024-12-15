using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using SpotifyPlaylistQueryMod.Data;
using SpotifyPlaylistQueryMod.Background.Services.Processing;
using SpotifyPlaylistQueryMod.Background.Services.Queue;
using SpotifyPlaylistQueryMod.Background.Models;
using SpotifyPlaylistQueryMod.Models;
using SpotifyPlaylistQueryMod.Models.Entities;
using SpotifyPlaylistQueryMod.Shared.Enums;
using SpotifyPlaylistQueryMod.Models.Enums;
using SpotifyPlaylistQueryMod.Background.Managers;

namespace SpotifyPlaylistQueryMod.Background.Services.Watcher;

public sealed class QueryTasksDistributor
{
    private readonly ITaskQueue<PlaylistQueryState> tasks;
    private readonly ApplicationDbContext context;
    private readonly SourcePlaylistStateManager playlistsManager;
    private readonly PlaylistProcessingService updateStatusManager;

    public QueryTasksDistributor(ITaskQueue<PlaylistQueryState> tasks, ApplicationDbContext context, PlaylistProcessingService updateStatusManager, SourcePlaylistStateManager playlistsManager)
    {
        this.context = context;
        this.tasks = tasks;
        this.updateStatusManager = updateStatusManager;
        this.playlistsManager = playlistsManager;
    }

    public async Task CreateTaskAsync(Dictionary<string, PlaylistInfoDiff> changedPlaylists, CancellationToken cancel = default)
    {
        Dictionary<string, Queue<PlaylistQueryState>> queries = await context.QueriesState
            .Where(s => changedPlaylists.Keys.Contains(s.Info.SourceId))
            .Where(s => !s.Info.IsSuperseded && !s.Info.IsPaused)
            .Where(s => s.Status == PlaylistQueryExecutionStatus.Active)
            .Join(context.Users,
                s => s.Info.UserId,
                u => u.Id,
                (s, u) => new { QueryState = s, User = u }
            )
            .Where(j => j.User.Status == UserStatus.None)
            .Select(j => j.QueryState)
            .GroupBy(q => q.Info.SourceId)
            .ToDictionaryAsync(
                 g => g.Key,
                 g => GetFilteredBySnapshotQueue(g, changedPlaylists[g.Key]),
                 cancel
            );

        var processingPlaylists = new List<IPlaylistInfo>();
        foreach (var (id, queue) in queries)
        {
            if (queue.Count == 0)
            {
                queries.Remove(id);
                continue;
            }
            updateStatusManager.TrackPlaylist(changedPlaylists[id], queue.Count);
            processingPlaylists.Add(changedPlaylists[id]);
        }

        ILookup<bool, string> emptyPlaylists = changedPlaylists.Values
            .Where(p => !queries.ContainsKey(p.Id))
            .ToLookup(p => p.IsSnapshotIdChanged, p => p.Id);

        await FinishEmptyPlaylists(emptyPlaylists, cancel);

        if (processingPlaylists.Count == 0) return;
        await updateStatusManager.PreparePlaylistsAsync(processingPlaylists, cancel);

        for (var i = 0; i < processingPlaylists.Count;)
        {
            var playlist = processingPlaylists[i];
            if (queries[playlist.Id].TryDequeue(out PlaylistQueryState? s))
            {
                await tasks.QueueAsync(s with { LastRunSnapshotId = playlist.SnapshotId }, cancel);
                i++;
            }
            else
            {
                queries.Remove(playlist.Id);
                processingPlaylists.RemoveAt(i);
            }

            if (i >= processingPlaylists.Count) i = 0;
        }
    }

    private async Task FinishEmptyPlaylists(ILookup<bool, string> playlists, CancellationToken cancel)
    {
        if (playlists.Count == 0) return;

        await playlistsManager.ExecuteNextCheckUpdateAsync(playlists[true], cancel);
        await playlistsManager.ExceuteIsProcessingUpdateAsync(playlists[false], false, cancel);
        await playlistsManager.ExecuteManualCheckReplaceUpdateAsync(cancel);
    }

    private static Queue<PlaylistQueryState> GetFilteredBySnapshotQueue(IEnumerable<PlaylistQueryState> queries, PlaylistInfoDiff playlist) =>
        new(queries.Where(s => s.LastRunSnapshotId != playlist.SnapshotId));
}