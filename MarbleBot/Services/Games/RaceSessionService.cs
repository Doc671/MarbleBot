using MarbleBot.Data.Repositories.Games;
using MarbleBot.Entities.Games;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

namespace MarbleBot.Services.Games;

public class RaceSessionService
{
    private readonly ConcurrentDictionary<ulong, RaceSession> _sessions = [];

    private readonly RaceEngine _raceEngine;
    private readonly IServiceScopeFactory _scopeFactory;

    public RaceSessionService(RaceEngine raceEngine, IServiceScopeFactory scopeFactory)
    {
        _raceEngine = raceEngine;
        _scopeFactory = scopeFactory;
    }

    public async Task<RaceSession> GetOrCreateSessionAsync(ulong channelId, int marbleLimit)
    {
        if (_sessions.TryGetValue(channelId, out RaceSession? existingSession))
        {
            return existingSession;
        }

        using IServiceScope scope = _scopeFactory.CreateScope();
        var signupGroupDao = scope.ServiceProvider.GetRequiredService<SignupGroupDao>();
        SignupGroup? group = await signupGroupDao.GetSignupGroupAsync(channelId, GameType.Race);

        var newSession = new RaceSession(_raceEngine, group?.Entries ?? [], marbleLimit);
        _sessions.AddOrUpdate(channelId, newSession, (_, _) => newSession);

        return newSession;
    }

    public async Task<RaceSession?> GetSessionAsync(ulong channelId)
    {
        if (_sessions.TryGetValue(channelId, out RaceSession? existingSession))
        {
            return existingSession;
        }

        using IServiceScope scope = _scopeFactory.CreateScope();
        var signupGroupDao = scope.ServiceProvider.GetRequiredService<SignupGroupDao>();
        SignupGroup? group = await signupGroupDao.GetSignupGroupAsync(channelId, GameType.Race);

        if (group == null)
        {
            return null;
        }

        var newSession = new RaceSession(_raceEngine, group.Entries, 10);
        _sessions.AddOrUpdate(channelId, newSession, (_, _) => newSession);

        return newSession;
    }

    public async Task<IReadOnlyList<SignupEntry>?> TryGetEntriesAsync(ulong channelId)
    {
        RaceSession? session = await GetSessionAsync(channelId);
        if (session == null) 
        {
            return null;
        }

        return session.GetEntries();
    }

    public async Task<bool> TryAddEntryAsync(ulong channelId, ulong userId, string marbleName)
    {
        const int marbleLimit = 10;
        RaceSession session = await GetOrCreateSessionAsync(channelId, marbleLimit);
        bool success = session.TryAddEntry(userId, marbleName);
        if (!success)
        {
            return false;
        }

        using IServiceScope scope = _scopeFactory.CreateScope();
        var signupDao = scope.ServiceProvider.GetRequiredService<SignupGroupDao>();

        SignupGroup signupGroup = await signupDao.GetOrCreateSignupGroupAsync(channelId, GameType.Race);
        await signupDao.AddSignupEntryAsync(signupGroup.Id, userId, GameType.Race, marbleName);

        return true;
    }

    public async Task<bool> TryRemoveEntryAsync(ulong channelId, ulong userId, string marbleName)
    {
        RaceSession? session = await GetSessionAsync(channelId);
        if (session == null)
        {
            return false;
        }

        using IServiceScope scope = _scopeFactory.CreateScope();
        var signupDao = scope.ServiceProvider.GetRequiredService<SignupGroupDao>();

        bool success = session.TryRemoveEntry(userId, marbleName);
        success &= await signupDao.TryRemoveSignupGroupAsync(channelId, GameType.Race);
        return success;
    }

    public async IAsyncEnumerable<RaceResult> RunAsync(ulong channelId)
    {
        if (!_sessions.TryGetValue(channelId, out var session))
        {
            yield break;
        }

        var results = new List<RaceResult>();
        try
        {
            await foreach (RaceResult result in session.RunAsync())
            {
                results.Add(result);
                yield return result;
            }
        }
        finally
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            var resultDao = scope.ServiceProvider.GetRequiredService<SignupGameResultDao>();

            string[] marbleNames = results.Select(result => result.MarbleName).ToArray();

            foreach (string marbleName in marbleNames)
            {
                await resultDao.IncrementTotalGamesAsync(channelId, marbleName, GameType.Race);
            }

            await resultDao.IncrementWinsAsync(channelId, marbleNames.Last(), GameType.Race);

            _sessions.TryRemove(channelId, out _);
        }
    }
}
