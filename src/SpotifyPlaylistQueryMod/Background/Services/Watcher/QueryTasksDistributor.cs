using Microsoft.EntityFrameworkCore;
using SpotifyPlaylistQueryMod.Background.Models;
using SpotifyPlaylistQueryMod.Background.Services.Processing;
using SpotifyPlaylistQueryMod.Background.Services.Queue;
using SpotifyPlaylistQueryMod.Data;
using SpotifyPlaylistQueryMod.Models;
using SpotifyPlaylistQueryMod.Models.Entities;
using SpotifyPlaylistQueryMod.Models.Enums;
using SpotifyPlaylistQueryMod.Shared.Enums;

namespace SpotifyPlaylistQueryMod.Background.Services.Watcher;

public sealed class QueryTasksDistributor
{
    private readonly ITaskQueue<ProcessingQueryState> tasks;
    private readonly ApplicationDbContext context;
    private readonly PlaylistProcessingService processingService;

    public QueryTasksDistributor(ITaskQueue<ProcessingQueryState> tasks, ApplicationDbContext context, PlaylistProcessingService processingService)
    {
        this.context = context;
        this.tasks = tasks;
        this.processingService = processingService;
    }

    public async Task CreateTaskAsync(Dictionary<string, IPlaylistInfo> changedPlaylists, CancellationToken cancel = default)
    {
        Dictionary<string, Queue<PlaylistQueryState>> queries = await context.QueriesState
            .AsNoTracking()
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
            processingService.TrackPlaylist(id, queue.Count);
            processingPlaylists.Add(changedPlaylists[id]);
        }

        await processingService.PrepareProcessingAsync(
            processingPlaylists.ToDictionary(p => p.Id),
            changedPlaylists.Keys,
            cancel
        );

        for (var i = 0; i < processingPlaylists.Count;)
        {
            var playlist = processingPlaylists[i];
            if (queries[playlist.Id].TryDequeue(out PlaylistQueryState? s))
            {
                await tasks.QueueAsync(new(s, playlist.SnapshotId), cancel);
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

    private static Queue<PlaylistQueryState> GetFilteredBySnapshotQueue(IEnumerable<PlaylistQueryState> queries, IPlaylistInfo playlist) =>
        new(queries.Where(s => s.LastRunSnapshotId != playlist.SnapshotId));
}