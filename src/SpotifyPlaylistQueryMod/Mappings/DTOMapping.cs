using SpotifyPlaylistQueryMod.Models.Entities;
using SpotifyPlaylistQueryMod.Shared.API;
using SpotifyPlaylistQueryMod.Shared.Enums;

namespace SpotifyPlaylistQueryMod.Mappings;

public static class DTOMapping
{
    public static PlaylistQueryDTO ToDTO(this PlaylistQueryInfo query, PlaylistQueryExecutionStatus status)
    {
        return new(status)
        {
            Id = query.Id,
            Query = query.Query,
            SourceId = query.SourceId,
            TargetId = query.TargetId,
            IsPaused = query.IsPaused,
            IsSuperseded = query.IsSuperseded,
        };
    }

    public static PlaylistQueryDTO ToDTO(this PlaylistQueryState state)
    {
        return state.Info.ToDTO(state.Status);
    }

    public static PlaylistQueryInfo ToPlaylistQueryInfo(this CreatePlaylistQueryDTO from, string userId)
    {
        return new()
        {
            UserId = userId,
            Query = from.Query,
            SourceId = from.SourceId,
            TargetId = from.TargetId,
        };
    }

    public static PlaylistQueryInfo ToPlaylistQueryInfo(this CreatePlaylistQueryDTO from, string userId, int id)
    {
        return new()
        {
            Id = id,
            UserId = userId,
            Query = from.Query,
            SourceId = from.SourceId,
            TargetId = from.TargetId,
        };
    }
}
