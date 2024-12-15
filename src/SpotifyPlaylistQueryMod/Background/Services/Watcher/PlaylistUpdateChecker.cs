using Microsoft.EntityFrameworkCore;
using SpotifyAPI.Web;
using SpotifyPlaylistQueryMod.Background.Models;
using SpotifyPlaylistQueryMod.Data;
using SpotifyPlaylistQueryMod.Models.Entities;
using SpotifyPlaylistQueryMod.Spotify;
using SpotifyPlaylistQueryMod.Spotify.Exceptions;
using SpotifyPlaylistQueryMod.Spotify.Models;
using SpotifyPlaylistQueryMod.Spotify.Services;
using SpotifyPlaylistQueryMod.Utils;
using System.Runtime.CompilerServices;

namespace SpotifyPlaylistQueryMod.Background.Services.Watcher;

public sealed class PlaylistUpdateChecker
{
    private readonly ApplicationDbContext context;
    private readonly ISpotifyClientFactory clientFactory;

    public PlaylistUpdateChecker(ApplicationDbContext context, ISpotifyClientFactory clientFactory)
    {
        this.context = context;
        this.clientFactory = clientFactory;
    }

    public async IAsyncEnumerable<PlaylistInfoDiff> FetchPendingPlaylistsAsync(bool onlyProcessing, [EnumeratorCancellation] CancellationToken cancel = default)
    {
        List<SourcePlaylist> playlists = await context.SourcePlaylists
            .Where(p => p.NextCheck <= DateTimeOffset.UtcNow && p.IsProcessing == onlyProcessing)
            .ToListAsync(cancel);

        foreach (var playlist in playlists)
        {
            Result<SpotifyPlaylistState> result = await GetPlaylistStateAsync(playlist, cancel: cancel);

            if (result.Exception is SpotifyNotFoundException && !playlist.IsPrivate)
                result = await GetPlaylistStateAsync(playlist, true, cancel);

            if (result.Value is not { SnapshotId: string newSnapshotId, IsPrivate: bool isPrivate }) continue;
            var updatedPlaylist = playlist with { SnapshotId = newSnapshotId, IsPrivate = isPrivate };
            yield return new(playlist, updatedPlaylist);
        }
    }

    private Task<Result<SpotifyPlaylistState>> GetPlaylistStateAsync(SourcePlaylist playlist, bool forcePrivate = false, CancellationToken cancel = default)
    {
        IPlaylistsClient playlistClient = playlist.IsPrivate || forcePrivate ?
            clientFactory.CreatePlaylistClient(playlist.OwnerId) :
            clientFactory.GlobalClient.Playlists;

        return playlistClient.GetStateAsync(playlist.Id, cancel).ToResultTask();
    }
}
