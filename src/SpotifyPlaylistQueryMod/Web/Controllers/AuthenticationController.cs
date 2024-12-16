using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace SpotifyPlaylistQueryMod.Web.Controllers;

[Route("api/Auth")]
[ApiController]
public sealed class AuthenticationController : ControllerBase
{
    private readonly ILogger<AuthenticationController> logger;

    public AuthenticationController(ILogger<AuthenticationController> logger) => this.logger = logger;

    [HttpGet("login")]
    public IActionResult Login(Uri? redirectUri)
    {
        if (redirectUri != null && redirectUri.Scheme != Uri.UriSchemeHttps && redirectUri.Scheme != Uri.UriSchemeHttp)
            return BadRequest("Only HTTP and HTTPS schemes are allowed for redirect URIs.");

        var uri = redirectUri?.ToString() ?? Request.Headers.Referer.ToString();
        logger.LogDebug("Logging in from {from}", Request.Headers.Host.ToString());
        return Challenge(new AuthenticationProperties { RedirectUri = uri });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        if (User?.Identity?.IsAuthenticated == true)
            await HttpContext.SignOutAsync();
        return NoContent();
    }
}
