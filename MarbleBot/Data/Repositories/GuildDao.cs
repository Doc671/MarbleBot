using MarbleBot.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarbleBot.Data.Repositories;

public class GuildDao
{
    private readonly MarbleBotDbContext _dbContext;

    public GuildDao(MarbleBotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guild?> GetGuildAsync(ulong id, bool includeChannels = false, CancellationToken cancellationToken = default)
    {
        if (includeChannels)
        {
            return await _dbContext.Guilds
                .Include(guild => guild.UsableChannels)
                .FirstOrDefaultAsync(guild => guild.Id == id, cancellationToken);
        }
        else
        {
            return await _dbContext.Guilds
                .FirstOrDefaultAsync(guild => guild.Id == id, cancellationToken);
        }
    }

    public async Task<Guild> GetOrCreateGuildAsync(ulong id, CancellationToken cancellationToken = default)
    {
        Guild? guild = await GetGuildAsync(id, true, cancellationToken);
        if (guild != null)
        {
            return guild;
        }

        guild = new Guild
        { 
            Id = id 
        };

        _dbContext.Guilds.Add(guild);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return guild;
    }

    public async Task<Color> GetColorAsync(ulong id, CancellationToken cancellationToken = default)
    {
        uint? color = await _dbContext.Guilds
            .AsNoTracking()
            .Where(guild => guild.Id == id)
            .Select(guild => (uint?)guild.Color)
            .SingleOrDefaultAsync(cancellationToken);

        return color ?? 0;
    }

    public async Task UpdateColorAsync(ulong id, uint color, CancellationToken cancellationToken = default)
    {
        Guild guild = await GetOrCreateGuildAsync(id, cancellationToken);
        guild.Color = color;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddUsableChannelAsync(ulong guildId, ulong channelId, CancellationToken cancellationToken = default)
    {
        if (await IsChannelUsable(guildId, channelId, cancellationToken))
        {
            return;
        }

        await GetOrCreateGuildAsync(guildId, cancellationToken);

        var guildChannel = new GuildUsableChannel
        {
            GuildId = guildId,
            ChannelId = channelId
        };

        _dbContext.GuildUsableChannels.Add(guildChannel);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> IsChannelUsable(ulong guildId, ulong channelId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GuildUsableChannels
            .AnyAsync(guildChannel => guildChannel.GuildId == guildId && guildChannel.ChannelId == channelId, cancellationToken);
    }

    public async Task<bool> TryRemoveUsableChannelAsync(ulong guildId, ulong channelId, CancellationToken cancellationToken = default)
    {
        GuildUsableChannel? guildChannel = await _dbContext.GuildUsableChannels
            .FirstOrDefaultAsync(guildChannel => guildChannel.GuildId == guildId && guildChannel.ChannelId == channelId, cancellationToken);

        if (guildChannel == null)
        {
            return false;
        }

        _dbContext.GuildUsableChannels.Remove(guildChannel);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
