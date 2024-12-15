using SpotifyAPI.Web;
using SpotifyPlaylistQueryMod.Background.Models;
using SpotifyPlaylistQueryMod.Models;
using SpotifyPlaylistQueryMod.Models.Entities;
using SpotifyPlaylistQueryMod.Managers;
using System.Diagnostics;
using SpotifyPlaylistQueryMod.Spotify.Models;

namespace SpotifyPlaylistQueryMod.Spotify.Services;

public sealed class SpotifyTracksInserter
{
    private readonly UsersManager usersManager;
    private readonly ISpotifyClientFactory clientFactory;

    public SpotifyTracksInserter(ISpotifyClientFactory clientFactory, UsersManager usersManager)
    {
        this.clientFactory = clientFactory;
        this.usersManager = usersManager;
    }

    public async Task ApplyPlaylistChangeResponseAsync(PlaylistChangeResponse response, CancellationToken cancel)
    {
        IPlaylistsClient baseClient = clientFactory.CreatePlaylistClient(response.UserId);

        await baseClient.RemoveTracksAsync(response.TargetId, response.OriginalResponse.Remove, cancel);

        if (response.OriginalResponse.Add.Count == 0) return;

        SpotifyPlaylistState state = await baseClient.GetStateAsync(response.TargetId, cancel);

        var total = state.TotalTracksCount;
        var position = CollaborativeInsertRequest.GetAbsolutePosition(total, response.OriginalResponse.Position);

        if (state.IsPrivate || !response.Add.Any())
        {
            await baseClient.AddTracksAsync(response.TargetId, response.OriginalResponse.Add, position, cancel);
            return;
        }

        Dictionary<string, int> collaborators = await GetValidCollaboratorsWithCountAsync(response.UserId, response.Add, cancel);

        if (collaborators.Count == 0)
        {
            await baseClient.AddTracksAsync(response.TargetId, response.OriginalResponse.Add, position, cancel);
            return;
        }

        Dictionary<string, IPlaylistsClient> clients = new() { { response.UserId, baseClient } };

        foreach (var collaborator in collaborators.Keys)
        {
            if (clients.ContainsKey(collaborator)) continue;
            IPlaylistsClient client = clientFactory.CreatePlaylistClient(collaborator);
            clients.Add(collaborator, client);
        }

        var request = new CollaborativeInsertRequest(response.TargetId, response.UserId, collaborators, response.Add.ToList())
        {
            InitialTracksCount = total,
            AbsolutePosition = position
        };

        await request.ApplyRequestAsync(clients, cancel);
    }


    private async Task<Dictionary<string, int>> GetValidCollaboratorsWithCountAsync(string userId, IEnumerable<ITrackInfo> tracks, CancellationToken cancel)
    {
        var users = tracks
            .GroupBy(t => t.AddedBy ?? userId)
            .ToDictionary(
                g => g.Key,
                g => g.Count()
            );

        HashSet<string> validUsers = [userId, .. await usersManager.GetValidCollaboratorsAsync(users.Keys, cancel)];

        if (validUsers.Count == 1) return [];

        var count = users
            .Where(kv => !validUsers.Contains(kv.Key))
            .Sum(kv => kv.Value);

        if (users.TryAdd(userId, count)) return users;

        users[userId] += count;
        return users;
    }
}