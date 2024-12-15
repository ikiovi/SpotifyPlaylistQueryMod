using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using SpotifyPlaylistQueryMod.Models;

namespace SpotifyPlaylistQueryMod.Background;

internal static class PlaylistsComparisonUtil
{
    public static ChangedTracks GetChangedTracksAsync(IEnumerable<ITrackInfo> oldTracks, IEnumerable<ITrackInfo> newTracks)
    {
        var oldTracksTable = oldTracks.ToDictionary(t => t.TrackId);
        var addedTracks = new List<TrackInfo>();

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