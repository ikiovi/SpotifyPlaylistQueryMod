using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;
using SpotifyPlaylistQueryMod.Background.Configuration;
using SpotifyPlaylistQueryMod.Background.Managers;
using SpotifyPlaylistQueryMod.Background.Models;
using SpotifyPlaylistQueryMod.Background.Services;
using SpotifyPlaylistQueryMod.Background.Services.Cache;
using SpotifyPlaylistQueryMod.Background.Services.Processing;
using SpotifyPlaylistQueryMod.Background.Services.Queue;
using SpotifyPlaylistQueryMod.Background.Services.Watcher;
using SpotifyPlaylistQueryMod.Background.Services.Worker;

namespace SpotifyPlaylistQueryMod;

public static partial class DependencyInjection
{
    public static IConfiguration AddBackgroundProcessingOptions(this IConfiguration config, IServiceCollection services)
    {
        services.AddOptions<BackgroundProcessingOptions>()
            .Bind(config.GetRequiredSection(BackgroundProcessingOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return config;
    }

    public static IServiceCollection SetupBackgroundServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<ITaskQueue<ProcessingQueryState>>(_ =>
        {
            var workersCount = 1;
            return new ChannelBasedTaskQueue<ProcessingQueryState>(workersCount);
        });
        services.AddSingleton<PlaylistQueriesTracker>();
        services.AddSingleton<PlaylistsInfoCache>();

        services.AddScoped<PlaylistUpdateChecker>();
        services.AddTransient<QueryExecuteService>();

        //TODO: Сonfiguration
        services.AddHttpClient<QueryExecuteService>()
        .SetHandlerLifetime(TimeSpan.FromMinutes(5))
        .AddPolicyHandler(HttpPolicyExtensions
            .HandleTransientHttpError()
            .Or<TimeoutRejectedException>()
            .WaitAndRetryAsync(2, _ => TimeSpan.FromSeconds(5))
        );

        services.AddBackgroundDataManagers(config);

        services.AddScoped<PlaylistProcessingService>();
        services.AddScoped<PlaylistsTracksService>();
        services.AddScoped<QueryTasksDistributor>();

        services.AddHostedService<PlaylistBackgroundWatcher>();
        services.AddHostedService<PlaylistQueryBackgroundWorker>();

        return services;
    }

    public static IServiceCollection AddBackgroundDataManagers(this IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<SourcePlaylistStateManager>();
        services.AddScoped<QueryStateManager>();
        return services;
    }
}
