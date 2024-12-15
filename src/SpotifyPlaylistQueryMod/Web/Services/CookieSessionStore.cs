using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Distributed;

namespace SpotifyPlaylistQueryMod.Web.Services;

public class CookieSessionStore : ITicketStore
{
    private readonly IDistributedCache cache;

    public CookieSessionStore(IDistributedCache cache) => this.cache = cache;

    public Task RemoveAsync(string key) =>
        cache.RemoveAsync(AuthenticationTicketKey(key));

    public Task RenewAsync(string key, AuthenticationTicket ticket) =>
        SetAsync(key, ticket);

    public async Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        var value = await cache.GetAsync(AuthenticationTicketKey(key));
        if (value == null) return null;
        return TicketSerializer.Default.Deserialize(value);
    }

    public Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var key = Guid.NewGuid().ToString();
        return SetAsync(key, ticket);
    }

    private async Task<string> SetAsync(string key, AuthenticationTicket ticket)
    {
        var options = new DistributedCacheEntryOptions();
        if (ticket.Properties.ExpiresUtc is DateTimeOffset expiresIn)
            options.SetAbsoluteExpiration(expiresIn);

        var value = TicketSerializer.Default.Serialize(ticket);
        await cache.SetAsync(AuthenticationTicketKey(key), value, options);
        return key;
    }

    public static string AuthenticationTicketKey(string key) => $"AuthTicket-{key}";
}
