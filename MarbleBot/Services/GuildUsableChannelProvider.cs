using MarbleBot.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace MarbleBot.Services;

public class GuildUsableChannelProvider
{
    private readonly IServiceScopeFactory _scopeFactory;

    private readonly HashSet<(ulong, ulong)> _guildChannelPairs = [];

    public GuildUsableChannelProvider(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<bool> IsChannelUsable(IInteractionContext context)
    {
        if (context.Channel.ChannelType is ChannelType.DM or ChannelType.Group)
        {
            return true;
        }

        ulong guildId = context.Guild.Id;
        ulong channelId = context.Channel.Id;
        if (_guildChannelPairs.Contains((guildId, channelId)))
        {
            return true;
        }

        using var scope = _scopeFactory.CreateScope();
        var guildDao = scope.ServiceProvider.GetRequiredService<GuildDao>();
        bool usable = await guildDao.IsChannelUsable(guildId, channelId);

        if (usable)
        {
            _guildChannelPairs.Add((guildId, channelId));
        }

        return usable;
    }

    public void RemovePair(ulong guildId, ulong channelId)
    {
        if (_guildChannelPairs.Contains((guildId, channelId)))
        {
            _guildChannelPairs.Remove((guildId, channelId));
        }
    }
}
