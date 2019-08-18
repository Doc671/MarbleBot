using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MarbleBot.Core;
using MarbleBot.Extensions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static MarbleBot.Global;

namespace MarbleBot.Modules
{
    /// <summary> Utility commands. </summary>
    public class Utility : MarbleBotModule
    {
        [Command("botinfo")]
        [Alias("info")]
        [Summary("Shows bot info.")]
        public async Task BotInfoCommandAsync()
            => await ReplyAsync(embed: new EmbedBuilder()
                .AddField("Daily Timeout", $"{DailyTimeout} hours", true)
                .AddField("Ongoing Scavenges", ScavengeInfo.Count, true)
                .AddField("Ongoing Sieges", SiegeInfo.Count, true)
                .AddField("Ongoing Wars", WarInfo.Count, true)
                .AddField("Servers", Servers.Value.Count, true)
                .AddField("Start Time (UTC)", StartTime.ToString("yyyy-MM-dd HH:mm:ss"), true)
                .AddField("Uptime", DateTime.UtcNow.Subtract(StartTime).ToString(), true)
                .WithAuthor(Context.Client.CurrentUser)
                .WithColor(GetColor(Context))
                .WithCurrentTimestamp()
                .WithFooter($"Requested by {Context.User.Username}#{Context.User.Discriminator}")
                .Build());

        [Command("checkearn")]
        [Alias("check", "checktimes")]
        [Summary("Shows the time remaining for each activity with a cooldown.")]
        public async Task CheckTimesCommandAsync()
        {
            var user = GetUser(Context);
            var timeUntilNextDaily = user.LastDaily.Subtract(DateTime.UtcNow.AddHours(-24));
            var timeUntilNextRace = user.LastRaceWin.Subtract(DateTime.UtcNow.AddHours(-6));
            var timeUntilNextScavenge = user.LastScavenge.Subtract(DateTime.UtcNow.AddHours(-6));
            var timeUntilNextSiege = user.LastSiegeWin.Subtract(DateTime.UtcNow.AddHours(-6));
            var timeUntilNextWar = user.LastWarWin.Subtract(DateTime.UtcNow.AddHours(-6));
            await ReplyAsync(embed: new EmbedBuilder()
                .AddField("Daily", timeUntilNextDaily.TotalHours > 24 ? "**Ready!**" : timeUntilNextDaily.ToString(), true)
                .AddField("Race", timeUntilNextRace.TotalHours > 6 ? "**Ready!**" : timeUntilNextRace.ToString(), true)
                .AddField("Scavenge", timeUntilNextScavenge.TotalHours > 6 ? "**Ready!**" : timeUntilNextScavenge.ToString(), true)
                .AddField("Siege", timeUntilNextSiege.TotalHours > 6 ? "**Ready!**" : timeUntilNextSiege.ToString(), true)
                .AddField("War", timeUntilNextWar.TotalHours > 6 ? "**Ready!**" : timeUntilNextWar.ToString(), true)
                .WithAuthor(Context.User)
                .WithColor(GetColor(Context))
                .WithCurrentTimestamp()
                .Build());
        }

