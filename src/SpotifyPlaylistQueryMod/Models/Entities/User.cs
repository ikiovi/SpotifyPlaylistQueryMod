using SpotifyPlaylistQueryMod.Models.Enums;
using SpotifyPlaylistQueryMod.Shared.Enums;

namespace SpotifyPlaylistQueryMod.Models.Entities;

public sealed record class User
{
    public required string Id { get; init; }
    public UserPrivileges Privileges { get; set; }
    public UserStatus Status { get; set; }
    public required string RefreshToken { get; set; }
    public required DateTimeOffset NextRefresh { get; set; }
    public bool IsCollaborationEnabled { get; set; }
}