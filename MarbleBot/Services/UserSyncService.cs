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
        _client.UserUpdated += OnUserUpdated;
        return Task.CompletedTask;
    }

    private async Task OnUserUpdated(SocketUser arg1, SocketUser arg2)
    {
        ulong userId = arg1.Id;
        if (_userCache.TryGetValue(userId, out var cachedUser) && cachedUser.Username == arg2.Username && cachedUser.GlobalName == arg2.GlobalName)
        {
            return;
        }

        if (arg1.Username == arg2.Username && arg1.GlobalName == arg2.GlobalName)
        {
            return;
        }

        using IServiceScope scope = _scopeFactory.CreateScope();
        var userDao = scope.ServiceProvider.GetRequiredService<UserDao>();
        if (!await userDao.ContainsAsync(userId))
        {
            return;
        }

        _userCache[userId] = (arg2.Username, arg2.GlobalName);

        _logger.LogInformation("User {UserId} updated: Username changed from {OldUsername} to {NewUsername}, GlobalName changed from {OldGlobalName} to {NewGlobalName}",
            userId, cachedUser.Username, arg2.Username, cachedUser.GlobalName, arg2.GlobalName);

        await userDao.UpdateNamesAsync(userId, arg2.Username, arg2.GlobalName);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
