using Microsoft.EntityFrameworkCore;
using SpotifyPlaylistQueryMod.Background.Models;
using SpotifyPlaylistQueryMod.Background.Services.Processing;
using SpotifyPlaylistQueryMod.Background.Services.Queue;
using SpotifyPlaylistQueryMod.Models.Entities;
using SpotifyPlaylistQueryMod.Spotify.Exceptions.Base;
using SpotifyPlaylistQueryMod.Spotify.Services;
using SpotifyPlaylistQueryMod.Shared.API;
using SpotifyPlaylistQueryMod.Background.Managers;

namespace SpotifyPlaylistQueryMod.Background.Services.Worker;

internal sealed class PlaylistQueryBackgroundWorker : BackgroundService
{
    private readonly ITaskQueue<PlaylistQueryState> tasks;
    private readonly ILogger<PlaylistQueryBackgroundWorker> logger;
    private readonly IServiceScopeFactory scopeFactory;

    public PlaylistQueryBackgroundWorker(ILogger<PlaylistQueryBackgroundWorker> logger, IServiceScopeFactory scopeFactory, ITaskQueue<PlaylistQueryState> tasks)
    {
        this.tasks = tasks;
        this.logger = logger;
        this.scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("{ServiceName} started.", nameof(PlaylistQueryBackgroundWorker));

        while (!stoppingToken.IsCancellationRequested)
        {
            PlaylistQueryState query = await tasks.DequeueAsync(stoppingToken);

            using var scope = scopeFactory.CreateScope();
            var queriesManager = scope.ServiceProvider.GetRequiredService<QueryStateManager>();
            var processingService = scope.ServiceProvider.GetRequiredService<PlaylistProcessingService>();

            try
            {
                await ProcessQueryAsync(query, scope, stoppingToken);
                await queriesManager.FinishProcessingAsync(query, stoppingToken);
            }
            catch (SpotifyItemInaccessibleException ex)
            {
                if (ex.ItemId == query.Info.SourceId) query.SetSourceStatusUnreadable();
                if (ex.ItemId == query.Info.SourceId) query.SetTargetStatusUnwritable();
                await queriesManager.FinishProcessingAsync(query, stoppingToken);
            }
            catch (SpotifyAuthenticationFailureException ex) when (ex.InnerException is not DbUpdateException) { }
            catch (HttpRequestException) { }
            await processingService.TryFinishPlaylistAsync(query.Info.SourceId, stoppingToken);
        }
    }

    private static async Task ProcessQueryAsync(PlaylistQueryState query, IServiceScope scope, CancellationToken cancel)
    {
        var tis = scope.ServiceProvider.GetRequiredService<SpotifyTracksInserter>();
        var qes = scope.ServiceProvider.GetRequiredService<QueryExecuteService>();

        PlaylistChangeResponse? changeResponse = await qes.ExecuteQueryAsync(query, cancel);

        if (changeResponse is null) return;

        query.InputType = changeResponse.OriginalResponse.NewInputType;

        await tis.ApplyPlaylistChangeResponseAsync(changeResponse, cancel);
    }
}