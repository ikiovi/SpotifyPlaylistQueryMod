using System.Diagnostics.CodeAnalysis;

namespace SpotifyPlaylistQueryMod.Models.Entities;

public sealed record Track : ITrackInfo
{
    public int Id { get; init; }
    public required string TrackId { get; init; }
    public required string? AddedBy { get; init; }
    public required DateTime AddedAt { get; init; }
    public required string SourcePlaylistId { get; init; }

    public Track() { }

    [SetsRequiredMembers]
    public Track(ITrackInfo track, string playlistId)
    {
        TrackId = track.TrackId;
        AddedAt = track.AddedAt;
        AddedBy = track.AddedBy;
        SourcePlaylistId = playlistId;
    }
}
