using MemoryPack;
using Microsoft.Extensions.Caching.Distributed;
using SpotifyPlaylistQueryMod.Models;

namespace SpotifyPlaylistQueryMod.Background.Services.Cache;

public sealed class PlaylistsInfoCache
{
    private readonly IDistributedCache cache;
    public PlaylistsInfoCache(IDistributedCache cache) => this.cache = cache;

    public async Task<string> GetPlaylistSnapshotIdAsync(string playlistId, CancellationToken cancel)
    {
        var snapshotId = await cache.GetStringAsync(playlistId, cancel);
        if (string.IsNullOrWhiteSpace(snapshotId)) throw new KeyNotFoundException();

        return snapshotId;
    }

    public Task StorePlaylistSnapshotAsync(IPlaylistInfo playlist, CancellationToken cancel) =>
        cache.SetStringAsync(playlist.Id, playlist.SnapshotId, cancel);

    public Task RemovePlaylistChangesAsync(string playlistId, CancellationToken cancel)
    {
        return Task.WhenAll(
            cache.RemoveAsync(playlistId, cancel),
            cache.RemoveAsync(ChangedTracksKey(playlistId), cancel),
            cache.RemoveAsync(PlaylistTracksKey(playlistId), cancel)
        );
    }

    public async Task<List<TrackInfo>?> GetPlaylistTracksAsync(string playlistId, CancellationToken cancel)
    {
        var bytes = await cache.GetAsync(PlaylistTracksKey(playlistId), cancel);
        if (bytes == null) return null;
        return MemoryPackSerializer.Deserialize<List<TrackInfo>>(bytes);
    }
    public async Task<ChangedTracks?> GetChangedTracksAsync(string playlistId, CancellationToken cancel)
    {
        var bytes = await cache.GetAsync(ChangedTracksKey(playlistId), cancel);
        if (bytes == null) return null;
        return MemoryPackSerializer.Deserialize<ChangedTracks>(bytes);
    }
    public Task StoreChangedTracksAsync(string playlistId, ChangedTracks tracks, CancellationToken cancel)
    {
        return SerializeAndSetAsync(ChangedTracksKey(playlistId), tracks, cancel);
    }
    public Task StorePlaylistTracksAsync(string playlistId, IEnumerable<ITrackInfo> tracks, CancellationToken cancel)
    {
        var dto = tracks
            .Select(t => new TrackInfo(t))
            .ToList();
        return SerializeAndSetAsync(PlaylistTracksKey(playlistId), dto, cancel);
    }

    private Task SerializeAndSetAsync<T>(string key, T value, CancellationToken cancel)
    {
        var bytes = MemoryPackSerializer.Serialize(value);
        return cache.SetAsync(key, bytes, cancel);
    }

    public static string ChangedTracksKey(string playlistId) => $"CT_{playlistId}";
    public static string PlaylistTracksKey(string playlistId) => $"SPT_{playlistId}";
}
