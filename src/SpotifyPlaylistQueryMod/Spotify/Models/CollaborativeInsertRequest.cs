using SpotifyAPI.Web;
using SpotifyPlaylistQueryMod.Utils;
using SpotifyPlaylistQueryMod.Models;

namespace SpotifyPlaylistQueryMod.Spotify.Models;

internal class CollaborativeInsertRequest
{
    private readonly HashSet<string> validUsers;
    private int addedTracks = 0;

    public required int AbsolutePosition { get; init; }
    public required int InitialTracksCount { get; init; }
    public int TotalTracksCount => InitialTracksCount + addedTracks;

    public string TargetId { get; }
    public string DefaultUserId { get; }
    public string BaseRequestsUserId { get; }
    public IEnumerable<TracksInsertRequest> BaseRequest { get; }
    public IEnumerable<TracksInsertRequest> RemainingRequests { get; }

    public CollaborativeInsertRequest(string targetId, string userId, IReadOnlyDictionary<string, int> countByValidUser, IReadOnlyList<ITrackInfo> tracks)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(countByValidUser.Count, 2, "collaboratorsCount");

        validUsers = countByValidUser.Keys.ToHashSet();

        TargetId = targetId;
        DefaultUserId = userId;

        BaseRequestsUserId = countByValidUser.MaxBy(kv => kv.Value).Key;

        ILookup<string, TracksInsertRequest> requests = CreateTracksInsertRequests(tracks, DefaultUserId)
            .ToLookup(r => UserIdOrDefault(r.UserId) == BaseRequestsUserId ? nameof(BaseRequest) : nameof(RemainingRequests));

        BaseRequest = requests[nameof(BaseRequest)];
        RemainingRequests = requests[nameof(RemainingRequests)];
    }

    public async Task ApplyRequestAsync(IReadOnlyDictionary<string, IPlaylistsClient> clients, CancellationToken cancel)
    {
        await ApplyBaseRequestsAsync(clients[BaseRequestsUserId], clients[DefaultUserId], cancel);
        await ApplyRemainingRequestsAsync(clients, cancel);
    }

    public async Task ApplyRemainingRequestsAsync(IReadOnlyDictionary<string, IPlaylistsClient> clients, CancellationToken cancel)
    {
        foreach (TracksInsertRequest r in RemainingRequests)
        {
            var currentUserId = validUsers.Contains(r.UserId) ? r.UserId : DefaultUserId;
            IPlaylistsClient currentClient = clients[currentUserId];

            var position = GetAbsolutePosition(TotalTracksCount, AbsolutePosition + r.RelativePosititon);
            addedTracks += r.Tracks.Count;

            if (r.UserId != DefaultUserId)
            {
                Result<SnapshotResponse?> result = await AddTracksAsync(currentClient, r.Tracks, position, cancel).ToResultTask();
                if (result.IsSuccess) continue;
                validUsers.Remove(r.UserId);
                currentClient = clients[DefaultUserId];
            }

            await AddTracksAsync(currentClient, r.Tracks, position, cancel);
        }
    }

    public async Task ApplyBaseRequestsAsync(IPlaylistsClient client, IPlaylistsClient fallbackClient, CancellationToken cancel)
    {
        var tracks = BaseRequest
            .SelectMany(r => r.Tracks)
            .ToList();

        addedTracks += tracks.Count;

        Result<SnapshotResponse?> result = await AddTracksAsync(client, tracks, cancel).ToResultTask();
        if (!result.IsSuccess) await AddTracksAsync(fallbackClient, tracks, cancel);
    }

    private Task<SnapshotResponse?> AddTracksAsync(IPlaylistsClient client, IList<string> tracks, CancellationToken cancel) =>
        AddTracksAsync(client, tracks, AbsolutePosition, cancel);
    private Task<SnapshotResponse?> AddTracksAsync(IPlaylistsClient client, IList<string> tracks, int position, CancellationToken cancel) =>
        client.AddTracksAsync(TargetId, tracks, position, cancel);

    private string UserIdOrDefault(string userId) =>
        validUsers.Contains(userId) ? userId : DefaultUserId;

    public static int GetAbsolutePosition(int total, int position)
    {
        return position switch
        {
            >= 0 when position <= total => position,
            < -1 => Math.Max(total - position, 0),
            _ => total
        };
    }

    public static IEnumerable<TracksInsertRequest> CreateTracksInsertRequests(IReadOnlyList<ITrackInfo> tracks, string defaultUser)
    {
        if (tracks.Count == 0) yield break;

        var position = 0;
        var currentUser = tracks[0].AddedBy ?? defaultUser;

        var currentRequest = new List<string>();
        var uniqueTracks = new HashSet<string>();

        for (var i = 0; i < tracks.Count; i++)
        {
            var track = tracks[i];
            var addedBy = track.AddedBy ?? defaultUser;

            if (addedBy != currentUser || currentRequest.Count >= 100)
            {
                yield return new(currentRequest, position - currentRequest.Count, currentUser);
                currentRequest = [];
                currentUser = addedBy;
            }

            currentRequest.Add(track.TrackId);
            uniqueTracks.Add(track.TrackId);
            position++;

            if (i + 1 != tracks.Count) continue;
            yield return new(currentRequest, position - currentRequest.Count, currentUser);
        }
    }
    public record TracksInsertRequest(IList<string> Tracks, int RelativePosititon, string UserId);
}