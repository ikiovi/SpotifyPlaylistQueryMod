using System.Diagnostics;
using AspNet.Security.OAuth.Spotify;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.HttpOverrides;
using SpotifyPlaylistQueryMod.Spotify.Configuration;
using SpotifyPlaylistQueryMod.Spotify.Services;
using SpotifyPlaylistQueryMod.Web;
using SpotifyPlaylistQueryMod.Web.Services;
using static SpotifyAPI.Web.Scopes;

namespace SpotifyPlaylistQueryMod;

public static partial class DependencyInjection
{
    public static IServiceCollection SetupAPIServices(this IServiceCollection services, IConfiguration config)
    {
        services.ConfigureExceptionHandlers(config);

        services.AddControllers();
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen();
        services.AddHttpContextAccessor();

        services.ConfigureAuthentication(config);
        services.ConfgureForwardedHeaders();

        services.AddCookieSessionStore(config);

        services.AddAuthorization();

        return services;
    }

    public static IServiceCollection ConfgureForwardedHeaders(this IServiceCollection services)
    {
        services.Configure<ForwardedHeadersOptions>(opts =>
        {
            opts.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
            opts.KnownProxies.Clear();
            opts.KnownNetworks.Clear();
        });

        return services;
    }
    public static IServiceCollection ConfigureExceptionHandlers(this IServiceCollection services, IConfiguration config)
    {
        services.AddExceptionHandler<AuthorizationExceptionHandler>();
        return services;
    }

    public static IServiceCollection AddCookieSessionStore(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<ITicketStore, CookieSessionStore>();

        services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
            .Configure<ITicketStore>((opts, store) => opts.SessionStore = store);
        return services;
    }

    public static IServiceCollection ConfigureAuthentication(this IServiceCollection services, IConfiguration config)
    {
        services.AddAuthentication(opts =>
        {
            opts.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            opts.DefaultChallengeScheme = SpotifyAuthenticationDefaults.AuthenticationScheme;
        })
        .AddCookie(opts =>
        {
            opts.ExpireTimeSpan = TimeSpan.FromDays(1); //TODO: Configuration
            opts.SlidingExpiration = true;
        })
        .AddSpotify(opts =>
        {
            var spotifyClientOptions = config
                .GetRequiredSection(SpotifyClientOptions.SectionName)
                .Get<SpotifyClientOptions>();

            Debug.Assert(spotifyClientOptions != null);

            opts.ClientId = spotifyClientOptions.ClientId;
            opts.ClientSecret = spotifyClientOptions.ClientSecret;
            opts.CallbackPath = "/callback";

            opts.CorrelationCookie.SameSite = SameSiteMode.Lax;
            opts.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            opts.CorrelationCookie.IsEssential = true;
            opts.CorrelationCookie.HttpOnly = true;
            opts.CorrelationCookie.Path = "/";

            opts.Scope.Add(PlaylistReadPrivate);
            opts.Scope.Add(PlaylistReadCollaborative);
            opts.Scope.Add(PlaylistModifyPublic);
            opts.Scope.Add(PlaylistModifyPrivate);

            opts.Events = new OAuthEvents
            {
                OnCreatingTicket = SpotifyTokenManager.OnCreatingTicket
            };
        });

        return services;
    }
}
