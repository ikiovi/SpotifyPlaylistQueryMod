using SpotifyPlaylistQueryMod.Models;

namespace SpotifyPlaylistQueryMod.Background.Models;

public sealed class PlaylistInfoDiff : IPlaylistInfo
{
    private readonly IPlaylistInfo currentPlaylist;
    public string Id => currentPlaylist.Id;
    public string OwnerId => currentPlaylist.OwnerId;
    public bool IsPrivate => currentPlaylist.IsPrivate;
    public string SnapshotId => currentPlaylist.SnapshotId;

    public bool IsChanged => IsPrivateChanged || IsSnapshotIdChanged;
    public bool IsSnapshotIdChanged { get; init; }
    public bool IsPrivateChanged { get; init; }

    public PlaylistInfoDiff(IPlaylistInfo playlist) => currentPlaylist = playlist;
    public PlaylistInfoDiff(IPlaylistInfo oldPlaylistVersion, IPlaylistInfo newPlaylistVersion) : this(newPlaylistVersion)
    {
        IsSnapshotIdChanged = oldPlaylistVersion.SnapshotId != newPlaylistVersion.SnapshotId;
        IsPrivateChanged = oldPlaylistVersion.IsPrivate != newPlaylistVersion.IsPrivate;
    }
}