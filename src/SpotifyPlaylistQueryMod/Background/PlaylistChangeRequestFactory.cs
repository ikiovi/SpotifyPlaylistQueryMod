using SpotifyPlaylistQueryMod.Background.Services;
using SpotifyPlaylistQueryMod.Models.Entities;
using SpotifyPlaylistQueryMod.Shared.Enums;
using SpotifyPlaylistQueryMod.Shared.API;
using SpotifyPlaylistQueryMod.Background.Models;

namespace SpotifyPlaylistQueryMod.Background;

internal static class PlaylistChangeRequestFactory
{
    public static async Task<PlaylistChangeRequestDTO> CreateRequestDTOAsync(PlaylistsTracksService tracksService, PlaylistQueryState state, CancellationToken cancel)
    {
        IChangedTracks<IBasicTrackInfo>? changeTracks = null;
        IReadOnlyList<IBasicTrackInfo>? oldSourcePlaylist = null;
        IReadOnlyList<IBasicTrackInfo>? newSourcePlaylist = null;
        IReadOnlyList<IBasicTrackInfo>? targetPlaylist = null;

        if (state.InputType.HasFlag(PlaylistQueryInputType.ChangedTracks))
            changeTracks = await tracksService.GetChangedTracksAsync(state.Info.SourceId, state.Info.UserId, cancel);
        if (state.InputType.HasFlag(PlaylistQueryInputType.OriginalSourcePlaylist))
            oldSourcePlaylist = await tracksService.GetOldSourcePlaylistAsync(state.Info.SourceId, cancel);
        if (state.InputType.HasFlag(PlaylistQueryInputType.ModifiedSourcePlaylist))
            newSourcePlaylist = await tracksService.GetCurrentSourcePlaylistAsync(state.Info.SourceId, state.Info.UserId, cancel);
        if (state.Info.TargetId != null && state.InputType.HasFlag(PlaylistQueryInputType.CurrentTargetPlaylist))
            targetPlaylist = await tracksService.GetCurrentTargetPlaylistAsync(state.Info.TargetId, state.Info.UserId, cancel);

        return new()
        {
            QueryId = state.Id,
            InputType = state.InputType,
            ChangedTracks = changeTracks,
            ModifiedSourcePlaylist = newSourcePlaylist,
            OriginalSourcePlaylist = oldSourcePlaylist,
            CurrentTargetPlaylist = targetPlaylist,
        };
    }
}
