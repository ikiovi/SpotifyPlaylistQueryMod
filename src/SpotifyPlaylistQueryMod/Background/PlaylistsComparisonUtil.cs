using SpotifyPlaylistQueryMod.Models;

namespace SpotifyPlaylistQueryMod.Background;

internal static class PlaylistsComparisonUtil
{
    public static ChangedTracks GetChangedTracksAsync(IEnumerable<ITrackInfo> oldTracks, IEnumerable<ITrackInfo> newTracks)
    {
        var oldTracksTable = new Dictionary<string, ITrackInfo>();
        var addedTracks = new List<TrackInfo>();

        foreach (ITrackInfo track in oldTracks)
        {
            // oldTracks.ToDictionary() will throw exception if there are duplicate tracks
            oldTracksTable.TryAdd(track.TrackId, track);
        }

        foreach (ITrackInfo track in newTracks)
        {
            if (oldTracksTable.Remove(track.TrackId)) continue;
            if (track is not TrackInfo trackInfo) trackInfo = new TrackInfo(track);
            addedTracks.Add(trackInfo);
        }

        return new()
        {
            Added = addedTracks,
            Removed = oldTracksTable.Values.Select(t => new TrackInfo(t)).ToArray(),
        };
    }
}