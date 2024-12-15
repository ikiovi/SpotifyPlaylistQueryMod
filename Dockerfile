FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
USER app
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY --link src/SpotifyPlaylistQueryMod/*.csproj SpotifyPlaylistQueryMod/
COPY --link src/SpotifyPlaylistQueryMod.Shared/**/**/*.csproj SpotifyPlaylistQueryMod.Shared/src/SpotifyPlaylistQueryMod.Shared/

RUN dotnet restore SpotifyPlaylistQueryMod/SpotifyPlaylistQueryMod.csproj

COPY --link src/SpotifyPlaylistQueryMod/ SpotifyPlaylistQueryMod/
COPY --link src/SpotifyPlaylistQueryMod.Shared/ SpotifyPlaylistQueryMod.Shared/


FROM build AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR /src/SpotifyPlaylistQueryMod
RUN dotnet publish ./SpotifyPlaylistQueryMod.csproj -c $BUILD_CONFIGURATION --no-restore -o /app


FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "SpotifyPlaylistQueryMod.dll"]