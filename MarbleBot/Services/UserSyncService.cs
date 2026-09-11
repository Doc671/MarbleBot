using MarbleBot.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MarbleBot.Services;

public class UserSyncService : IHostedService
{
    private readonly DiscordSocketClient _client;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UserSyncService> _logger;

    private readonly Dictionary<ulong, (string? Username, string? GlobalName)> _userCache = new();

    public UserSyncService(DiscordSocketClient client, IServiceScopeFactory scopeFactory, ILogger<UserSyncService> logger)
    {
        _client = client;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _client.UserUpdated += async (oldUser, newUser) =>
        {
            if (_userCache.TryGetValue(newUser.Id, out var cachedUser) && cachedUser.Username == newUser.Username && cachedUser.GlobalName == newUser.GlobalName)
            {
                return;
            }

            if (oldUser.Username == newUser.Username && oldUser.GlobalName == newUser.GlobalName)
            {
                return;
            }

            await OnUserUpdated(newUser.Id, oldUser.Username, oldUser.GlobalName, newUser.Username, newUser.GlobalName);
        };

        return Task.CompletedTask;
    }

    public void RegisterUsernameChanges(ulong userId, string? username, string? globalName)
    {
        if (_userCache.TryGetValue(userId, out var cachedUser) && cachedUser.Username == username && cachedUser.GlobalName == globalName)
        {
            return;
        }

        _userCache[userId] = (username, globalName);
        _ = OnUserUpdated(userId, cachedUser.Username, cachedUser.GlobalName, username, globalName);
    }

    private async Task OnUserUpdated(ulong userId, string? oldUsername, string? oldGlobalName, string? newUsername, string? newGlobalName)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        var userDao = scope.ServiceProvider.GetRequiredService<UserDao>();
        if (!await userDao.ContainsAsync(userId))
        {
            return;
        }

        _userCache[userId] = (newUsername, newGlobalName);

        _logger.LogInformation("User {UserId} updated: Username changed from {OldUsername} to {NewUsername}, GlobalName changed from {OldGlobalName} to {NewGlobalName}",
            userId, oldUsername, newUsername, oldGlobalName, newGlobalName);

        await userDao.UpdateNamesAsync(userId, newUsername, newGlobalName);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
