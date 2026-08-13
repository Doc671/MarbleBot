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

    public RaceSession GetOrCreateSession(ulong channelId, int marbleLimit)
    {
        return _sessions.GetOrAdd(channelId, _ => new RaceSession(_raceEngine, marbleLimit));
    }

    public RaceSession? GetSession(ulong channelId)
    {
        _sessions.TryGetValue(channelId, out var session);
        return session;
    }

    public async Task<IReadOnlyList<SignupEntry>?> TryGetEntriesAsync(ulong channelId)
    {
        RaceSession? session = GetSession(channelId);
        if (session == null) 
        {
            return null;
        }

        return session.GetEntries();
    }

    public async Task<bool> TryAddEntryAsync(ulong channelId, ulong userId, string marbleName)
    {
        const int marbleLimit = 10;
        RaceSession session = GetOrCreateSession(channelId, marbleLimit);
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
        RaceSession? session = GetSession(channelId);
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