        [Command("help")]
        [Alias("cmds")]
        [Summary("Gives the user help.")]
        public async Task HelpCommandAsync([Remainder] string command = "")
        {
            var builder = new EmbedBuilder()
                .WithCurrentTimestamp()
                .WithColor(GetColor(Context));
            switch (command.ToLower())
            {
                case "":
                    if ((Context.User as SocketGuildUser).GuildPermissions.ManageMessages)
                        builder.AddField("Modules", "Economy\nFun\nGames\nModeration\nRoles\nUtility\nYouTube");
                    else builder.AddField("Modules", "Economy\nFun\nGames\nRoles\nUtility\nYouTube");
                    builder.WithDescription("*by Doc671#1965*\n\nUse `mb/help` followed by the name of a module or a command for more info.")
                        .WithTitle("MarbleBot Help");
                    await ReplyAsync(embed: builder.Build());
                    break;
                case "economy":
                    var allCmds = new StringBuilder();
                    var commands = (IEnumerable<CommandInfo>)Global.CommandService.Commands.Where(c => string.Compare(c.Module.Name, command, true) == 0
                        && !c.Preconditions.Any(p => p is RequireOwnerAttribute)).OrderBy(c => c.Name);
                    if (Context.IsPrivate) commands = commands.Where(c => c.Remarks != "Not DMs" || c.Remarks != "CM Only");
                    else
                    {
                        if (Context.Guild.Id == CM) commands = commands.Where(c => c.Remarks != "Not CM");
                        else commands = commands.Where(c => c.Remarks != "CM Only");
                    }
                    if (GetUser(Context).Stage < 2) commands = commands.Where(c => c.Remarks != "Stage2");
                    foreach (var cmd in commands)
                    {
                        var name = cmd.Name.IsEmpty() ? "help" : cmd.Name;
                        allCmds.AppendLine($"**{name}** - {cmd.Summary}");
                    }
                    builder.AddField(
                            $"{System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(command)} Commands",
                            allCmds.ToString())
                        .WithTitle("MarbleBot Help");
                    await ReplyAsync(embed: builder.Build());
                    break;
                case "fun": goto case "economy";
                case "games":
                    await ReplyAsync(embed: builder
                        .AddField("Games commands", new StringBuilder()
                            .AppendLine("**race** - Participate in a marble race!")
                            .AppendLine("**scavenge** - Scavenge for items!")
                            .AppendLine("**siege** - Participate in a Marble Siege!")
                            .AppendLine("**war** - Participate in a Marble War!")
                            .ToString())
                        .WithDescription("*by Doc671#1965*")
                        .WithTitle("MarbleBot Help")
                        .Build());
                    break;
                case "race":
                case "roles":
                case "scavenge":
                case "siege":
                case "utility":
                case "war":
                case "youtube": goto case "economy";
                case "moderation":
                    if ((Context.User as SocketGuildUser).GuildPermissions.ManageMessages) goto case "economy";
                    else break;
                case "yt":
                    command = "youtube";
                    goto case "economy";
                case "full":
                    var output = new StringBuilder();
                    foreach (var cmd in Global.CommandService.Commands)
                    {
                        var user = GetUser(Context);
                        if (string.Compare(cmd.Module.Name, "Sneak", true) != 0 && (GetUser(Context).Stage > 2 || (GetUser(Context).Stage < 2 && string.Compare(cmd.Remarks, "Stage2", true) != 0)))
                            output.Append($"`{cmd.Name}` ");
                    }
                    output.Append("\n\nThis has been deprecated.");
                    await ReplyAsync(embed: new EmbedBuilder()
                        .WithColor(GetColor(Context))
                        .WithCurrentTimestamp()
                        .WithDescription(output.ToString())
                        .WithTitle("MarbleBot Help")
                        .Build());
                    break;
                default:
                    var hCommand = new HelpCommand();
                    var rawCommand = Global.CommandService.Commands.Where(c => c.Name.ToLower() == command.ToLower() || c.Aliases.Any(alias => alias == command)).First();
                    hCommand = new HelpCommand(rawCommand.Name, rawCommand.Summary, $"mb/{rawCommand.Name.ToLower()}", rawCommand.Aliases);

                    string json;
                    using (var itemFile = new StreamReader($"Resources{Path.DirectorySeparatorChar}ExtraCommandInfo.json"))
                        json = itemFile.ReadToEnd();
                    var commandDict = JObject.Parse(json).ToObject<Dictionary<string, Dictionary<string, string>>>();
                    if (commandDict.ContainsKey(hCommand.Name) || hCommand.Aliases.Any(alias => commandDict.ContainsKey(alias)))
                    {
                        hCommand.Example = commandDict[hCommand.Name]["Example"];
                        hCommand.Usage = commandDict[hCommand.Name]["Usage"];
                    }

                    if (!hCommand.Desc.IsEmpty())
                    {
                        builder.WithDescription(hCommand.Desc)
                            .AddField("Usage", $"`{hCommand.Usage}`")
                            .WithTitle($"MarbleBot Help: **{hCommand.Name[0].ToString().ToUpper() + string.Concat(hCommand.Name.Skip(1))}**");

                        if (hCommand.Aliases.Length > 1)
                        {
                            var aliases = new StringBuilder();
                            foreach (var alias in hCommand.Aliases)
                                aliases.AppendLine($"`mb/{alias}`");
                            builder.AddField("Aliases", aliases.ToString());
                        }

                        if (!hCommand.Example.IsEmpty()) builder.AddField("Example", $"`{hCommand.Example}`");

                        await ReplyAsync(embed: builder.Build());
                    }
                    else await ReplyAsync("Could not find requested command!");
                    break;
            }
        }

