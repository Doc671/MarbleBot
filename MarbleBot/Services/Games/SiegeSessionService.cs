using MarbleBot.Data.Repositories.Games;
using MarbleBot.Entities;
using MarbleBot.Entities.Games;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

namespace MarbleBot.Services.Games;

public class SiegeSessionService
{
    private readonly ConcurrentDictionary<ulong, SiegeSession> _sessions = [];

    private readonly SiegeEngine _siegeEngine;
    private readonly IServiceScopeFactory _scopeFactory;

    public SiegeSessionService(SiegeEngine siegeEngine, IServiceScopeFactory scopeFactory)
    {
        _siegeEngine = siegeEngine;
        _scopeFactory = scopeFactory;
    }

    public async Task<SiegeSession> GetOrCreateSessionAsync(ulong channelId, int marbleLimit)
    {
        if (_sessions.TryGetValue(channelId, out SiegeSession? existingSession))
        {
            return existingSession;
        }

        using IServiceScope scope = _scopeFactory.CreateScope();
        var signupGroupDao = scope.ServiceProvider.GetRequiredService<SignupGroupDao>();
        SignupGroup? group = await signupGroupDao.GetSignupGroupAsync(channelId, GameType.Siege);

        var newSession = new SiegeSession(_siegeEngine, group?.Entries ?? [], marbleLimit);
        _sessions.AddOrUpdate(channelId, newSession, (_, _) => newSession);

        return newSession;
    }

    public async Task<SiegeSession?> GetSessionAsync(ulong channelId)
    {
        if (_sessions.TryGetValue(channelId, out SiegeSession? existingSession))
        {
            return existingSession;
        }

        using IServiceScope scope = _scopeFactory.CreateScope();
        var signupGroupDao = scope.ServiceProvider.GetRequiredService<SignupGroupDao>();
        SignupGroup? group = await signupGroupDao.GetSignupGroupAsync(channelId, GameType.Siege);
        if (group == null)
        {
            return null;
        }

        var newSession = new SiegeSession(_siegeEngine, group.Entries, 10);
        _sessions.AddOrUpdate(channelId, newSession, (_, _) => newSession);

        return newSession;
    }

    public async Task<IReadOnlyList<SignupEntry>?> TryGetEntriesAsync(ulong channelId)
    {
        SiegeSession? session = await GetSessionAsync(channelId);
        if (session == null)
        {
            return null;
        }

        return session.GetEntries();
    }

    public async Task<bool> TryAddEntryAsync(ulong channelId, User user, string marbleName)
    {
        const int marbleLimit = 10;
        SiegeSession session = await GetOrCreateSessionAsync(channelId, marbleLimit);
        bool success = session.TryAddEntry(user, marbleName);
        if (!success)
        {
            return false;
        }

        using IServiceScope scope = _scopeFactory.CreateScope();
        var signupDao = scope.ServiceProvider.GetRequiredService<SignupGroupDao>();

        SignupGroup signupGroup = await signupDao.GetOrCreateSignupGroupAsync(channelId, GameType.Siege);
        await signupDao.AddSignupEntryAsync(signupGroup.Id, user.Id, GameType.Siege, marbleName);

        return true;
    }

    public async Task<bool> TryRemoveEntryAsync(ulong channelId, ulong userId, string marbleName)
    {
        SiegeSession? session = await GetSessionAsync(channelId);
        if (session == null)
        {
            return false;
        }

        using IServiceScope scope = _scopeFactory.CreateScope();
        var signupDao = scope.ServiceProvider.GetRequiredService<SignupGroupDao>();

        bool success = session.TryRemoveEntry(userId, marbleName);
        success &= await signupDao.TryRemoveSignupGroupAsync(channelId, GameType.Siege);
        return success;
    }
}
