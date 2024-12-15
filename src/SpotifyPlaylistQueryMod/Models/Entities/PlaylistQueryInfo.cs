namespace SpotifyPlaylistQueryMod.Models.Entities;

public sealed record class PlaylistQueryInfo
{
    public int Id { get; init; }
    public required string UserId { get; init; }
    public required string SourceId { get; set; }
    public string? TargetId { get; set; }
    public required string Query { get; set; }
    public bool IsPaused { get; set; }
    public bool IsSuperseded { get; set; }
    public uint Version { get; set; }
    public void Supersede()
    {
        Version++;
        IsSuperseded = true;
        TargetId = null;
    }
}