using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MarbleBot.Core;
using MarbleBot.Extensions;
using System;
using System.Collections.Generic;
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
                .AddField("Ongoing Races", RaceAlive.Count, true)
                .AddField("Ongoing Scavenges", ScavengeInfo.Count, true)
                .AddField("Ongoing Sieges", SiegeInfo.Count, true)
                .AddField("Ongoing Wars", WarInfo.Count, true)
                .AddField("Servers", Global.Servers.Value.Count, true)
                .AddField("Start Time (UTC)", StartTime.Value.ToString("yyyy-MM-dd HH:mm:ss"), true)
                .AddField("Uptime", DateTime.UtcNow.Subtract(StartTime.Value).ToString(), true)
                .WithAuthor(Context.Client.CurrentUser)
                .WithColor(GetColor(Context))
                .WithCurrentTimestamp()
                .WithFooter($"Requested by {Context.User.Username}#{Context.User.Discriminator}")
                .Build());

        [Command("help")]
        [Alias("cmds")]
        [Summary("Gives the user help.")]
        public async Task HelpCommandAsync([Remainder] string command = "")
        {
            await Context.Channel.TriggerTypingAsync();
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
                    var rawCommand = Global.CommandService.Commands.Where(c => c.Name.ToLower() == command.ToLower()).First();
                    hCommand = new HelpCommand(rawCommand.Name, rawCommand.Summary, $"mb/{rawCommand.Name.ToLower()}", rawCommand.Aliases);
                    switch (command)
                    {
                        // Fun
                        case "7ball": hCommand.Usage = "mb/7ball <condition>"; hCommand.Example = "mb/7ball Will I break?"; break;
                        case "bet": hCommand.Usage = "mb/bet [number of marbles]"; hCommand.Example = "mb/bet 30"; break;
                        case "choose": hCommand.Usage = "mb/choose <choice1> | <choice2>"; hCommand.Example = "mb/choose Red | Yellow | Green | Blue"; break;
                        case "orangeify": hCommand.Usage = "mb/orangeify <text>"; hCommand.Example = "mb/orangeify Drink Poup Soop!"; break;
                        case "random": hCommand.Usage = "mb/random <number1> <number2>"; hCommand.Example = "mb/random 1 5"; break;
                        case "rate": hCommand.Usage = "mb/rate <text>"; hCommand.Example = "mb/rate Marbles"; break;
                        case "repeat": hCommand.Usage = "mb/repeat <text>"; hCommand.Example = "mb/repeat Hello!"; break;
                        case "reverse": hCommand.Usage = "mb/reverse <text>"; hCommand.Example = "mb/reverse Bowl"; break;
                        case "vinhglish": hCommand.Usage = "mb/vinglish <optional word>"; hCommand.Example = "mb/vinhglish Am Will You"; break;

                        // Utility
                        case "userinfo": hCommand.Desc = "Displays information about a user."; hCommand.Usage = "mb/userinfo <user>"; hCommand.Example = "mb/userinfo MarbleBot"; break;

                        // Economy
                        case "balance": hCommand.Usage = "mb/balance <optional user>"; break;
                        case "buy": hCommand.Usage = "mb/buy <item ID> <# of items>"; hCommand.Example = "mb/buy 1 1"; break;
                        case "craft": hCommand.Usage = "mb/craft <item ID> <# of items>"; hCommand.Example = "mb/craft 014 2"; break;
                        case "dismantle": hCommand.Usage = "mb/dismantle <item ID> <# of items>"; hCommand.Example = "mb/decraft 045 10"; break;
                        case "inventory": hCommand.Usage = "mb/inventory <optional user>"; break;
                        case "item": hCommand.Usage = "mb/item <item ID>"; break;
                        case "poupsoop": hCommand.Usage = "mb/poupsoop <# Regular> | <# Limited> | <# Frozen> | <# Orange> | <# Electric> | <# Burning> | <# Rotten> | <# Ulteymut> | <# Variety Pack>"; hCommand.Example = "mb/poupsoop 3 | 1"; break;
                        case "profile": hCommand.Usage = "mb/profile <optional user>"; break;
                        case "recipes": hCommand.Usage = "mb/recipes <optional group number>"; hCommand.Example = "mb/recipes 2"; break;
                        case "sell": hCommand.Usage = "mb/sell <item ID> <# of items>"; hCommand.Example = "mb/sell 1 1"; break;
                        case "use": hCommand.Usage = "mb/use <item ID>"; break;

                        // Moderation
                        case "addrole": hCommand.Usage = "mb/addrole <role name>"; break;
                        case "clearchannel": hCommand.Usage = "mb/clearchannel <announcement/autoresponse/usable>"; break;
                        case "removerole": hCommand.Usage = "mb/removerole <role name>"; break;
                        case "setchannel": hCommand.Usage = "mb/setchannel <announcement/autoresponse/usable> <channel ID>"; break;

                        // Roles
                        case "give": hCommand.Usage = "mb/give <role>"; hCommand.Example = "mb/give Owner"; break;
                        case "role": hCommand.Usage = "mb/role <role>"; hCommand.Example = "mb/role Bots"; break;
                        case "rolelist": hCommand.Usage = "mb/rolelist"; break;
                        case "take": hCommand.Usage = "mb/take <role>"; hCommand.Example = "mb/take Criminal"; break;

                        // YT
                        case "cv": hCommand.Usage = "mb/cv <video link> <optional description>"; hCommand.Example = "A thrilling race made with an incredible, one of a kind feature! https://www.youtube.com/watch?v=7lp80lBO1Vs"; break;
                        case "searchchannel": hCommand.Usage = "mb/searchchannel <channelname>"; hCommand.Example = "mb/searchchannel carykh"; break;
                        case "searchvideo": hCommand.Usage = "mb/searchvideo <videoname>"; hCommand.Example = "mb/searchvideo The Amazing Marble Race"; break;
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
            await Context.Channel.TriggerTypingAsync();
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
            await Context.Channel.TriggerTypingAsync();
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
        }

        [Command("uptime")]
        [Summary("Displays how long the bot has been running for.")]
        public async Task UptimeCommandAsync()
        => await ReplyAsync($"The bot has been running for **{GetDateString(DateTime.UtcNow.Subtract(StartTime.Value))}**.");

        [Command("userinfo")]
        [Summary("Displays information about a user.")]
        [Remarks("Not DMs")]
        public async Task UserInfoCommandAsync([Remainder] string username = "")
        {
            if (!Context.IsPrivate)
            {
                await Context.Channel.TriggerTypingAsync();
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
