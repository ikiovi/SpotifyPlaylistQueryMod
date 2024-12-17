using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;
using SpotifyPlaylistQueryMod.Background.Configuration;
using SpotifyPlaylistQueryMod.Background.Managers;
using SpotifyPlaylistQueryMod.Background.Services;
using SpotifyPlaylistQueryMod.Background.Services.Cache;
using SpotifyPlaylistQueryMod.Background.Services.Processing;
using SpotifyPlaylistQueryMod.Background.Services.Queue;
using SpotifyPlaylistQueryMod.Background.Services.Watcher;
using SpotifyPlaylistQueryMod.Background.Services.Worker;
using SpotifyPlaylistQueryMod.Models.Entities;

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
        services.AddSingleton<ITaskQueue<PlaylistQueryState>>(_ =>
        {
            var workersCount = 1;
            return new ChannelBasedTaskQueue<PlaylistQueryState>(workersCount);
        });
        services.AddSingleton<PlaylistQueriesTracker>();
        services.AddSingleton<PlaylistsInfoCache>();

        services.AddScoped<PlaylistUpdateChecker>();
        services.AddTransient<QueryExecuteService>();

        //TODO: Сonfiguration
        IAsyncPolicy<HttpResponseMessage> retry = HttpPolicyExtensions
            .HandleTransientHttpError()
            .Or<TimeoutRejectedException>()
            .WaitAndRetryAsync(2, _ => TimeSpan.FromSeconds(5));
        IAsyncPolicy<HttpResponseMessage> timeout = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(15));

        services.AddHttpClient<QueryExecuteService>()
        .SetHandlerLifetime(TimeSpan.FromMinutes(5))
        .AddPolicyHandler(Policy.WrapAsync(timeout, retry));

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
