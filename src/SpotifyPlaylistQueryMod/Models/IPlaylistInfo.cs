namespace SpotifyPlaylistQueryMod.Models;

public interface IPlaylistInfo
{
    public string Id { get; }
    public string SnapshotId { get; }
    public string OwnerId { get; }
    public bool IsPrivate { get; }
}