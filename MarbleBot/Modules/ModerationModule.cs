using MarbleBot.Data.Repositories;
using MarbleBot.Services;

namespace MarbleBot.Modules;

public class ModerationModule : MarbleBotSlashCommandModule
{
    private readonly GuildDao _guildDao;
    private readonly GuildColorProvider _guildColorProvider;
    private readonly GuildUsableChannelProvider _guildUsableChannelProvider;

    public ModerationModule(GuildDao guildDao, GuildColorProvider guildColorProvider, GuildUsableChannelProvider guildUsableChannelProvider)
    {
        _guildDao = guildDao;
        _guildColorProvider = guildColorProvider;
        _guildUsableChannelProvider = guildUsableChannelProvider;
    }

    [SlashCommand("addallowedchannel", "Allows users to use the bot in the given channel.")]
    [RequireContext(ContextType.Guild)]
    [RequireUserPermission(ChannelPermission.ManageChannels)]
    public async Task AddAllowedChannelCommand(SocketChannel channel)
    {
        await _guildDao.AddUsableChannelAsync(Context.Guild.Id, channel.Id);
        await RespondAsync("Successfully added channel to the list of channels where users are allowed to use the bot.", ephemeral: true);
    }

    [SlashCommand("removeallowedchannel", "Removes the ability users to use the bot in the given channel.")]
    [RequireContext(ContextType.Guild)]
    [RequireUserPermission(ChannelPermission.ManageChannels)]
    public async Task RemoveAllowedChannelCommand(SocketChannel channel)
    {
        if (await _guildDao.TryRemoveUsableChannelAsync(Context.Guild.Id, channel.Id))
        {
            await RespondAsync("Successfully added channel to the list of channels where users are allowed to use the bot.", ephemeral: true);
        }
        else
        {
            _guildUsableChannelProvider.RemovePair(Context.Guild.Id, Context.Channel.Id);
            await RespondWithErrorAsync("The given channel was not in the list of channels where users are allowed to use the bot.");
        }
    }

    [SlashCommand("setservercolor", "Sets the embed color for this server.")]
    [RequireContext(ContextType.Guild)]
    [RequireUserPermission(ChannelPermission.ManageMessages)]
    public async Task SetServerColorCommand(string hexCode)
    {
        uint color;
        if (hexCode.StartsWith('#'))
        {
            color = uint.Parse(hexCode[1..], System.Globalization.NumberStyles.HexNumber);
        }
        else
        {
            color = uint.Parse(hexCode, System.Globalization.NumberStyles.HexNumber);
        }

        _guildColorProvider.UpdateColor(Context.Guild.Id, color);

        await _guildDao.UpdateColorAsync(Context.Guild.Id, color);

        var embedBuilder = new EmbedBuilder()
            .WithColor(color)
            .WithDescription("Successfully updated server color.");

        await RespondAsync(embed: embedBuilder.Build());
    }
}
