using Discord;
using Discord.Commands;
using MarbleBot.Common;
using MarbleBot.Common.Games.Scavenge;
using MarbleBot.Common.Games.Siege;
using MarbleBot.Common.Games.War;
using MarbleBot.Extensions;
using MarbleBot.Modules.Games.Services;
using MarbleBot.Services;
using NLog;
using NLog.Targets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarbleBot.Modules
{
    [Summary("Owner-only commands.")]
    public class Sneak : MarbleBotModule
    {
        private readonly DailyTimeoutService _dailyTimeoutService;
        private readonly GamesService _gamesService;

        public Sneak(DailyTimeoutService dailyTimeoutService, GamesService gamesService)
        {
            _dailyTimeoutService = dailyTimeoutService;
            _gamesService = gamesService;
        }

        [Command("backup")]
        [Summary("Copies all files in the Data directory a compressed archive.")]
        [RequireOwner]
        public async Task BackupCommand()
        {
            ZipFile.CreateFromDirectory("Data", $"Backup-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}.zip");
            await SendSuccessAsync("Success.");
        }

        [Command("clearmemory")]
        [Alias("cm")]
        [RequireOwner]
        public async Task ClearMemoryCommand()
        {
            _dailyTimeoutService.DailyTimeout = 48;

            foreach ((_, Scavenge scavenge) in _gamesService.Scavenges)
            {
                scavenge.Finalise();
            }

            foreach ((_, Siege siege) in _gamesService.Sieges)
            {
                siege.Finalise();
            }

            foreach ((_, War war) in _gamesService.Wars)
            {
                war.Finalise();
            }

            _gamesService.Scavenges = new ConcurrentDictionary<ulong, Scavenge>();
            _gamesService.Sieges = new ConcurrentDictionary<ulong, Siege>();
            _gamesService.Wars = new ConcurrentDictionary<ulong, War>();

            await SendSuccessAsync("Success.");
        }

        [Command("dailytimeout")]
        [Alias("dt")]
        [Summary("Changes daily timeout (in hours).")]
        [RequireOwner]
        public async Task DailyTimeoutCommand(int hours)
        {
            _dailyTimeoutService.DailyTimeout = hours;
            await ReplyAsync($"Successfully updated daily timeout to **{hours}** hours!");
        }

        [Command("directmessage")]
        [Summary("Sends a direct message to the given user.")]
        [Alias("dm")]
        [RequireOwner]
        public async Task DirectMessageCommand(ulong userId, [Remainder] string message)
        {
            await Context.Client.GetUser(userId).SendMessageAsync(message);
        }

        [Command("fixbalance")]
        [Summary("Fixes the balance of each user.")]
        [Alias("fixbal")]
        [RequireOwner]
        public async Task FixBalanceCommand()
        {
            var itemsDict = Item.GetItems();
            var usersDict = MarbleBotUser.GetUsers();
            var newUsersDict = new Dictionary<ulong, MarbleBotUser>();
            foreach ((ulong userId, MarbleBotUser user) in usersDict!)
            {
                user.Balance = user.NetWorth - (user.Items?.Aggregate(0m, (total, itemPair) =>
                {
                    total += itemsDict[itemPair.Key].Price * itemPair.Value;
                    return total;
                }) ?? 0);
                newUsersDict.Add(userId, user);
            }

            MarbleBotUser.UpdateUsers(newUsersDict);
            await SendSuccessAsync("Success.");
        }

        [Command("logs")]
        [Summary("Shows the contents of the current log file.")]
        [RequireOwner]
        public async Task LogCommand()
        {
            string logs;
            using (var logFile =
                new StreamReader((LogManager.Configuration.FindTargetByName("logfile") as FileTarget)
                !.FileName.ToString()
                !.RemoveChar('\'')))
            {
                logs = logFile.ReadToEnd();
            }

            for (int i = 0; i < logs.Length; i += 2000)
            {
                await ReplyAsync(logs[i..(i + 2000 > logs.Length ? logs.Length : i + 2000)]);
            }
        }

        [Command("postmessage")]
        [Summary("Posts the given message in the given text channel.")]
        [RequireOwner]
        public async Task PostMessageCommand(ITextChannel textChannel, string message)
        {
            await textChannel.SendMessageAsync(message);
        }

        [Command("setgame")]
        [RequireOwner]
        public async Task SetGameCommand([Remainder] string game)
        {
            await Context.Client.SetGameAsync(game);
            await SendSuccessAsync("Success.");
        }

        [Command("setstatus")]
        [RequireOwner]
        public async Task SetStatusCommand(string status)
        {
            await Context.Client.SetStatusAsync(status switch
            {
                "idle" => UserStatus.Idle,
                "dnd" => UserStatus.DoNotDisturb,
                "invisible" => UserStatus.Invisible,
                _ => UserStatus.Online
            });
            await SendSuccessAsync("Success.");
        }

        [Command("siegedict")]
        [RequireOwner]
        public async Task SiegeDictCommand()
        {
            var output = new StringBuilder();
            foreach ((ulong siegeId, Siege siege) in _gamesService.Sieges)
            {
                output.AppendLine($"**{siegeId}** - {siege}");
            }

            await ReplyAsync(embed: new EmbedBuilder()
                .WithColor(GetColor(Context))
                .WithCurrentTimestamp()
                .WithDescription(output.ToString())
                .WithTitle($"All Sieges: {_gamesService.Sieges.Count}")
                .Build());
        }

        [Command("update")]
        [Summary("Releases update info to all bot channels.")]
        [RequireOwner]
        public async Task UpdateCommand(string major, [Remainder] string info)
        {
            bool isMajor = string.Compare(major, "major", StringComparison.OrdinalIgnoreCase) == 0;

            var builder = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithCurrentTimestamp()
                .WithDescription(info)
                .WithTitle("MarbleBot Update");

            var guildsDict = MarbleBotGuild.GetGuilds();
            foreach ((ulong key, MarbleBotGuild value) in guildsDict)
            {
                if (value.AnnouncementChannel != 0)
                {
                    var channel = Context.Client.GetGuild(key)
                        .GetTextChannel(value.AnnouncementChannel);
                    var msg = await channel.SendMessageAsync(embed: builder.Build());
                    if (isMajor)
                    {
                        await msg.PinAsync();
                    }
                }
            }
        }
    }
}
