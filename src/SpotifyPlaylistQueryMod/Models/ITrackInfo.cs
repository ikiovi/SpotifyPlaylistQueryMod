using SpotifyPlaylistQueryMod.Shared.API;

namespace SpotifyPlaylistQueryMod.Models;

public interface ITrackInfo : IBasicTrackInfo
{
    public string? AddedBy { get; }
}