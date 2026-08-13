using MarbleBot.Entities.Games;
using MarbleBot.Services.Games;
using Microsoft.EntityFrameworkCore;

namespace MarbleBot.Data.Repositories.Games;

public class SignupGroupDao
{
    private readonly MarbleBotDbContext _dbContext;

    public SignupGroupDao(MarbleBotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SignupGroup?> GetSignupGroupAsync(ulong channelId, GameType gameType)
    {
        return await _dbContext.SignupGroups
            .Include(group => group.Entries)
            .FirstOrDefaultAsync(group => group.ChannelId == channelId && group.GameType == gameType);
    }

    public async Task<SignupGroup> GetOrCreateSignupGroupAsync(ulong channelId, GameType gameType)
    {
        var signupGroup = await GetSignupGroupAsync(channelId, gameType);
        if (signupGroup != null)
        {
            return signupGroup;
        }

        return await CreateSignupGroup(channelId, gameType);
    }

    public async Task<SignupGroup> CreateSignupGroup(ulong channelId, GameType gameType)
    {
        var signupGroup = new SignupGroup
        {
            ChannelId = channelId,
            GameType = gameType
        };

        _dbContext.SignupGroups.Add(signupGroup);
        await _dbContext.SaveChangesAsync();
        return signupGroup;
    }

    public async Task<bool> TryRemoveSignupGroupAsync(ulong channelId, GameType gameType)
    {
        SignupGroup? signupGroup = await GetSignupGroupAsync(channelId, gameType);
        if (signupGroup == null)
        {
            return false;
        }

        _dbContext.SignupGroups.Remove(signupGroup);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task AddSignupEntryAsync(int signupGroupId, ulong userId, GameType gameType, string marbleName)
    {
        var entry = new SignupEntry
        {
            SignupGroupId = signupGroupId,
            UserId = userId,
            GameType = gameType,
            MarbleName = marbleName
        };

        _dbContext.SignupEntries.Add(entry);
        await _dbContext.SaveChangesAsync();
    }
}
