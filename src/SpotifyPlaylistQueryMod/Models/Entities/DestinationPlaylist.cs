using System.Diagnostics.CodeAnalysis;

namespace SpotifyPlaylistQueryMod.Models.Entities;

public sealed record DestinationPlaylist
{
    public required string Id { get; init; }
    public required string OwnerId { get; init; }
    public bool IsPrivate { get; set; }
    public PlaylistQueryInfo? ActiveQuery { get; set; }

    public DestinationPlaylist() { }

    [SetsRequiredMembers]
    public DestinationPlaylist(IPlaylistInfo playlist)
    {
        Id = playlist.Id;
        OwnerId = playlist.OwnerId;
        IsPrivate = playlist.IsPrivate;
    }

    public void EnsureCanSuperseedBy(string userId)
    {
        if (OwnerId != userId)
            throw new InvalidOperationException("Only owner can supersede existing query");
        if (ActiveQuery?.UserId == userId)
            throw new InvalidOperationException($"You already have query [{ActiveQuery.Id}] for this playlist");
    }
}