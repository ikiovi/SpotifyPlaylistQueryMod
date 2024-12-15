using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using AspNet.Security.OAuth.Spotify;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SpotifyAPI.Web;
using SpotifyPlaylistQueryMod.Managers;
using SpotifyPlaylistQueryMod.Models.Entities;
using SpotifyPlaylistQueryMod.Spotify.Configuration;
using SpotifyPlaylistQueryMod.Spotify.Exceptions;
using SpotifyPlaylistQueryMod.Spotify.Exceptions.Base;
using SpotifyPlaylistQueryMod.Web.Extensions;

namespace SpotifyPlaylistQueryMod.Spotify.Services;

public sealed class SpotifyTokenManager
{
    private readonly IMemoryCache memoryCache;
    private readonly IServiceScopeFactory scopeFactory;
    private readonly IOptions<SpotifyClientOptions> options;
    private readonly SpotifyClientConfig clientConfig;

    public SpotifyTokenManager(IMemoryCache memoryCache, IServiceScopeFactory scopeFactory, IOptions<SpotifyClientOptions> options, SpotifyClientConfig clientConfig)
    {
        this.memoryCache = memoryCache;
        this.options = options;
        this.scopeFactory = scopeFactory;
        this.clientConfig = clientConfig;
    }

    public Task<string> GetUserAccessTokenAsync(string userId)
    {
        if (TryGetAccessToken(userId, out var accessToken)) return Task.FromResult(accessToken);
        return RefreshUserAccessTokenAsync(userId);
    }

    public async Task<string> RefreshUserAccessTokenAsync(string userId)
    {
        using var scope = scopeFactory.CreateScope();
        var usersManager = scope.ServiceProvider.GetRequiredService<UsersManager>();

        User user = await usersManager.GetAuthenticatedUserAsync(userId);

        var request = new AuthorizationCodeRefreshRequest(
            options.Value.ClientId,
            options.Value.ClientSecret,
            user.RefreshToken
        );

        AuthorizationCodeRefreshResponse response;

        try
        {
            response = await new OAuthClient(clientConfig).RequestToken(request);
        }
        catch (APIException ex) when (SpotifyInvalidGrantException.CanCreate(ex))
        {
            Debug.Assert(ex.Response != null);
            await usersManager.MarkUserAsUnauthorizedAsync(user);
            throw new SpotifyInvalidGrantException(ex.Response) { UserId = userId };
        }

        DateTimeOffset nextRefresh = user.NextRefresh;

        if (response.RefreshToken is not string refreshToken)
            return SetAccessToken(userId, response.AccessToken, nextRefresh);

        try
        {
            nextRefresh = DateTimeOffset.UtcNow.AddSeconds(response.ExpiresIn);
            await usersManager.RefreshAsync(user, refreshToken, nextRefresh);
            return SetAccessToken(userId, response.AccessToken, nextRefresh);
        }
        catch (DBConcurrencyException ex)
        {
            if (TryGetAccessToken(userId, out var accessToken)) return accessToken;
            throw new SpotifyAuthenticationFailureException("Failed to retrieve access token.", ex) { UserId = userId };
        }
    }

    private string SetAccessToken(string userId, string accessToken, DateTimeOffset nextRefresh)
    {
        return memoryCache.Set(AccessTokenCacheKey(userId), accessToken, nextRefresh);
    }

    private bool TryGetAccessToken(string userId, [NotNullWhen(true)] out string? accessToken)
    {
        return memoryCache.TryGetValue(AccessTokenCacheKey(userId), out accessToken) && !string.IsNullOrWhiteSpace(accessToken);
    }

    public static string AccessTokenCacheKey(string userId) =>
        $"{SpotifyAuthenticationDefaults.Issuer}AccessToken-{userId}";

    public static async Task OnCreatingTicket(OAuthCreatingTicketContext ctx)
    {
        if (DateTimeOffset.UtcNow + ctx.ExpiresIn is not DateTimeOffset nextRefresh)
        {
            ctx.Fail(new InvalidOperationException());
            return;
        }

        string? id = ctx.Principal?.GetCurrentUserId();

        if (string.IsNullOrWhiteSpace(id) ||
            string.IsNullOrWhiteSpace(ctx.RefreshToken) ||
            string.IsNullOrWhiteSpace(ctx.AccessToken)) ctx.Fail(new InvalidOperationException());

        var userManager = ctx.HttpContext.RequestServices.GetRequiredService<UsersManager>();
        var memoryCache = ctx.HttpContext.RequestServices.GetRequiredService<IMemoryCache>();

        try
        {
            User user = await userManager.CreateOrUpdateAsync(id, ctx.RefreshToken, nextRefresh);
            ctx.Identity?.AddClaim(new(ClaimTypes.Role, user.Privileges.ToString()));
        }
        catch (InvalidOperationException ex)
        {
            ctx.Fail(ex);
            return;
        }
        memoryCache.Set(AccessTokenCacheKey(id), ctx.AccessToken, nextRefresh);
    }
}
