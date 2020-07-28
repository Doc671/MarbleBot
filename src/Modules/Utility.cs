using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MarbleBot.Common;
using MarbleBot.Extensions;
using MarbleBot.Modules.Games.Services;
using MarbleBot.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarbleBot.Modules
{
    [Summary("Utility commands.")]
    public class Utility : MarbleBotModule
    {
        private readonly BotCredentials _botCredentials;
        private readonly CommandService _commandService;
        private readonly DailyTimeoutService _dailyTimeoutService;
        private readonly GamesService _gamesService;
        private readonly StartTimeService _startTimeService;

        public Utility(BotCredentials botCredentials, CommandService commandService,
            DailyTimeoutService dailyTimeoutService, GamesService gamesService, StartTimeService startTimeService)
        {
            _botCredentials = botCredentials;
            _commandService = commandService;
            _dailyTimeoutService = dailyTimeoutService;
            _gamesService = gamesService;
            _startTimeService = startTimeService;
        }

        [Command("botinfo")]
        [Alias("info")]
        [Summary("Shows bot info.")]
        public async Task BotInfoCommand()
        {
            await ReplyAsync(embed: new EmbedBuilder()
                .AddField("Daily Timeout", $"{_dailyTimeoutService.DailyTimeout} hours", true)
                .AddField("Ongoing Scavenges", _gamesService.Scavenges.Count, true)
                .AddField("Ongoing Sieges", _gamesService.Sieges.Count, true)
                .AddField("Ongoing Wars", _gamesService.Wars.Count, true)
                .AddField("Servers", MarbleBotGuild.GetGuilds().Count, true)
                .AddField("Start Time (UTC)", _startTimeService.StartTime.ToString("yyyy-MM-dd HH:mm:ss"), true)
                .AddField("Uptime", (DateTime.UtcNow - _startTimeService.StartTime).ToString(), true)
                .WithAuthor(Context.Client.CurrentUser)
                .WithColor(GetColor(Context))
                .WithCurrentTimestamp()
                .WithFooter($"Requested by {Context.User.Username}#{Context.User.Discriminator}")
                .Build());
        }

        [Command("checkearn")]
        [Alias("check", "checktimes")]
        [Summary("Shows the time remaining for each activity with a cooldown.")]
        public async Task CheckTimesCommand()
        {
            MarbleBotUser user = MarbleBotUser.Find(Context);
            TimeSpan timeUntilNextDaily = user.LastDaily - DateTime.UtcNow.AddHours(-24);
            TimeSpan timeUntilNextRace = user.LastRaceWin - DateTime.UtcNow.AddHours(-6);
            TimeSpan timeUntilNextScavenge = user.LastScavenge - DateTime.UtcNow.AddHours(-6);
            TimeSpan timeUntilNextSiege = user.LastSiegeWin - DateTime.UtcNow.AddHours(-6);
            TimeSpan timeUntilNextWar = user.LastWarWin - DateTime.UtcNow.AddHours(-6);
            await ReplyAsync(embed: new EmbedBuilder()
                .AddField("Daily",
                    timeUntilNextDaily.TotalHours < 0 ? "**Ready!**" : timeUntilNextDaily.ToString(@"hh\:mm\:ss"), 
                    inline: true)
                .AddField("Race",
                    timeUntilNextRace.TotalHours < 0 ? "**Ready!**" : timeUntilNextRace.ToString(@"hh\:mm\:ss"), 
                    inline: true)
                .AddField("Scavenge",
                    timeUntilNextScavenge.TotalHours < 0 ? "**Ready!**" : timeUntilNextScavenge.ToString(@"hh\:mm\:ss"),
                    inline: true)
                .AddField("Siege",
                    timeUntilNextSiege.TotalHours < 0 ? "**Ready!**" : timeUntilNextSiege.ToString(@"hh\:mm\:ss"), 
                    inline: true)
                .AddField("War",
                    timeUntilNextWar.TotalHours < 0 ? "**Ready!**" : timeUntilNextWar.ToString(@"hh\:mm\:ss"), 
                    inline: true)
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

            ModuleInfo module = _commandService.Modules.FirstOrDefault(m =>
                string.Equals(m.Name, commandToFind, StringComparison.CurrentCultureIgnoreCase));
            if (module != null)
            {
                bool owner = _botCredentials.AdminIds.Any(id => id == Context.User.Id);

                if (!owner && module.Name == "Moderation" && !Context.IsPrivate &&
                    !(Context.User as SocketGuildUser)!.GuildPermissions.ManageMessages
                    || module.Name == "Sneak")
                {
                    await SendErrorAsync("You cannot access this module!");
                    return;
                }

                IEnumerable<CommandInfo> commands = module.Commands
                    .Where(c => owner || !c.Preconditions.Any(p => p is RequireOwnerAttribute)).OrderBy(c => c.Name);

                if (Context.IsPrivate)
                {
                    if (!owner)
                    {
                        commands = commands.Where(c =>
                            c.Preconditions != null && !c.Preconditions.Any(p => p is RequireContextAttribute));
                    }
                }
                else if (Context.Guild.Id != CommunityMarble)
                {
                    commands = commands.Where(commandInfo => commandInfo.Remarks != "CM Only");
                }

                if (MarbleBotUser.Find(Context)?.Stage < 2)
                {
                    commands = commands.Where(commandInfo => commandInfo.Remarks != "Stage2");
                }

                var commandInfos = commands as CommandInfo[] ?? commands.ToArray();
                if (!commandInfos.Any())
                {
                    await SendErrorAsync("No applicable commands in this module could be found!");
                }

                await ReplyAsync(embed: builder
                    .AddField($"{CultureInfo.CurrentCulture.TextInfo.ToTitleCase(commandToFind)} Commands",
                        commandInfos.Aggregate(new StringBuilder(), (stringBuilder, commandInfo) =>
                        {
                            stringBuilder.AppendLine($"**{commandInfo.Name}** - {commandInfo.Summary}");
                            return stringBuilder;
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
                CommandInfo command = _commandService.Commands.FirstOrDefault(commandInfo =>
                    commandInfo.Name.ToLower() == commandToFind || commandInfo.Aliases.Any(alias => alias == commandToFind)
                    && !commandInfo.Preconditions.Any(precondition => precondition is RequireOwnerAttribute));

                // If neither a command nor a module could be found, show a list of modules
                if (command == null)
                {
                    if (!Context.IsPrivate && (Context.User as SocketGuildUser)!.GuildPermissions.ManageMessages)
                    {
                        builder.AddField("Modules", "Economy\nFun\nGames\nModeration\nRoles\nUtility\nYouTube");
                    }
                    else
                    {
                        builder.AddField("Modules", "Economy\nFun\nGames\nRoles\nUtility\nYouTube");
                    }

                    await ReplyAsync(embed: builder
                        .WithDescription("*by Doc671#1965*\n\nUse `mb/help` followed by the name of a module or a command for more info.")
                        .WithTitle("MarbleBot Help")
                        .Build());
                    return;
                }

                string example = "";
                string usage = $"mb/{command.Aliases[0]}{command.Parameters.Aggregate(new StringBuilder(), (stringBuilder, param) => { stringBuilder.Append($" <{param.Name}>"); return stringBuilder; })}";

                // Gets extra command info (e.g. an example of the command's usage) if present
                string json;
                using (var itemFile = new StreamReader($"Resources{Path.DirectorySeparatorChar}ExtraCommandInfo.json"))
                {
                    json = await itemFile.ReadToEndAsync();
                }

                var commandDict = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(json);
                if (commandDict!.ContainsKey(command.Name) ||
                    command.Aliases.Any(alias => commandDict.ContainsKey(alias)))
                {
                    example = commandDict[command.Name].ContainsKey("Example")
                        ? commandDict[command.Name]["Example"]
                        : "";
                    usage = commandDict[command.Name].ContainsKey("Usage") ? commandDict[command.Name]["Usage"] : "";
                }

                builder.WithDescription(command.Summary)
                    .AddField("Usage", $"`{usage}`")
                    .WithTitle($"MarbleBot Help: **{command.Name.CamelToTitleCase()}**");

                if (!string.IsNullOrEmpty(example))
                {
                    builder.AddField("Example", $"`{example}`", inline: true);
                }

                builder.AddField("Module", $"{CultureInfo.CurrentCulture.TextInfo.ToTitleCase(command.Module.Name)}",
                    true);

                if (command.Aliases.Count > 1)
                {
                    builder.AddField("Aliases", command.Aliases.Skip(1).Aggregate(new StringBuilder(),
                        (stringBuilder, alias) =>
                        {
                            stringBuilder.AppendLine($"`mb/{alias}`");
                            return stringBuilder;
                        }).ToString(), inline: true);
                }

                if (command.Parameters.Count != 0)
                {
                    builder.AddField("Parameters", command.Parameters.Aggregate(new StringBuilder(),
                        (stringBuilder, param) =>
                        {
                            stringBuilder.AppendLine($"{param.Name.CamelToTitleCase()} ({(param.IsOptional ? "optional " : "")}{(param.IsRemainder ? "remainder " : "")}{param.Type.Name})");
                            return stringBuilder;
                        }).ToString(), inline: true);
                }

                if (command.Preconditions.Count != 0)
                {
                    builder.AddField("Preconditions", command.Preconditions.Aggregate(new StringBuilder(),
                        (stringBuilder, precondition) =>
                        {
                            stringBuilder.AppendLine((precondition.TypeId as Type)!.Name[7..^9].CamelToTitleCase());
                            return stringBuilder;
                        }).ToString(), inline: true);
                }

                await ReplyAsync(embed: builder.Build());
            }
        }

        [Command("invite")]
        [Alias("invitelink")]
        [Summary("Gives the bot's invite link.")]
        public async Task InviteCommand()
        {
            await ReplyAsync(new StringBuilder()
                .AppendLine("Use this link to invite MarbleBot to your guild: https://discordapp.com/oauth2/authorize?client_id=286228526234075136&scope=bot&permissions=1")
                .Append("\nUse `mb/setchannel announcement <channel ID>` to set the channel where bot updates get posted, ")
                .Append("`mb/setchannel autoresponse <channel ID>` to set the channel where autoresponses can be used and ")
                .Append("`mb/setchannel usable <channel ID>` to set a channel where commands can be used! ")
                .Append("If no usable channel is set, commands can be used anywhere.")
                .ToString());
        }

        [Command("serverinfo")]
        [Alias("guildinfo")]
        [Summary("Displays information about the current guild.")]
        [RequireContext(ContextType.Guild)]
        public async Task ServerInfoCommand()
        {
            var builder = new EmbedBuilder();
            int botUsers = 0;
            int onlineUsers = 0;
            foreach (SocketGuildUser user in Context.Guild.Users)
            {
                if (user.IsBot)
                {
                    botUsers++;
                }

                if (user.Status != UserStatus.Offline)
                {
                    onlineUsers++;
                }
            }

            MarbleBotGuild mbServer = MarbleBotGuild.Find(Context);

            builder.WithThumbnailUrl(Context.Guild.IconUrl)
                .WithTitle(Context.Guild.Name)
                .AddField("Owner", $"{Context.Guild.Owner.Username}#{Context.Guild.Owner.Discriminator}", true)
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
            {
                builder.AddField("Announcement Channel", $"<#{mbServer.AnnouncementChannel}>", true);
            }

            if (mbServer.AutoresponseChannel != 0)
            {
                builder.AddField("Autoresponse Channel", $"<#{mbServer.AutoresponseChannel}>", true);
            }

            if (mbServer.UsableChannels.Count != 0)
            {
                var output = new StringBuilder();
                foreach (ulong channelId in mbServer.UsableChannels)
                {
                    if ((Context.User as IGuildUser)!.GetPermissions(Context.Guild.GetChannel(channelId)).ViewChannel)
                    {
                        output.AppendLine($"<#{channelId}>");
                    }
                }

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
            foreach (SocketGuildUser user in Context.Guild.Users)
            {
                if (user.GuildPermissions.ManageMessages && !user.IsBot)
                {
                    string status = user.Status switch
                    {
                        UserStatus.AFK => "Idle",
                        UserStatus.DoNotDisturb => "Do Not Disturb",
                        UserStatus.Idle => "Idle",
                        UserStatus.Online => "Online",
                        _ => "Offline"
                    };
                    output.AppendLine(string.IsNullOrEmpty(user.Nickname) 
                        ? $"{user.Username}#{user.Discriminator}: **{status}**" 
                        : $"{user.Nickname} ({user.Username}#{user.Discriminator}): **{status}**");
                }
            }

            await ReplyAsync(output.ToString());
        }

        [Command("uptime")]
        [Summary("Displays how long the bot has been running for.")]
        public async Task UptimeCommand()
        {
            await ReplyAsync($"The bot has been running for **{GetTimeSpanSentence(DateTime.UtcNow - _startTimeService.StartTime)}**.");
        }

        [Command("userinfo")]
        [Summary("Displays information about a user.")]
        [RequireContext(ContextType.Guild)]
        public async Task UserInfoCommand([Remainder] string username = "")
        {
            if (!Context.IsPrivate)
            {
                var builder = new EmbedBuilder();
                var user = (SocketGuildUser)Context.User;
                bool userFound = true;
                username = username.ToLower();
                if (!string.IsNullOrEmpty(username))
                {
                    if (username[0] == '<')
                    {
                        try
                        {
                            ulong.TryParse(username.Trim('<').Trim('>').Trim('@'), out ulong id);
                            user = Context.Guild.GetUser(id);
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
                            user = Context.Guild.Users.FirstOrDefault(u => u.Username.ToLower().Contains(username)
                                                                           || username.Contains(u.Username.ToLower()));
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
                    foreach (SocketRole role in user.Roles)
                    {
                        roles.AppendLine(role.Name);
                    }

                    builder.WithAuthor(user)
                        .AddField("Status", status, true)
                        .AddField("Nickname", nickname, true)
                        .AddField("Registered", user.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"), true)
                        .AddField("Joined", ((DateTimeOffset)user.JoinedAt!).ToString("yyyy-MM-dd HH:mm:ss"), true)
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
