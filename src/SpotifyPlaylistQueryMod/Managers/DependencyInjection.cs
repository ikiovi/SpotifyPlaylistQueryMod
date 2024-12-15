using Microsoft.EntityFrameworkCore;
using Polly;
using SpotifyPlaylistQueryMod.Managers;
using SpotifyPlaylistQueryMod.Managers.Attributes;

namespace SpotifyPlaylistQueryMod;

public static partial class DependencyInjection
{
    public static IServiceCollection AddDatabaseConcurrencyRetryPolicy(this IServiceCollection services, IConfiguration config)
    {
        services.AddKeyedSingleton<IAsyncPolicy>(DbRetryPolicyAttribute.ServiceKey,
            (_, _) => Policy
                .Handle<DbUpdateConcurrencyException>()
                .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))) //TODO: Configuration
        );

        return services;
    }
    public static IServiceCollection SetupDataManagers(this IServiceCollection services, IConfiguration config)
    {
        AddDatabaseConcurrencyRetryPolicy(services, config);

        services.AddScoped<UsersManager>();
        services.AddScoped<PlaylistsManager>();
        services.AddScoped<QueriesManager>();
        services.AddScoped<TracksManager>();

        return services;
    }
}