        [Command("invite")]
        [Alias("invitelink")]
        [Summary("Gives the bot's invite link.")]
        public async Task InviteCommandAsync() => await ReplyAsync(new StringBuilder()
                .AppendLine("Use this link to invite MarbleBot to your server: https://discordapp.com/oauth2/authorize?client_id=286228526234075136&scope=bot&permissions=1")
                .Append("\nUse `mb/setchannel announcement <channel ID>` to set the channel where bot updates get posted, ")
                .Append("`mb/setchannel autoresponse <channel ID>` to set the channel where autoresponses can be used and ")
                .Append("`mb/setchannel usable <channel ID>` to set a channel where commands can be used! ")
                .Append("If no usable channel is set, commands can be used anywhere.")
                .ToString());

        [Command("serverinfo")]
        [Summary("Displays information about the current server.")]
        [Remarks("Not DMs")]
        public async Task ServerInfoCommandAsync()
        {
            if (!Context.IsPrivate)
            {
                EmbedBuilder builder = new EmbedBuilder();
                int botUsers = 0;
                int onlineUsers = 0;
                SocketGuildUser[] users = Context.Guild.Users.ToArray();
                for (int i = 0; i < Context.Guild.Users.Count - 1; i++)
                {
                    if (users[i].IsBot) botUsers++;
                    if (users[i].Status.ToString().ToLower() == "online") onlineUsers++;
                }

                var owner = Context.Guild.GetUser(Context.Guild.OwnerId);
                var mbServer = GetServer(Context);

                builder.WithThumbnailUrl(Context.Guild.IconUrl)
                    .WithTitle(Context.Guild.Name)
                    .AddField("Owner", $"{owner.Username}#{owner.Discriminator}", true)
                    .AddField("Voice Region", Context.Guild.VoiceRegionId, true)
                    .AddField("Text Channels", Context.Guild.TextChannels.Count, true)
                    .AddField("Voice Channels", Context.Guild.VoiceChannels.Count, true)
                    .AddField("Members", Context.Guild.Users.Count, true)
                    .AddField("Bots", botUsers, true)
                    .AddField("Online", onlineUsers, true)
                    .AddField("Roles", Context.Guild.Roles.Count, true)
                    .AddField("Embed", mbServer.Color.ToString(), true)
                    .WithColor(GetColor(Context))
                    .WithTimestamp(DateTime.UtcNow)
                    .WithFooter(Context.Guild.Id.ToString());

                if (mbServer.AnnouncementChannel != 0)
                    builder.AddField("Announcement Channel", $"<#{mbServer.AnnouncementChannel}>", true);

                if (mbServer.AutoresponseChannel != 0)
                    builder.AddField("Autoresponse Channel", $"<#{mbServer.AutoresponseChannel}>", true);

                if (mbServer.UsableChannels.Count != 0)
                {
                    var output = new StringBuilder();
                    foreach (var channel in mbServer.UsableChannels)
                        output.AppendLine($"<#{channel}>");
                    builder.AddField("Usable Channels", output.ToString(), true);
                }

                await ReplyAsync(embed: builder.Build());
            }
            else await ReplyAsync("This is a DM, not a server!");
        }

