using System.Collections.Concurrent;
using NuGet.Packaging.Signing;
using SpotifyPlaylistQueryMod.Utils;
using SpotifyPlaylistQueryMod.Background.Services.Cache;
using SpotifyPlaylistQueryMod.Background.Models;
using SpotifyPlaylistQueryMod.Models;
using SpotifyPlaylistQueryMod.Managers;
using SpotifyPlaylistQueryMod.Shared.API;
using SpotifyPlaylistQueryMod.Background.Managers;

namespace SpotifyPlaylistQueryMod.Background.Services.Processing;

public sealed class PlaylistProcessingService
{
    private readonly PlaylistQueriesTracker queriesTracker;
    private readonly PlaylistsInfoCache playlistsCache;
    private readonly SourcePlaylistStateManager playlistManager;
    private readonly TracksManager tracksManager;

    public PlaylistProcessingService(PlaylistQueriesTracker queriesTracker, PlaylistsInfoCache playlistsCache, SourcePlaylistStateManager playlistManager, TracksManager tracksManager)
    {
        this.queriesTracker = queriesTracker;
        this.playlistsCache = playlistsCache;
        this.playlistManager = playlistManager;
        this.tracksManager = tracksManager;
    }

    private async Task FinishAsync(string playlistId, CancellationToken cancel)
    {
        var snapshotId = await playlistsCache.GetPlaylistSnapshotIdAsync(playlistId, cancel);
        IChangedTracks<ITrackInfo>? changedTracks = await playlistsCache.GetChangedTracksAsync(playlistId, cancel);
        if (changedTracks == null) throw new InvalidOperationException();
        await tracksManager.ApplyChangedTracksAsync(playlistId, changedTracks, cancel);

        await Task.WhenAll(
            playlistManager.CompleteProcessingAsync(playlistId, snapshotId, cancel),
            playlistsCache.RemovePlaylistChangesAsync(playlistId, cancel)
        );
    }

    public Task PreparePlaylistsAsync(IReadOnlyList<IPlaylistInfo> playlists, CancellationToken cancel)
    {
        if (playlists.Count == 0) return Task.CompletedTask;

        IEnumerable<Task> tasks = playlists
            .Select(p => playlistsCache.StorePlaylistSnapshotAsync(p, cancel))
            .Prepend(playlistManager.ExceuteIsProcessingUpdateAsync(playlists.Select(p => p.Id), true, cancel));

        return Task.WhenAll(tasks);
    }

    public void TrackPlaylist(IPlaylistInfo playlist, int queriesCount) =>
        queriesTracker.Set(playlist.Id, queriesCount);

    public async Task<bool> TryFinishPlaylistAsync(string playlistId, CancellationToken cancel)
    {
        if (!queriesTracker.Decrement(playlistId)) return false;
        await FinishAsync(playlistId, cancel);
        return true;
    }
}