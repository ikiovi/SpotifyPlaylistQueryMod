using System.Collections.Concurrent;

namespace SpotifyPlaylistQueryMod.Background.Services.Processing;

public sealed class PlaylistQueriesTracker
{
    private readonly ConcurrentDictionary<string, int> queries = [];

    public bool Set(string playlistId, int queriesCount) =>
        queries.TryAdd(playlistId, queriesCount);

    public bool Decrement(string playlistId)
    {
        if (!queries.TryGetValue(playlistId, out var remainingQueries))
            return false;

        var newValue = remainingQueries - 1;

        if (newValue == 0)
        {
            queries.TryRemove(playlistId, out _);
            return true;
        }

        queries.TryUpdate(playlistId, newValue, remainingQueries);
        return false;
    }
}
