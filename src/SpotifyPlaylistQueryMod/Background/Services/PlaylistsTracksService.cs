using Microsoft.EntityFrameworkCore;
using SpotifyAPI.Web;
using SpotifyPlaylistQueryMod.Data;
using SpotifyPlaylistQueryMod.Models;
using SpotifyPlaylistQueryMod.Background.Services.Cache;
using SpotifyPlaylistQueryMod.Spotify.Services;
using SpotifyPlaylistQueryMod.Shared.API;
using SpotifyPlaylistQueryMod.Spotify;

namespace SpotifyPlaylistQueryMod.Background.Services;

public sealed class PlaylistsTracksService
{
    private readonly ApplicationDbContext context;
    private readonly ISpotifyClientFactory clientFactory;
    private readonly PlaylistsInfoCache tracksCache;

    public PlaylistsTracksService(PlaylistsInfoCache tracksCache, ApplicationDbContext context, ISpotifyClientFactory clientFactory)
    {
        this.tracksCache = tracksCache;
        this.context = context;
        this.clientFactory = clientFactory;
    }

    private IAsyncEnumerable<TrackInfo> GetCurrentPlaylistAsync(string id, string userId, CancellationToken cancel)
    {
        ISpotifyClient client = clientFactory.CreateClient(userId);
        return client.GetPlaylistTracksAsync(id, cancel);
    }

    public async Task<IReadOnlyList<ITrackInfo>> GetCurrentTargetPlaylistAsync(string id, string userId, CancellationToken cancel) =>
        await GetCurrentPlaylistAsync(id, userId, cancel).ToListAsync(cancel);

    public async Task<IReadOnlyList<ITrackInfo>> GetCurrentSourcePlaylistAsync(string id, string userId, CancellationToken cancel)
    {
        IReadOnlyList<ITrackInfo>? tracks = await tracksCache.GetPlaylistTracksAsync(id, cancel);
        if (tracks != null) return tracks;

        tracks = await GetCurrentPlaylistAsync(id, userId, cancel).ToListAsync(cancel);

        await tracksCache.StorePlaylistTracksAsync(id, tracks, cancel);
        return tracks;
    }

    public Task<List<ITrackInfo>> GetOldSourcePlaylistAsync(string id, CancellationToken cancel)
    {
        return context.Tracks
            .Where(t => t.SourcePlaylistId == id)
            .ToListAsync<ITrackInfo>(cancel);
    }

    public async Task<IChangedTracks<ITrackInfo>> GetChangedTracksAsync(string id, string userId, CancellationToken cancel)
    {
        ChangedTracks? tracks = await tracksCache.GetChangedTracksAsync(id, cancel);
        if (tracks != null) return tracks;

        IEnumerable<ITrackInfo> oldTracks = await GetOldSourcePlaylistAsync(id, cancel);
        IReadOnlyList<ITrackInfo> newTracks = await GetCurrentSourcePlaylistAsync(id, userId, cancel);

        tracks = PlaylistsComparisonUtil.GetChangedTracksAsync(oldTracks, newTracks);

        await tracksCache.StoreChangedTracksAsync(id, tracks, cancel);
        return tracks;
    }

    public async Task<Dictionary<string, ITrackInfo>> GetAllSourceTracksAsync(string id, string userId, CancellationToken cancel)
    {
        List<ITrackInfo> oldTracks = await GetOldSourcePlaylistAsync(id, cancel);
        IChangedTracks<ITrackInfo>? changedTracks = await tracksCache.GetChangedTracksAsync(id, cancel);

        if (changedTracks != null)
        {
            oldTracks.AddRange(changedTracks.Added);
            oldTracks.AddRange(changedTracks.Removed);
        }
        else
        {
            oldTracks.AddRange(await GetCurrentSourcePlaylistAsync(id, userId, cancel));
        }

        var tracks = new Dictionary<string, ITrackInfo>();

        foreach (ITrackInfo track in oldTracks)
        {
            tracks.TryAdd(track.TrackId, track);
        }

        return tracks;
    }
}