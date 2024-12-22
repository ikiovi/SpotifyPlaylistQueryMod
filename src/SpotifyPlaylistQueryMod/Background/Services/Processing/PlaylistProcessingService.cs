using SpotifyPlaylistQueryMod.Background.Services.Cache;
using SpotifyPlaylistQueryMod.Models;
using SpotifyPlaylistQueryMod.Managers;
using SpotifyPlaylistQueryMod.Shared.API;
using SpotifyPlaylistQueryMod.Background.Managers;

namespace SpotifyPlaylistQueryMod.Background.Services.Processing;

public sealed class PlaylistProcessingService
{
    private readonly PlaylistQueriesTracker queriesTracker;
    private readonly PlaylistsInfoCache playlistsCache;
    private readonly TracksManager tracksManager;
    private readonly SourcePlaylistStateManager playlistsManager;

    public PlaylistProcessingService(PlaylistQueriesTracker queriesTracker, PlaylistsInfoCache playlistsCache, TracksManager tracksManager, SourcePlaylistStateManager playlistsManager)
    {
        this.queriesTracker = queriesTracker;
        this.playlistsCache = playlistsCache;
        this.tracksManager = tracksManager;
        this.playlistsManager = playlistsManager;
    }

    private async Task FinishAsync(string playlistId, CancellationToken cancel)
    {
        IChangedTracks<ITrackInfo>? changedTracks = await playlistsCache.GetChangedTracksAsync(playlistId, cancel);
        if (changedTracks == null) throw new InvalidOperationException();
        await tracksManager.ApplyChangedTracksAsync(playlistId, changedTracks, cancel);

        await Task.WhenAll(
            playlistsCache.RemovePlaylistChangesAsync(playlistId, cancel),
            playlistsManager.CompleteProcessingAsync(playlistId, cancel)
        );
    }

    public void TrackPlaylist(string playlistId, int queriesCount) =>
        queriesTracker.Set(playlistId, queriesCount);

    public async Task<bool> TryFinishPlaylistAsync(string playlistId, CancellationToken cancel)
    {
        if (!queriesTracker.Decrement(playlistId)) return false;
        await FinishAsync(playlistId, cancel);
        return true;
    }

    public async Task PrepareProcessingAsync(IReadOnlyDictionary<string, IPlaylistInfo> processingPlaylists, IEnumerable<string> allChangedPlaylists, CancellationToken cancel)
    {
        await playlistsManager.FinishEmptyPlaylistsAsync(
            allChangedPlaylists.Where(id => !processingPlaylists.ContainsKey(id)),
            cancel
        );

        await playlistsManager.PreparePlaylistsAsync(processingPlaylists, cancel);
    }
}