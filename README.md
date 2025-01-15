# SpotifyPlaylistQueryMod

SpotifyPlaylistQueryMod is an ASP.NET Core application designed to simplify the creation of dynamic versions of Spotify playlists.  

This tool handles routine tasks, allowing you to focus on defining update logic by creating a custom Query API for processing changes.  

---

## Deployment

Before starting, you need to create a Spotify application. See the [official documentation](https://developer.spotify.com/documentation/web-api/tutorials/getting-started) for details.  

The application uses `docker compose` for deployment. For local setup, see the [Local Development](#local-development) section.  

To provide `ClientId` and `ClientSecret`, use a `docker-compose.override.yaml` file. More details about multiple Compose files are available in the [Docker documentation](https://docs.docker.com/compose/how-tos/multiple-compose-files/merge/).  

### Example

1. Clone the repository:
   ```bash
   git clone https://github.com/ikiovi/SpotifyPlaylistQueryMod.git
   cd SpotifyPlaylistQueryMod
   ```

2. Set Spotify API credentials in `docker-compose.override.yaml`:
   ```yaml
   services:
       spotify-pqm-backend:
           environment:
               - SpotifyOptions__ClientId=<your-client-id>
               - SpotifyOptions__ClientSecret=<your-client-secret>
   ```

3. **SSL Configuration**:  
   **The application doesn't manage SSL internally.**  \
   By default, it runs on `127.0.0.1:5001` without SSL. Use a **reverse proxy** (e.g., Caddy, Nginx) to manage SSL.


## Configuration

So far the number of configuration parameters is small.  

### Main Parameters

#### `SpotifyOptions`  
Defines credentials for Spotify API. Parameters are listed in [SpotifyClientOptions.cs](https://github.com/ikiovi/SpotifyPlaylistQueryMod/blob/main/src/SpotifyPlaylistQueryMod/Spotify/Configuration/SpotifyClientOptions.cs):  
- **`ClientId`** — Spotify client ID.  
- **`ClientSecret`** — Spotify client secret.

#### `ProcessingOptions`  
Configures background processing. Defined in [BackgroundProcessingOptions.cs](https://github.com/ikiovi/SpotifyPlaylistQueryMod/blob/main/src/SpotifyPlaylistQueryMod/Background/Configuration/BackgroundProcessingOptions.cs).

| Parameter                              | Description                                              | Default Value          |
|----------------------------------------|----------------------------------------------------------|------------------------|
| `ProcessingOptions__PlaylistNextCheckOffset` | Time interval to check for playlist updates              | `01:00:00` (1 hour)    |
| `ProcessingOptions__WatchInterval`            | Interval to scan for playlists requiring processing       | `00:05:00` (5 minutes) |

**Note**: Default values are in `appsettings.json`. To override them, use `docker-compose.override.yaml`:
```yaml
services:
    spotify-pqm-backend:
        environment:
            # <...>
            - "ProcessingOptions__PlaylistNextCheckOffset=00:30:00"
```

See [Microsoft's ASP.NET Core configuration documentation](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-8.0) for more details.


## Local Development

To run locally, you need Postgres and a Redis-compatible database (e.g., Valkey).  

Default connection strings are included in `appsettings.Development.json`. Update them as needed for your environment.

### Managing Spotify API Credentials

Use .NET User Secrets to securely store Spotify API credentials:
```bash
dotnet user-secrets set SpotifyOptions:ClientId <your-client-id>
dotnet user-secrets set SpotifyOptions:ClientSecret <your-client-secret>
```


## Contributions are welcome

For any questions or suggestions, feel free to open an issue.