        [Command("staffcheck")]
        [Summary("Displays a list of all staff members and their statuses.")]
        [Remarks("Not DMs")]
        [RequireContext(ContextType.Guild)]
        public async Task StaffCheckCommandAsync()
        {
            var output = new StringBuilder();
            foreach (var user in Context.Guild.Users)
            {
                if (user.GuildPermissions.ManageMessages)
                {
                    var status = user.Status switch
                    {
                        UserStatus.AFK => "Idle",
                        UserStatus.DoNotDisturb => "Do Not Disturb",
                        UserStatus.Idle => "Idle",
                        UserStatus.Online => "Online",
                        _ => "Offline"
                    };
                    if (user.Nickname.IsEmpty())
                        output.AppendLine($"{user.Username}#{user.Discriminator}: **{status}**");
                    else
                        output.AppendLine($"{user.Nickname} ({user.Username}#{user.Discriminator}): **{status}**");
                }
            }
            await ReplyAsync(output.ToString());
        }

        [Command("uptime")]
        [Summary("Displays how long the bot has been running for.")]
        public async Task UptimeCommandAsync()
        => await ReplyAsync($"The bot has been running for **{GetDateString(DateTime.UtcNow.Subtract(StartTime))}**.");

        [Command("userinfo")]
        [Summary("Displays information about a user.")]
        [Remarks("Not DMs")]
        public async Task UserInfoCommandAsync([Remainder] string username = "")
        {
            if (!Context.IsPrivate)
            {
                EmbedBuilder builder = new EmbedBuilder();
                SocketGuildUser user = (SocketGuildUser)Context.User;
                var userFound = true;
                username = username.ToLower();
                if (!username.IsEmpty())
                {
                    if (username[0] == '<')
                    {
                        try
                        {
                            ulong.TryParse(username.Trim('<').Trim('>').Trim('@'), out ulong ID);
                            user = Context.Guild.GetUser(ID);
                        }
                        catch (NullReferenceException ex)
                        {
                            await Log(ex.ToString());
                            await ReplyAsync("Invalid ID!");
                            userFound = false;
                        }
                    }
                    else
                    {
                        try
                        {
                            user = Context.Guild.Users.Where(u => u.Username.ToLower().Contains(username)
                            || username.Contains(u.Username.ToLower())).FirstOrDefault();
                        }
                        catch (NullReferenceException ex)
                        {
                            await Log(ex.ToString());
                            await ReplyAsync("Could not find the requested user!");
                            userFound = false;
                        }
                    }
                }
                if (userFound)
                {
                    string status = user.Status switch
                    {
                        UserStatus.Online => "Online",
                        UserStatus.Idle => "Idle",
                        UserStatus.DoNotDisturb => "Do Not Disturb",
                        UserStatus.AFK => "Idle",
                        _ => "Offline"
                    };

                    string nickname = user.Nickname.IsEmpty() ? "None" : user.Nickname;
                    var roles = new StringBuilder();
                    foreach (var role in user.Roles)
                        roles.AppendLine(role.Name);

                    builder.WithAuthor(user)
                        .AddField("Status", status, true)
                        .AddField("Nickname", nickname, true)
                        .AddField("Registered", user.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"), true)
                        .AddField("Joined", ((DateTimeOffset)user.JoinedAt).ToString("yyyy-MM-dd HH:mm:ss"), true)
                        .AddField("Roles", roles.ToString(), true)
                        .WithColor(GetColor(Context))
                        .WithCurrentTimestamp()
                        .WithFooter("All times in UTC, all dates YYYY-MM-DD.");

                    await ReplyAsync(embed: builder.Build());
                }
            }
        }
    }
}
