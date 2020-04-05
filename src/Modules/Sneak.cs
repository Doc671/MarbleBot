using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MarbleBot.Common;
using MarbleBot.Extensions;
using MarbleBot.Modules.Games.Services;
using MarbleBot.Services;
using NLog;
using NLog.Targets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
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
            System.IO.Compression.ZipFile.CreateFromDirectory("Data", $"Backup-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}.zip");
            await ReplyAsync("Success.");
        }

        [Command("clearmemory")]
        [Alias("cm")]
        [Summary("Resets all values in MarbleBot.Global")]
        [RequireOwner]
        public async Task ClearMemoryCommand()
        {
            _dailyTimeoutService.DailyTimeout = 48;
            foreach (var pair in _gamesService.Scavenges)
            {
                pair.Value.Dispose();
            }

            foreach (var pair in _gamesService.Sieges)
            {
                pair.Value.Dispose();
            }

            foreach (var pair in _gamesService.Wars)
            {
                pair.Value.Dispose();
            }

            _gamesService.Scavenges = new ConcurrentDictionary<ulong, Scavenge>();
            _gamesService.Sieges = new ConcurrentDictionary<ulong, Siege>();
            _gamesService.Wars = new ConcurrentDictionary<ulong, War>();

            await ReplyAsync("Success.");
        }

        [Command("dailytimeout")]
        [Alias("dt")]
        [Summary("Changes daily timeout (in hours).")]
        [RequireOwner]
        public async Task DailyTimeoutCommand(string rawHours)
        {
            if (int.TryParse(rawHours, out int hours))
            {
                _dailyTimeoutService.DailyTimeout = hours;
                await ReplyAsync($"Successfully updated daily timeout to **{hours}** hours!");
            }
            else
            {
                await ReplyAsync("Invalid number of hours!");
            }
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
            foreach (var userPair in usersDict!)
            {
                var user = userPair.Value;
                user.Balance = user.NetWorth - (user.Items == null ? 0 : user.Items.Aggregate(0m, (total, itemPair) =>
                {
                    total += itemsDict[itemPair.Key].Price * itemPair.Value;
                    return total;
                }));
                newUsersDict.Add(userPair.Key, user);
            }
            MarbleBotUser.UpdateUsers(newUsersDict);
            await ReplyAsync("Success.");
        }

        [Command("logs")]
        [Summary("Shows the contents of the current log file.")]
        [RequireOwner]
        public async Task LogCommand()
        {
            string logs;
            using (var logFile = new StreamReader((LogManager.Configuration.FindTargetByName("logfile") as FileTarget)!.FileName.ToString()!.RemoveChar('\'')))
            {
                logs = logFile.ReadToEnd();
            }
            for (int i = 0; i < logs.Length; i += 2000)
            {
                await ReplyAsync(logs[i..(i + 2000 > logs.Length ? logs.Length : i + 2000)]);
            }
        }

        [Command("melmon")]
        [Summary("melmon")]
        [RequireOwner]
        public async Task MelmonCommand(string melmon, [Remainder] string msg)
        {
            SocketGuild srvr = Context.Client.GetGuild(TheHatStoar);
            ISocketMessageChannel chnl = srvr.GetTextChannel(TheHatStoar);
            switch (melmon)
            {
                case "desk": await chnl.SendMessageAsync(msg); break;
                case "flam": chnl = srvr.GetTextChannel(224277892182310912); await chnl.SendMessageAsync(msg); break;
                case "ken": srvr = Context.Client.GetGuild(CommunityMarble); chnl = srvr.GetTextChannel(CommunityMarble); await chnl.SendMessageAsync(msg); break;
                case "adam": chnl = srvr.GetTextChannel(240570994211684352); await chnl.SendMessageAsync(msg); break;
                case "brady": chnl = srvr.GetTextChannel(237158048282443776); await chnl.SendMessageAsync(msg); break;
                default:
                    var split = melmon.Split(',');
                    ulong.TryParse(split[0], out ulong srvrId);
                    ulong.TryParse(split[1], out ulong chnlId);
                    srvr = Context.Client.GetGuild(srvrId);
                    chnl = srvr.GetTextChannel(chnlId);
                    await chnl.SendMessageAsync(msg);
                    break;
            }
        }

        [Command("setgame")]
        [RequireOwner]
        public async Task SetGameCommand([Remainder] string game)
        {
            await Context.Client.SetGameAsync(game);
            await ReplyAsync("Success.");
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
            await ReplyAsync("Success.");
        }

        [Command("siegedict")]
        [RequireOwner]
        public async Task SiegeDictCommand()
        {
            var output = new StringBuilder();
            foreach (var siegePair in _gamesService.Sieges)
            {
                output.AppendLine($"**{siegePair.Key}** - {siegePair.Value}");
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
            var isMajor = string.Compare(major, "major", true) == 0;

            var builder = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithCurrentTimestamp()
                .WithDescription(info)
                .WithTitle("MarbleBot Update");

            var guildsDict = MarbleBotGuild.GetGuilds();
            foreach (var guildPair in guildsDict)
            {
                if (guildPair.Value.AnnouncementChannel != 0)
                {
                    var channel = Context.Client.GetGuild(guildPair.Key).GetTextChannel(guildPair.Value.AnnouncementChannel);
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
