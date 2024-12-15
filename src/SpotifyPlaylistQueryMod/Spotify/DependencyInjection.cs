using Microsoft.Extensions.Options;
using SpotifyAPI.Web;
using SpotifyPlaylistQueryMod.Spotify.Configuration;
using SpotifyPlaylistQueryMod.Spotify.Services;

namespace SpotifyPlaylistQueryMod;

public static partial class DependencyInjection
{
    public static IConfiguration AddSpotifyOptions(this IConfiguration config, IServiceCollection services)
    {
        services.AddOptions<SpotifyClientOptions>()
            .Bind(config.GetRequiredSection(SpotifyClientOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return config;
    }

    public static IServiceCollection SetupSpotifyServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<SpotifyTokenManager>();
        services.AddSpotifyClientFactory(config);
        services.AddScoped<SpotifyTracksInserter>();

        return services;
    }

    public static IServiceCollection AddSpotifyClientFactory(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton(_ =>
            SpotifyClientConfig.CreateDefault().WithRetryHandler(new SimpleRetryHandler() { TooManyRequestsConsumesARetry = true }));

        services.AddSingleton<ISpotifyClient>(sp =>
        {
            var spotifyConfig = sp.GetRequiredService<SpotifyClientConfig>();
            var spotifyClientOptions = sp.GetRequiredService<IOptions<SpotifyClientOptions>>().Value;
            var authenticator = new ClientCredentialsAuthenticator(spotifyClientOptions.ClientId, spotifyClientOptions.ClientSecret);
            return new SpotifyClient(spotifyConfig.WithAuthenticator(authenticator));
        });

        services.AddSingleton<ISpotifyClientFactory, SpotifyClientFactory>();

        return services;
    }
}
