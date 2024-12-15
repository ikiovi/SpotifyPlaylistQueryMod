using Microsoft.EntityFrameworkCore;
using SpotifyAPI.Web;
using SpotifyPlaylistQueryMod.Spotify.Services;
using SpotifyPlaylistQueryMod.Models.Entities;
using SpotifyPlaylistQueryMod.Data;
using SpotifyPlaylistQueryMod.Spotify;

namespace SpotifyPlaylistQueryMod.Managers;

public sealed class PlaylistsManager
{
    private readonly ApplicationDbContext context;
    private readonly ISpotifyClientFactory clientFactory;

    public PlaylistsManager(ApplicationDbContext context, ISpotifyClientFactory clientFactory)
    {
        this.context = context;
        this.clientFactory = clientFactory;
    }

    public async Task<SourcePlaylist> CreateSourcePlaylistAsync(string playlistId, string userId, CancellationToken cancel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(playlistId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        SourcePlaylist? playlist = await context.SourcePlaylists.FindAsync([playlistId], cancellationToken: cancel);

        if (playlist != null && playlist.OwnerId == userId) return playlist;

        IPlaylistsClient client = clientFactory.CreatePlaylistClient(userId);

        if (playlist != null)
        {
            await client.EnsureReadAccessAsync(playlistId, cancel);
            return playlist;
        }

        playlist = new(await client.GetInfoAsync(playlistId, cancel));

        await context.SourcePlaylists.AddAsync(playlist, cancel);
        await context.SaveChangesAsync(cancel);
        return playlist;
    }

    public async Task<DestinationPlaylist> CreateDestinationPlaylistAsync(string playlistId, string userId, CancellationToken cancel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(playlistId, nameof(playlistId));
        ArgumentException.ThrowIfNullOrWhiteSpace(userId, nameof(userId));

        DestinationPlaylist? playlist = await context.DestinationPlaylists
            .Include(p => p.ActiveQuery)
            .FirstOrDefaultAsync(p => p.Id == playlistId, cancellationToken: cancel);

        IPlaylistsClient client = clientFactory.CreatePlaylistClient(userId);

        if (playlist?.OwnerId == userId) return playlist;
        if (playlist != null)
        {
            await client.EnsureWriteAccessAsync(playlistId, cancel);
            return playlist;
        }

        playlist = new(await client.GetInfoAsync(playlistId, cancel));

        if (playlist.OwnerId != userId)
            await client.EnsureWriteAccessAsync(playlistId, cancel);

        await context.DestinationPlaylists.AddAsync(playlist, cancel);
        await context.SaveChangesAsync(cancel);

        return playlist;
    }
}