using SpotifyPlaylistQueryMod.Background.Models;
using SpotifyPlaylistQueryMod.Models;
using SpotifyPlaylistQueryMod.Models.Entities;
using SpotifyPlaylistQueryMod.Utils;
using SpotifyPlaylistQueryMod.Shared.API;
using SpotifyPlaylistQueryMod.Shared.Enums;
using SpotifyPlaylistQueryMod.Shared.API.DTO;

namespace SpotifyPlaylistQueryMod.Background.Services.Worker;

public sealed class QueryExecuteService
{
    private readonly HttpClient httpClient;
    private readonly PlaylistsTracksService playlistsTracksService;

    public QueryExecuteService(PlaylistsTracksService playlistsTracksService, HttpClient httpClient)
    {
        this.playlistsTracksService = playlistsTracksService;
        this.httpClient = httpClient;
    }

    public async Task<PlaylistChangeResponse?> ExecuteQueryAsync(PlaylistQueryState state, CancellationToken cancel)
    {
        IChangedTracks<ITrackInfo> changedTracks = await playlistsTracksService.GetChangedTracksAsync(state.Info.SourceId, state.Info.UserId, cancel);
        if (state.InputType == PlaylistQueryInputType.ChangedTracks && !changedTracks.HasChanges) return null;
        try
        {
            return await ExecuteExternalQueryAsync(state, cancel);
        }
        catch (TaskCanceledException) when (!cancel.IsCancellationRequested)
        {
            return null;
        }
    }

    public async Task<PlaylistChangeResponse?> ExecuteExternalQueryAsync(PlaylistQueryState state, CancellationToken cancel)
    {
        var request = await PlaylistChangeRequestFactory.CreateRequestDTOAsync(playlistsTracksService, state, cancel);
        var response = await httpClient.PostAsJsonAsync(state.Info.Query, request, cancel);

        response.EnsureSuccessStatusCode();

        if (state.Info.TargetId == null) return null;
        var changeResponseResult = await response.Content.ReadFromJsonAsync<PlaylistChangeResponseDTO>(cancel).ToResultTask();

        if (changeResponseResult.Value is null) return null;

        Dictionary<string, ITrackInfo> metadata = await playlistsTracksService.GetAllSourceTracksAsync(state.Info.SourceId, state.Info.UserId, cancel);

        return new PlaylistChangeResponse(metadata)
        {
            OriginalResponse = changeResponseResult.Value,
            TargetId = state.Info.TargetId,
            UserId = state.Info.UserId
        };
    }
}
