using System.Data;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using Polly;
using SpotifyPlaylistQueryMod.Data;
using SpotifyPlaylistQueryMod.Data.Exceptions;
using SpotifyPlaylistQueryMod.Managers.Attributes;
using SpotifyPlaylistQueryMod.Models.Entities;
using SpotifyPlaylistQueryMod.Models.Enums;
using SpotifyPlaylistQueryMod.Spotify.Exceptions.Base;
using SpotifyPlaylistQueryMod.Shared.Enums;

namespace SpotifyPlaylistQueryMod.Managers;

public sealed class UsersManager
{
    private readonly ILogger<UsersManager> logger;
    private readonly ApplicationDbContext context;

    public UsersManager(ILogger<UsersManager> logger, ApplicationDbContext context)
    {
        this.context = context;
        this.logger = logger;
    }

    public async Task<User> GetAuthenticatedUserAsync(string userId)
    {
        User? user = await context.Users.FindAsync(userId);

        EntityNotFoundException.ThrowIfNullWithKey(user, userId);

        if (user.Status == UserStatus.AuthenticationRequired)
            throw new SpotifyAuthenticationFailureException("The user's token has been revoked or is invalid.", userId);

        if (user.Status < UserStatus.None)
            throw new InvalidOperationException($"Can't authenticate the user. Reason: {new { user.Status }}");

        return user;
    }

    public async Task MarkUserAsUnauthorizedAsync(User user, CancellationToken cancel = default)
    {
        try
        {
            user.Status = UserStatus.AuthenticationRequired;
            await context.SaveChangesAsync(cancel);
        }
        catch (DBConcurrencyException ex)
        {
            logger.LogWarning(ex, "Failed to change status of [{userId}]", user.Id);
        }
    }

    public async Task RefreshAsync(User user, string refreshToken, DateTimeOffset nextRefresh, CancellationToken cancel = default)
    {
        try
        {
            user.NextRefresh = nextRefresh;
            user.RefreshToken = refreshToken;
            await context.SaveChangesAsync(cancel);
        }
        catch (DBConcurrencyException ex)
        {
            logger.LogWarning(ex, "Token update failed for [{userId}]", user.Id);
            throw;
        }
    }

    public async Task<User> CreateOrUpdateAsync(string userId, string refreshToken, DateTimeOffset nextRefresh, CancellationToken cancel = default)
    {
        using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancel);
        User? user = await context.Users.FindAsync([userId], cancel);

        if (user?.Status < 0)
        {
            await transaction.RollbackAsync(cancel);
            throw new InvalidOperationException($"Can't update the user. Reason: {new { user.Status }}");
        }

        try
        {
            if (user is not null)
            {
                user.RefreshToken = refreshToken;
                user.NextRefresh = nextRefresh;
                await context.SaveChangesAsync(cancel);
            }
            user ??= await CreateAsync(userId, refreshToken, nextRefresh, cancel);
            await transaction.CommitAsync(cancel);
            return user;
        }
        catch
        {
            await transaction.RollbackAsync(cancel);
            throw;
        }
    }

    private async Task<User> CreateAsync(string userId, string refreshToken, DateTimeOffset nextRefresh, CancellationToken cancel = default)
    {
        User user = new()
        {
            Id = userId,
            RefreshToken = refreshToken,
            NextRefresh = nextRefresh,
            Privileges = UserPrivileges.None
        };
        await context.Users.AddAsync(user, cancel);
        await context.SaveChangesAsync(cancel);
        return user;
    }

    public Task<List<string>> GetValidCollaboratorsAsync(ICollection<string> users, CancellationToken cancel)
    {
        return context.Users
            .Where(u => users.Contains(u.Id))
            .Where(u => u.IsCollaborationEnabled)
            .Where(u => u.Status == UserStatus.None)
            .Select(u => u.Id)
            .ToListAsync(cancel);
    }
}