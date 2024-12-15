using SpotifyPlaylistQueryMod;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

config.AddUserSecrets<Program>(true)
    .AddEnvironmentVariables();

config.AddSpotifyOptions(builder.Services)
    .AddBackgroundProcessingOptions(builder.Services);

builder.Services
    .SetupStorageServices(config)
    .SetupSpotifyServices(config)
    .SetupDataManagers(config)
    .SetupBackgroundServices(config)
    .SetupAPIServices(config);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.InitializeApplicationDatabaseAsync();
    await scope.ServiceProvider.InitializeDPDatabaseAsync();
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(b =>
{
    b.SetIsOriginAllowed(_ => true)
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowCredentials();
});

app.UseCookiePolicy(new CookiePolicyOptions
{
    MinimumSameSitePolicy = SameSiteMode.None,
    Secure = CookieSecurePolicy.SameAsRequest
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();