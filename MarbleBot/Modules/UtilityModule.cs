using System.Diagnostics;
using System.Text;

namespace MarbleBot.Modules;

public class UtilityModule : MarbleBotSlashCommandModule
{
    [SlashCommand("about", "Displays information about the bot.")]
    public async Task AboutCommmand()
    {
        var builder = new EmbedBuilder()
            .WithDescription("MarbleBot has currently been brought back in a limited capacity, so certain commands may be missing.")
            .WithAuthor(Context.Client.CurrentUser)
            .AddField("What happened to all the economy commands (e.g. daily)?", "Daily rewards used to increase exponentially over time from 200 units of money to a maximum of 40000. While the level of inflation was funny, I don't want to punish people who don't keep daily streaks to that extent.", true)
            .AddField("What happened to all of my money and items?", "It was not possible to re-obtain the data from March 2021 (when the bot was last online), so money and items have been re-imported from a November 2020 backup. Balances are currently frozen for now due to the inflation that the daily command caused. It is possible that everyone's balances could get flattened in the future (and certain items removed) to combat the levels of inflation that the daily command caused.")
            .AddField("What happened to the Siege and War minigames?", "The bot has been brought back in a limited capacity for now. These minigames may return in the future.");

        await ReplyAsync(embed: builder.Build());
    }

    [SlashCommand("serverinfo", "Displays information about the current server.")]
    [RequireContext(ContextType.Guild)]
    public async Task ServerInfoCommand()
    {
        int botUserCount = 0;
        int onlineUserCount = 0;
        foreach (SocketGuildUser user in Context.Guild.Users)
        {
            if (user.IsBot)
            {
                botUserCount++;
            }

            if (user.Status != UserStatus.Offline)
            {
                onlineUserCount++;
            }
        }

        var builder = new EmbedBuilder()
            .WithThumbnailUrl(Context.Guild.IconUrl)
            .WithTitle(Context.Guild.Name)
            .AddField("Created At", Context.Guild.CreatedAt, true)
            .AddField("Text Channels", Context.Guild.TextChannels.Count, true)
            .AddField("Voice Channels", Context.Guild.VoiceChannels.Count, true)
            .AddField("Members", Context.Guild.Users.Count, true)
            .AddField("Bots", botUserCount, true)
            .AddField("Online", onlineUserCount, true)
            .AddField("Roles", Context.Guild.Roles.Count, true)
            .WithColor(Color.Blue)
            .WithFooter($"Server ID: {Context.Guild.Id}");

        if (Context.Guild.Owner != null)
        {
            builder.AddField("Owner", $"{Context.Guild.Owner.DisplayName} ({Context.Guild.Owner.GlobalName})");
        }

        await ReplyAsync(embed: builder.Build());
    }

    [SlashCommand("uptime", "Displays how long the bot has been running.")]
    public async Task UptimeCommand()
    {
        DateTime startTime = Process.GetCurrentProcess().StartTime.ToUniversalTime();

        long unixTimestamp = ((DateTimeOffset)startTime).ToUnixTimeSeconds();

        var embedBuilder = new EmbedBuilder()
            .WithTitle("MarbleBot Uptime")
            .WithDescription($"Online since: **<t:{unixTimestamp}:R>**\nUptime: **<t:{unixTimestamp}:R>**")
            .WithColor(Color.Blue);

        await RespondAsync(embed: embedBuilder.Build());
    }

    [SlashCommand("userinfo", "Displays information about a user.")]
    [RequireContext(ContextType.Guild)]
    public async Task UserInfoCommand(string username = "")
    {
        SocketGuildUser? user;
        if (string.IsNullOrEmpty(username))
        {
            user = (SocketGuildUser)Context.User;
        }
        else
        {
            user = Context.Guild.Users.FirstOrDefault(user => user.Username.Equals(username, StringComparison.InvariantCultureIgnoreCase)
                || user.DisplayName.Equals(username, StringComparison.InvariantCultureIgnoreCase)
                || (user.Nickname != null && user.Nickname.Equals(username, StringComparison.InvariantCultureIgnoreCase)));

            if (user == null)
            {
                await RespondWithErrorAsync("Could not find the requested user.");
                return;
            }
        }

        string nickname = string.IsNullOrEmpty(user.Nickname) ? "None" : user.Nickname;

        var roles = new StringBuilder();
        foreach (SocketRole role in user.Roles)
        {
            roles.AppendLine(role.Name);
        }

        var embedBuilder = new EmbedBuilder()
            .WithAuthor(user)
            .WithTitle($"{user.GlobalName}")
            .AddField("Nickname", nickname, true)
            .AddField("Account Created", $"<t:{user.CreatedAt.ToUnixTimeSeconds()}:R>", true)
            .AddField("Joined Server", $"<t:{user.JoinedAt?.ToUnixTimeSeconds()}:R>", true)
            .AddField("Roles", roles.Length > 0 ? roles.ToString() : "None")
            .WithColor(Color.Blue);

        await ReplyAsync(embed: embedBuilder.Build());
    }
}
