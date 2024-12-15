namespace SpotifyPlaylistQueryMod.Spotify.Models;

public record SpotifyPlaylistState
{
    public required string SnapshotId { get; set; }
    public bool Public { get; set; } = true;
    public bool IsPrivate => !Public;
    public int TotalTracksCount { get; set; }

    public void Deconstruct(out string snapshotId, out bool isPrivate)
    {
        snapshotId = SnapshotId;
        isPrivate = IsPrivate;
    }
}