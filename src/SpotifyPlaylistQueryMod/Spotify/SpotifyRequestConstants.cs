using SpotifyAPI.Web;

namespace SpotifyPlaylistQueryMod.Spotify;

internal static class SpotifyRequestConstants
{
    public const string PlaylistInfoGetRequestFileds = "id,owner(id),snapshot_id,public";
    public static readonly PlaylistGetRequest PlaylistInfoRequestOptions = PlaylistGetFromFields(PlaylistInfoGetRequestFileds);

    public const string PlaylistStateGetRequestFileds = "snapshot_id,public,tracks(total)";
    public static readonly PlaylistGetRequest PlaylistStateRequestOptions = PlaylistGetFromFields(PlaylistStateGetRequestFileds);

    public const string TrackInfoRequestFields = "items(track(id,type),added_at,added_by(id))";
    public static readonly PlaylistGetItemsRequest TrackInfoRequestOptions = PlaylistGetItemsRequestFromFields(TrackInfoRequestFields);

    public static PlaylistGetRequest PlaylistGetFromFields(params string[] fields)
    {
        var request = new PlaylistGetRequest(PlaylistGetRequest.AdditionalTypes.Track);
        request.Fields.AddRange(fields);
        return request;
    }

    public static PlaylistGetItemsRequest PlaylistGetItemsRequestFromFields(params string[] fields)
    {
        var request = new PlaylistGetItemsRequest(PlaylistGetItemsRequest.AdditionalTypes.Track);
        request.Fields.Add("next");
        request.Fields.AddRange(fields);
        return request;
    }
}