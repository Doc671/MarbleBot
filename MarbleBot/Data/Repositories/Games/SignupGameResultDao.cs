using MarbleBot.Entities.Games;
using MarbleBot.Services.Games;
using Microsoft.EntityFrameworkCore;

namespace MarbleBot.Data.Repositories.Games;

public class SignupGameResultDao
{
    private readonly MarbleBotDbContext _dbContext;

    public SignupGameResultDao(MarbleBotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SignupGameResultContainer> GetOrCreateResultContainerAsync(ulong channelId, string marbleName, GameType gameType)
    {
        SignupGameResultContainer? winner = await GetResultContainerAsync(channelId, marbleName, gameType);

        if (winner == null)
        {
            winner = new SignupGameResultContainer()
            {
                ChannelId = channelId,
                MarbleName = marbleName,
                UpperMarbleName = marbleName.ToUpperInvariant(),
                GameType = gameType
            };

            _dbContext.SignupGameResults.Add(winner);
            await _dbContext.SaveChangesAsync();
        }

        return winner;
    }

    public async Task<SignupGameResultContainer?> GetResultContainerAsync(ulong channelId, string marbleName, GameType gameType)
    {
        string upperMarbleName = marbleName.ToUpperInvariant();
        return await _dbContext.SignupGameResults
            .FirstOrDefaultAsync(winner => winner.ChannelId == channelId 
                && winner.GameType == gameType 
                && winner.UpperMarbleName == upperMarbleName);
    }

    public async Task<IReadOnlyList<LeaderboardEntry>> GetUsesAsync(GameType gameType, int page, int itemsPerPage)
    {
        var orderedResults = _dbContext.SignupGameResults
            .Where(winner => winner.GameType == gameType)
            .GroupBy(winner => winner.UpperMarbleName)
            .Select(group => new LeaderboardEntry
            {
                MarbleName = group.Key,
                Uses = group.Sum(winner => winner.Uses),
                Wins = group.Sum(winner => winner.Wins)
            })
            .OrderByDescending(result => result.Uses);

        return await AssignUsePlaces(orderedResults)
            .Skip((page - 1) * itemsPerPage)
            .Take(itemsPerPage)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<LeaderboardEntry>> GetUsesAsync(ulong channelId, GameType gameType, int page, int itemsPerPage)
    {
        var orderedResults = _dbContext.SignupGameResults
            .Where(winner => winner.ChannelId == channelId && winner.GameType == gameType)
            .GroupBy(winner => winner.UpperMarbleName)
            .Select(group => new LeaderboardEntry
            {
                MarbleName = group.Key,
                Uses = group.Sum(winner => winner.Uses),
                Wins = group.Sum(winner => winner.Wins)
            })
            .OrderByDescending(result => result.Uses);

        return await AssignUsePlaces(orderedResults)
            .Skip((page - 1) * itemsPerPage)
            .Take(itemsPerPage)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<LeaderboardEntry>> GetWinsAsync(GameType gameType, int page, int itemsPerPage)
    {
        var orderedResults = _dbContext.SignupGameResults
            .Where(winner => winner.GameType == gameType)
            .GroupBy(winner => winner.UpperMarbleName)
            .Select(group => new LeaderboardEntry
            {
                MarbleName = group.Key,
                Uses = group.Sum(winner => winner.Uses),
                Wins = group.Sum(winner => winner.Wins)
            })
            .OrderByDescending(result => result.Wins);

        return await AssignWinPlaces(orderedResults)
            .Skip((page - 1) * itemsPerPage)
            .Take(itemsPerPage)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<LeaderboardEntry>> GetWinsAsync(ulong channelId, GameType gameType, int page, int itemsPerPage)
    {
        var orderedResults = _dbContext.SignupGameResults
            .Where(winner => winner.ChannelId == channelId && winner.GameType == gameType)
            .GroupBy(winner => winner.UpperMarbleName)
            .Select(group => new LeaderboardEntry
            {
                MarbleName = group.Key,
                Uses = group.Sum(winner => winner.Uses),
                Wins = group.Sum(winner => winner.Wins)
            })
            .OrderByDescending(result => result.Wins);

        return await AssignWinPlaces(orderedResults)
            .Skip((page - 1) * itemsPerPage)
            .Take(itemsPerPage)
            .ToListAsync();
    }

    private static async IAsyncEnumerable<LeaderboardEntry> AssignUsePlaces(IOrderedQueryable<LeaderboardEntry> sortedEntries)
    {
        int currentPlace = 0;
        int previousUses = -1;
        foreach (LeaderboardEntry entry in sortedEntries)
        {
            if (entry.Uses != previousUses)
            {
                currentPlace++;
            }

            entry.Place = currentPlace;
            previousUses = entry.Uses;

            yield return entry;
        }
    }

    private static async IAsyncEnumerable<LeaderboardEntry> AssignWinPlaces(IOrderedQueryable<LeaderboardEntry> sortedEntries)
    {
        int currentPlace = 0;
        int previousWins = -1;
        foreach (LeaderboardEntry entry in sortedEntries)
        {
            if (entry.Wins != previousWins)
            {
                currentPlace++;
            }

            entry.Place = currentPlace;
            previousWins = entry.Wins;

            yield return entry;
        }
    }

    public async Task IncrementWinsAsync(ulong channelId, string marbleName, GameType gameType)
    {
        SignupGameResultContainer winner = await GetOrCreateResultContainerAsync(channelId, marbleName, gameType);

        winner.Wins++;

        await _dbContext.SaveChangesAsync();
    }

    public async Task IncrementTotalGamesAsync(ulong channelId, string marbleName, GameType gameType)
    {
        SignupGameResultContainer winner = await GetOrCreateResultContainerAsync(channelId, marbleName, gameType);

        winner.Uses++;

        await _dbContext.SaveChangesAsync();
    }
}
