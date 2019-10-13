using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MarbleBot.Common;
using MarbleBot.Extensions;
using MarbleBot.Modules.Games.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using NLog.Targets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static MarbleBot.Global;

namespace MarbleBot.Modules
{
    /// <summary> Owner-only commands. >:) </summary>
    public class Sneak : MarbleBotModule
    {
        private readonly GamesService _gamesService;

        public Sneak(GamesService gamesService)
        {
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
            DailyTimeout = 48;
            foreach (var pair in _gamesService.ScavengeInfo)
                pair.Value.Dispose();
            foreach (var pair in _gamesService.SiegeInfo)
                pair.Value.Dispose();
            foreach (var pair in _gamesService.WarInfo)
                pair.Value.Dispose();
            _gamesService.ScavengeInfo = new ConcurrentDictionary<ulong, Scavenge>();
            _gamesService.SiegeInfo = new ConcurrentDictionary<ulong, Siege>();
            _gamesService.WarInfo = new ConcurrentDictionary<ulong, War>();

            await ReplyAsync("Success.");
        }

        [Command("dailytimeout")]
        [Alias("dt")]
        [Summary("Changes daily timeout (in hours).")]
        [RequireOwner]
        public async Task DailyTimeoutCommand(string rawHours)
        {
            if (ushort.TryParse(rawHours, out ushort hours))
            {
                DailyTimeout = hours;
                await ReplyAsync($"Successfully updated daily timeout to **{hours}** hours!");
            }
            else await ReplyAsync("Invalid number of hours!");
        }

        [Command("fixbalance")]
        [Summary("Fixes the balance of each user.")]
        [Alias("fixbal")]
        [RequireOwner]
        public async Task FixBalanceCommand()
        {
            var itemsObj = GetItemsObject();
            var usersDict = GetUsersObject().ToObject<Dictionary<string, MarbleBotUser>>();
            var newUsersDict = new Dictionary<string, MarbleBotUser>();
            foreach (var userPair in usersDict)
            {
                var user = userPair.Value;
                user.Balance = user.NetWorth - (user.Items == null ? 0 : user.Items.Aggregate(0m, (total, itemPair) =>
                {
                    total += itemsObj[itemPair.Key.ToString("000")].ToObject<Item>().Price * itemPair.Value;
                    return total;
                }));
                newUsersDict.Add(userPair.Key, user);
            }
            WriteUsers(JObject.FromObject(newUsersDict));
            await ReplyAsync("Success.");
        }

        [Command("logs")]
        [Summary("Shows the contents of the current log file.")]
        [RequireOwner]
        public async Task LogCommand()
        {
            string logs;
            using (var logFile = new StreamReader((LogManager.Configuration.FindTargetByName("logfile") as FileTarget).FileName.ToString().RemoveChar('\'')))
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
            SocketGuild srvr = Context.Client.GetGuild(THS);
            ISocketMessageChannel chnl = srvr.GetTextChannel(THS);
            switch (melmon)
            {
                case "desk": await chnl.SendMessageAsync(msg); break;
                case "flam": chnl = srvr.GetTextChannel(224277892182310912); await chnl.SendMessageAsync(msg); break;
                case "ken": srvr = Context.Client.GetGuild(CM); chnl = srvr.GetTextChannel(CM); await chnl.SendMessageAsync(msg); break;
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

        [Command("seezun")]
        [RequireOwner]
        public async Task SeezunCommand(string seezun)
        {
            var obj = GetItemsObject();
            var items = obj.ToObject<Dictionary<string, Item>>();
            switch (seezun)
            {
                case "limited":
                    items["002"] = new Item(items["002"], onSale: true);
                    items["009"] = new Item(items["009"], onSale: true);
                    break;
                case "frozen":
                    items["003"] = new Item(items["003"], onSale: true);
                    goto case "limited";
                case "orange":
                    items["004"] = new Item(items["004"], onSale: true);
                    goto case "limited";
                case "electric":
                    items["005"] = new Item(items["005"], onSale: true);
                    goto case "limited";
                case "burning":
                    items["006"] = new Item(items["006"], onSale: true);
                    goto case "limited";
                case "rotten":
                    items["007"] = new Item(items["007"], onSale: true);
                    goto case "limited";
                case "ulteymut":
                    items["008"] = new Item(items["008"], onSale: true);
                    goto case "limited";
                default:
                    for (int i = 2; i < 10; i++)
                        items[$"00{i}"] = new Item(items[$"00{i}"], onSale: false);
                    break;
            }
            using (var itemFile = new JsonTextWriter(new StreamWriter($"Resources{Path.DirectorySeparatorChar}Items.json")))
            {
                var serialiser = new JsonSerializer() { Formatting = Formatting.Indented };
                serialiser.Serialize(itemFile, obj);
            }
            await ReplyAsync("Successfully updated.");
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
            foreach (var siegePair in _gamesService.SiegeInfo)
                output.AppendLine($"**{siegePair.Key}** - {siegePair.Value}");
            await ReplyAsync(embed: new EmbedBuilder()
                .WithColor(GetColor(Context))
                .WithCurrentTimestamp()
                .WithDescription(output.ToString())
                .WithTitle($"All Sieges: {_gamesService.SiegeInfo.Count}")
                .Build());
        }

        [Command("update")]
        [Summary("Releases update info to all bot channels.")]
        [RequireOwner]
        public async Task UpdateCommand(string _major, [Remainder] string info)
        {
            var major = string.Compare(_major, "major", true) == 0;

            var builder = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithCurrentTimestamp()
                .WithDescription(info)
                .WithTitle("MarbleBot Update");

            var guildDict = GetGuildsObject().ToObject<Dictionary<ulong, MarbleBotGuild>>();
            foreach (var guildPair in guildDict)
            {
                if (guildPair.Value.AnnouncementChannel != 0)
                {
                    var channel = Context.Client.GetGuild(guildPair.Key).GetTextChannel(guildPair.Value.AnnouncementChannel);
                    var msg = await channel.SendMessageAsync(embed: builder.Build());
                    if (major) await msg.PinAsync();
                }
            }
        }
    }
}
