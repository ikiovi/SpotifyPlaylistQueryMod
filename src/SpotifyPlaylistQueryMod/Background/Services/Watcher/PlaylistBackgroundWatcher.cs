using Microsoft.Extensions.Options;
using SpotifyPlaylistQueryMod.Background.Configuration;
using SpotifyPlaylistQueryMod.Background.Managers;
using SpotifyPlaylistQueryMod.Background.Models;
using SpotifyPlaylistQueryMod.Models;
using SpotifyPlaylistQueryMod.Spotify.Exceptions;
using SpotifyPlaylistQueryMod.Utils;

namespace SpotifyPlaylistQueryMod.Background.Services.Watcher;

internal sealed class PlaylistBackgroundWatcher : BackgroundService
{
    private readonly ILogger<PlaylistBackgroundWatcher> logger;
    private readonly IServiceScopeFactory scopeFactory;
    private readonly IOptions<BackgroundProcessingOptions> options;

    public PlaylistBackgroundWatcher(ILogger<PlaylistBackgroundWatcher> logger, IServiceScopeFactory scopeFactory, IOptions<BackgroundProcessingOptions> options)
    {
        this.logger = logger;
        this.scopeFactory = scopeFactory;
        this.options = options;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("{ServiceName} started.", nameof(PlaylistBackgroundWatcher));

        await CheckPlaylistsAsync(true, stoppingToken);
        await CheckPlaylistsAsync(false, stoppingToken);

        using var timer = new PeriodicTimer(options.Value.WatchInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await CheckPlaylistsAsync(false, stoppingToken);
        }
    }

    private async Task CheckPlaylistsAsync(bool isProcessing, CancellationToken cancel)
    {
        using var scope = scopeFactory.CreateScope();

        var taskDistributor = scope.ServiceProvider.GetRequiredService<QueryTasksDistributor>();
        var updateChecker = scope.ServiceProvider.GetRequiredService<PlaylistUpdateChecker>();
        var playlistsManager = scope.ServiceProvider.GetRequiredService<SourcePlaylistStateManager>();
        var queriesManager = scope.ServiceProvider.GetRequiredService<QueryStateManager>();

        var updateTask = new Dictionary<string, IPlaylistInfo>();
        var privatePlaylists = new List<string>();
        var publicPlaylists = new List<string>();
        var unauthorizedUsers = new List<string>();

        await foreach (PlaylistInfoDiff playlist in updateChecker.FetchPendingPlaylistsAsync(isProcessing, cancel))
        {
            if (playlist.IsPrivateChanged && playlist.IsPrivate)
                privatePlaylists.Add(playlist.Id);
            else if (playlist.IsPrivateChanged && !playlist.IsPrivate)
                publicPlaylists.Add(playlist.Id);

            updateTask.Add(playlist.Id, playlist);
        }

        if (updateTask.Count == 0) return;

        await playlistsManager.ExecuteIsPrivateUpdateAsync(privatePlaylists, true, cancel);
        await playlistsManager.ExecuteIsPrivateUpdateAsync(publicPlaylists, false, cancel);
        await queriesManager.ExecuteReadStatusUpdateAsync(publicPlaylists, true, cancel);
        await queriesManager.ExecuteReadStatusUpdateAsync(privatePlaylists, false, cancel);

        await taskDistributor.CreateTaskAsync(updateTask, cancel);
    }
}