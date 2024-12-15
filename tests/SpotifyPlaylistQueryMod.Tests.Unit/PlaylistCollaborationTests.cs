using System.Linq.Expressions;
using Newtonsoft.Json;
using NSubstitute;
using SpotifyAPI.Web;
using SpotifyPlaylistQueryMod.Models;
using SpotifyPlaylistQueryMod.Spotify;
using SpotifyPlaylistQueryMod.Spotify.Models;

namespace SpotifyPlaylistQueryMod.Tests.Unit;

public class PlaylistCollaborationTests
{
    [Fact]
    public async Task CollaborationInsertTest()
    {
        string queryOwnerId = "a";
        HashSet<string> validUsers = [queryOwnerId, "b", "c"];
        List<TrackInfo> initialTracks = GetTestTracks("./TestData/Tracks/initial.json");
        List<TrackInfo> expectedTracks = GetTestTracks("./TestData/Tracks/expected.json");

        Dictionary<string, int> tracksCount = initialTracks
            .GroupBy(t => validUsers.Contains(t.AddedBy!) ? t.AddedBy! : queryOwnerId)
            .ToDictionary(g => g.Key, g => g.Count());

        var request = new CollaborativeInsertRequest("_", queryOwnerId, tracksCount, initialTracks)
        {
            AbsolutePosition = 0,
            InitialTracksCount = 0,
        };

        List<string> resultTracksList = [];
        List<string> resultUsersList = [];

        var clients = new Dictionary<string, IPlaylistsClient>();

        foreach (string user in validUsers)
        {
            Action<PlaylistAddItemsRequest> callback = r =>
            {
                var position = r.Position ?? -1;
                resultTracksList.InsertRange(position, r.Uris);
                resultUsersList.InsertRange(position, Enumerable.Repeat(user, r.Uris.Count));
            };
            IPlaylistsClient client = Substitute.For<IPlaylistsClient>();
            await client.AddItems(Arg.Any<string>(), Arg.Do(callback), default);
            clients.Add(user, client);
        }

        await request.ApplyRequestAsync(clients, default!);

        Assert.Equal(expectedTracks.Select(SpotifyAPIExtensions.ToTrackURI), resultTracksList);
        Assert.Equal(expectedTracks.Select(t => t.AddedBy), resultUsersList);
    }

    private static List<TrackInfo> GetTestTracks(string path)
    {
        var json = File.ReadAllText(path);
        var spotifyTracks = JsonConvert.DeserializeObject<List<Track>>(json);

        if (spotifyTracks == null) throw new InvalidOperationException("Test data invalid.");

        return spotifyTracks.Select(t => new TrackInfo(t.trackId, t.addedBy)).ToList();
    }
}

file record Track(string addedBy, string trackId);