using System.Runtime.CompilerServices;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Http;
using SpotifyPlaylistQueryMod.Models;
using SpotifyPlaylistQueryMod.Spotify.Exceptions;
using SpotifyPlaylistQueryMod.Spotify.Models;

namespace SpotifyPlaylistQueryMod.Spotify;

public static class SpotifyAPIExtensions
{
    private static readonly PlaylistChangeDetailsRequest WriteAccessCheckRequest = new() { Description = string.Empty };

    private static async Task<T> MapExceptions<T>(this Task<T> task, string id)
    {
        try
        {
            return await task;
        }
        catch (APIException ex) when (SpotifyNotFoundException.CanCreate(ex))
        {
            throw new SpotifyNotFoundException(ex.Response!) { ItemId = id };
        }
        catch (APIException ex) when (SpotifyForbiddenException.CanCreate(ex))
        {
            throw new SpotifyForbiddenException(ex.Response!) { ItemId = id };
        }
    }

    private static Task<FullPlaylist> GetAsync(this IPlaylistsClient client, string id, PlaylistGetRequest request, CancellationToken cancel) =>
        client.Get(id, request, cancel).MapExceptions(id);

    public static async Task<PlaylistInfo> GetInfoAsync(this IPlaylistsClient client, string id, CancellationToken cancel = default)
    {
        FullPlaylist? playlist = await client.GetAsync(id, SpotifyRequestConstants.PlaylistInfoRequestOptions, cancel);

        if (string.IsNullOrWhiteSpace(playlist.Id) ||
            string.IsNullOrWhiteSpace(playlist.Owner?.Id) ||
            string.IsNullOrWhiteSpace(playlist.SnapshotId)) throw new APIException();

        return new()
        {
            Id = playlist.Id,
            OwnerId = playlist.Owner.Id,
            SnapshotId = playlist.SnapshotId,
            IsPrivate = playlist.Public ?? false
        };
    }

    public static async Task<SpotifyPlaylistState> GetStateAsync(this IPlaylistsClient client, string id, CancellationToken cancel = default)
    {
        FullPlaylist? playlist = await client.GetAsync(id, SpotifyRequestConstants.PlaylistStateRequestOptions, cancel);

        if (string.IsNullOrWhiteSpace(playlist.SnapshotId) ||
            playlist is not { Public: bool isPublic, Tracks.Total: int total }) throw new APIException();

        return new()
        {
            SnapshotId = playlist.SnapshotId,
            TotalTracksCount = total,
            Public = isPublic,
        };
    }

    public static async IAsyncEnumerable<TrackInfo> GetPlaylistTracksAsync(this ISpotifyClient client, string id, [EnumeratorCancellation] CancellationToken cancel = default)
    {
        Paging<PlaylistTrack<IPlayableItem>> tracks = await client.Playlists
            .GetItems(id, SpotifyRequestConstants.TrackInfoRequestOptions, cancel)
            .MapExceptions(id);

        await foreach (var track in client.Paginate(tracks, cancel: cancel))
        {
            if (track.Track is not FullTrack { Id: string trackId }) continue;

            yield return new()
            {
                TrackId = trackId,
                AddedBy = track.AddedBy.Id,
                AddedAt = track.AddedAt ?? DateTime.MinValue
            };
        }
    }

    public static Task EnsureReadAccessAsync(this IPlaylistsClient client, string id, CancellationToken cancel = default) =>
        client.GetAsync(id, SpotifyRequestConstants.PlaylistStateRequestOptions, cancel);

    public static Task EnsureWriteAccessAsync(this IPlaylistsClient client, string id, CancellationToken cancel = default) =>
        client.ChangeDetails(id, WriteAccessCheckRequest, cancel).MapExceptions(id);

    public static async Task<bool> HasWriteAccessAsync(this IPlaylistsClient client, string id, CancellationToken cancel = default)
    {
        try
        {
            await client.EnsureWriteAccessAsync(id, cancel);
            return true;
        }
        catch (SpotifyForbiddenException)
        {
            return false;
        }
    }

    public static async Task<SnapshotResponse?> RemoveTracksAsync(this IPlaylistsClient client, string id, IEnumerable<string> tracks, CancellationToken cancel = default)
    {
        if (!tracks.Any()) return null;

        IEnumerable<PlaylistRemoveItemsRequest> requests = tracks
            .Select(id => new PlaylistRemoveItemsRequest.Item { Uri = ConvertToSpotifyTrackURI(id) })
            .Chunk(100)
            .Select(items => new PlaylistRemoveItemsRequest { Tracks = items });

        SnapshotResponse? response = null;

        foreach (var request in requests)
        {
            response = await client.RemoveItems(id, request, cancel).MapExceptions(id);
        }

        return response;
    }

    public static async Task<SnapshotResponse?> AddTracksAsync(this IPlaylistsClient client, string id, IEnumerable<string> tracks, int position, CancellationToken cancel = default)
    {
        if (!tracks.Any()) return null;

        IEnumerable<PlaylistAddItemsRequest> requests = tracks
            .Select(ConvertToSpotifyTrackURI)
            .Chunk(100)
            .Select((t, n) => new PlaylistAddItemsRequest(t) { Position = position + 100 * n });

        SnapshotResponse? response = null;

        foreach (var request in requests)
        {
            response = await client.AddItems(id, request, cancel).MapExceptions(id);
        }

        return response;
    }

    public static string ConvertToSpotifyTrackURI(string id) => $"spotify:track:{id}";
    public static string ToTrackURI(this ITrackInfo track) => ConvertToSpotifyTrackURI(track.TrackId);
}