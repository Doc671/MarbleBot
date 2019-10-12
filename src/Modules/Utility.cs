using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MarbleBot.Extensions;
using MarbleBot.Modules.Games.Services;
using MarbleBot.Services;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
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
        private readonly BotCredentials _botCredentials;
        private readonly CommandService _commandService;
        private readonly GamesService _gamesService;

        public Utility(BotCredentials botCredentials, CommandService commandService, GamesService gamesService)
        {
            _botCredentials = botCredentials;
            _commandService = commandService;
            _gamesService = gamesService;
        }

        [Command("botinfo")]
        [Alias("info")]
        [Summary("Shows bot info.")]
        public async Task BotInfoCommand()
            => await ReplyAsync(embed: new EmbedBuilder()
                .AddField("Daily Timeout", $"{DailyTimeout} hours", true)
                .AddField("Ongoing Scavenges", _gamesService.ScavengeInfo.Count, true)
                .AddField("Ongoing Sieges", _gamesService.SiegeInfo.Count, true)
                .AddField("Ongoing Wars", _gamesService.WarInfo.Count, true)
                .AddField("Servers", GetGuildsObject().Count, true)
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
        public async Task CheckTimesCommand()
        {
            var user = GetUser(Context);
            var timeUntilNextDaily = user.LastDaily.Subtract(DateTime.UtcNow.AddHours(-24));
            var timeUntilNextRace = user.LastRaceWin.Subtract(DateTime.UtcNow.AddHours(-6));
            var timeUntilNextScavenge = user.LastScavenge.Subtract(DateTime.UtcNow.AddHours(-6));
            var timeUntilNextSiege = user.LastSiegeWin.Subtract(DateTime.UtcNow.AddHours(-6));
            var timeUntilNextWar = user.LastWarWin.Subtract(DateTime.UtcNow.AddHours(-6));
            await ReplyAsync(embed: new EmbedBuilder()
                .AddField("Daily", timeUntilNextDaily.TotalHours < 0 ? "**Ready!**" : timeUntilNextDaily.ToString(), true)
                .AddField("Race", timeUntilNextRace.TotalHours < 0 ? "**Ready!**" : timeUntilNextRace.ToString(), true)
                .AddField("Scavenge", timeUntilNextScavenge.TotalHours < 0 ? "**Ready!**" : timeUntilNextScavenge.ToString(), true)
                .AddField("Siege", timeUntilNextSiege.TotalHours < 0 ? "**Ready!**" : timeUntilNextSiege.ToString(), true)
                .AddField("War", timeUntilNextWar.TotalHours < 0 ? "**Ready!**" : timeUntilNextWar.ToString(), true)
                .WithAuthor(Context.User)
                .WithColor(GetColor(Context))
                .WithCurrentTimestamp()
                .Build());
        }

        [Command("help")]
        [Alias("cmds", "commands", "searchcommand", "modules", "searchmodule")]
        [Summary("Gives the user help. Using `mb/help <module name>` or `mb/help <command name>` will give information about the module or command respectively.")]
        public async Task HelpCommand([Remainder] string commandToFind = "")
        {
            var builder = new EmbedBuilder()
                .WithCurrentTimestamp()
                .WithColor(GetColor(Context));

            var module = _commandService.Modules.Where(m => m.Name.ToLower() == commandToFind.ToLower()).FirstOrDefault();
            if (module != null)
            {
                bool owner = _botCredentials.AdminIds.Any(id => id == Context.User.Id);

                if ((module.Name == "Moderation" && !(Context.User as SocketGuildUser).GuildPermissions.ManageMessages)
                    || (module.Name == "Sneak" && !owner))
                {
                    await SendErrorAsync("You cannot access this module!");
                    return;
                }

                IEnumerable<CommandInfo> commands = module.Commands.Where(c => owner ? true : !c.Preconditions.Any(p => p is RequireOwnerAttribute)).OrderBy(c => c.Name);

                if (Context.Guild.Id != CM) commands = commands.Where(c => c.Remarks != "CM Only");

                if (Context.IsPrivate) commands = commands.Where(c => c.Preconditions != null && !c.Preconditions.Any(p => p is RequireContextAttribute));

                if (GetUser(Context).Stage < 2) commands = commands.Where(c => c.Remarks != "Stage2");

                await ReplyAsync(embed: builder
                    .AddField(
                            $"{CultureInfo.CurrentCulture.TextInfo.ToTitleCase(commandToFind)} Commands",
                            commands.Aggregate(new StringBuilder(), (builder, c) =>
                            {
                                builder.AppendLine($"**{c.Name}** - {c.Summary}");
                                return builder;
                            }).ToString())
                    .WithTitle("MarbleBot Help")
                    .Build());
            }
            else if (commandToFind.ToLower() == "games")
            {
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
            }
            else
            {
                // If a module could not be found, try searching for a command
                var command = _commandService.Commands.Where(c => c.Name.ToLower() == commandToFind || c.Aliases.Any(alias => alias == commandToFind)
                    && !c.Preconditions.Any(p => p is RequireOwnerAttribute)).FirstOrDefault();

                // If neither a command nor a module could be found, show a list of modules
                if (command == null)
                {
                    if ((Context.User as SocketGuildUser).GuildPermissions.ManageMessages)
                        builder.AddField("Modules", "Economy\nFun\nGames\nModeration\nRoles\nUtility\nYouTube");
                    else builder.AddField("Modules", "Economy\nFun\nGames\nRoles\nUtility\nYouTube");

                    await ReplyAsync(embed: builder
                        .WithDescription("*by Doc671#1965*\n\nUse `mb/help` followed by the name of a module or a command for more info.")
                        .WithTitle("MarbleBot Help")
                        .Build());
                    return;
                }

                string example = "";
                string usage = $"mb/{command.Aliases[0]}{command.Parameters.Aggregate(new StringBuilder(), (builder, param) => { builder.Append($" <{param.Name}>"); return builder; })}";

                // Gets extra command info (e.g. an example of the command's usage) if present
                string json;
                using (var itemFile = new StreamReader($"Resources{Path.DirectorySeparatorChar}ExtraCommandInfo.json"))
                    json = itemFile.ReadToEnd();
                var commandDict = JObject.Parse(json).ToObject<Dictionary<string, Dictionary<string, string>>>();
                if (commandDict.ContainsKey(command.Name) || command.Aliases.Any(alias => commandDict.ContainsKey(alias)))
                {
                    example = commandDict[command.Name].ContainsKey("Example") ? commandDict[command.Name]["Example"] : "";
                    usage = commandDict[command.Name].ContainsKey("Usage") ? commandDict[command.Name]["Usage"] : "";
                }

                builder.WithDescription(command.Summary)
                    .AddField("Usage", $"`{usage}`")
                    .WithTitle($"MarbleBot Help: **{command.Name.CamelToTitleCase()}**");

                if (!string.IsNullOrEmpty(example)) builder.AddField("Example", $"`{example}`", true);

                builder.AddField("Module", $"{CultureInfo.CurrentCulture.TextInfo.ToTitleCase(command.Module.Name)}", true);

                if (command.Aliases.Count() != 0)
                    builder.AddField("Aliases", command.Aliases.Aggregate(new StringBuilder(), (builder, alias) =>
                    {
                        builder.AppendLine($"`mb/{alias}`");
                        return builder;
                    }).ToString(), true);

                if (command.Parameters.Count() != 0)
                    builder.AddField("Parameters", command.Parameters.Aggregate(new StringBuilder(), (builder, param) =>
                    {
                        builder.AppendLine($"{param.Name.CamelToTitleCase()} ({(param.IsOptional ? "optional " : "")}{(param.IsRemainder ? "remainder " : "")}{param.Type.Name})");
                        return builder;
                    }).ToString(), true);

                if (command.Preconditions.Count() != 0)
                    builder.AddField("Preconditions", command.Preconditions.Aggregate(new StringBuilder(), (builder, precondition) =>
                    {
                        builder.AppendLine((precondition.TypeId as Type).Name.CamelToTitleCase());
                        return builder;
                    }).ToString(), true);

                await ReplyAsync(embed: builder.Build());
            }
        }

        [Command("invite")]
        [Alias("invitelink")]
        [Summary("Gives the bot's invite link.")]
        public async Task InviteCommand() => await ReplyAsync(new StringBuilder()
                .AppendLine("Use this link to invite MarbleBot to your guild: https://discordapp.com/oauth2/authorize?client_id=286228526234075136&scope=bot&permissions=1")
                .Append("\nUse `mb/setchannel announcement <channel ID>` to set the channel where bot updates get posted, ")
                .Append("`mb/setchannel autoresponse <channel ID>` to set the channel where autoresponses can be used and ")
                .Append("`mb/setchannel usable <channel ID>` to set a channel where commands can be used! ")
                .Append("If no usable channel is set, commands can be used anywhere.")
                .ToString());

        [Command("serverinfo")]
        [Alias("guildinfo")]
        [Summary("Displays information about the current guild.")]
        [RequireContext(ContextType.Guild)]
        public async Task ServerInfoCommand()
        {
            var builder = new EmbedBuilder();
            int botUsers = 0;
            int onlineUsers = 0;
            SocketGuildUser[] users = Context.Guild.Users.ToArray();
            for (int i = 0; i < Context.Guild.Users.Count - 1; i++)
            {
                if (users[i].IsBot) botUsers++;
                if (users[i].Status.ToString().ToLower() == "online") onlineUsers++;
            }

            var owner = Context.Guild.GetUser(Context.Guild.OwnerId);
            var mbServer = GetGuild(Context);

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
                .AddField("Embed", $"#{mbServer.Color.ToUpper()}", true)
                .AddField("Prefix", mbServer.Prefix, true)
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

        [Command("staffcheck")]
        [Summary("Displays a list of all staff members and their statuses.")]
        [RequireContext(ContextType.Guild)]
        public async Task StaffCheckCommand()
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
                    if (string.IsNullOrEmpty(user.Nickname))
                        output.AppendLine($"{user.Username}#{user.Discriminator}: **{status}**");
                    else
                        output.AppendLine($"{user.Nickname} ({user.Username}#{user.Discriminator}): **{status}**");
                }
            }
            await ReplyAsync(output.ToString());
        }

        [Command("uptime")]
        [Summary("Displays how long the bot has been running for.")]
        public async Task UptimeCommand()
        => await ReplyAsync($"The bot has been running for **{GetDateString(DateTime.UtcNow.Subtract(StartTime))}**.");

        [Command("userinfo")]
        [Summary("Displays information about a user.")]
        [RequireContext(ContextType.Guild)]
        public async Task UserInfoCommand([Remainder] string username = "")
        {
            if (!Context.IsPrivate)
            {
                var builder = new EmbedBuilder();
                var user = (SocketGuildUser)Context.User;
                var userFound = true;
                username = username.ToLower();
                if (!string.IsNullOrEmpty(username))
                {
                    if (username[0] == '<')
                    {
                        try
                        {
                            ulong.TryParse(username.Trim('<').Trim('>').Trim('@'), out ulong Id);
                            user = Context.Guild.GetUser(Id);
                        }
                        catch (NullReferenceException ex)
                        {
                            Logger.Error(ex);
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
                            Logger.Error(ex);
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

                    string nickname = string.IsNullOrEmpty(user.Nickname) ? "None" : user.Nickname;
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
