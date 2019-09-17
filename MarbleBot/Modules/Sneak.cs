using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MarbleBot.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

        [Command("backup")]
        [Summary("Copies all files in the Data directory to the Backup directory.")]
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
            AutoresponseLastUse = new DateTime();
            Autoresponses = new Dictionary<string, string>();
            using (var autoresponseFile = new StreamReader($"Resources{Path.DirectorySeparatorChar}Autoresponses.txt"))
            {
                while (!autoresponseFile.EndOfStream)
                {
                    var autoresponsePair = (await autoresponseFile.ReadLineAsync()).Split(';');
                    Autoresponses.Add(autoresponsePair[0], autoresponsePair[1]);
                }
            }

            DailyTimeout = 48;
            Servers = new List<MarbleBotGuild>();
            using (var srvrFile = new StreamReader($"Data{Path.DirectorySeparatorChar}Guilds.json"))
            {
                string json;
                using (var users = new StreamReader($"Data{Path.DirectorySeparatorChar}Guilds.json"))
                    json = await users.ReadToEndAsync();
                var allServers = JsonConvert.DeserializeObject<Dictionary<ulong, MarbleBotGuild>>(json);
                foreach (var guild in allServers)
                {
                    var guild2 = guild.Value;
                    guild2.Id = guild.Key;
                    Servers.Add(guild2);
                }
            }

            foreach (var pair in ScavengeInfo)
                pair.Value.Dispose();
            foreach (var pair in SiegeInfo)
                pair.Value.Dispose();
            foreach (var pair in WarInfo)
                pair.Value.Dispose();
            ScavengeInfo = new ConcurrentDictionary<ulong, Scavenge>();
            SiegeInfo = new ConcurrentDictionary<ulong, Siege>();
            WarInfo = new ConcurrentDictionary<ulong, War>();

            using (var stream = new StreamReader($"Keys{Path.DirectorySeparatorChar}MBK.txt")) YTKey = stream.ReadLine();

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

        [Command("disposeall")]
        [RequireOwner]
        public async Task DisposeAllAsync()
        {
            foreach (var pair in ScavengeInfo)
                pair.Value.Dispose();
            foreach (var pair in SiegeInfo)
                pair.Value.Dispose();
            foreach (var pair in WarInfo)
                pair.Value.Dispose();
            await ReplyAsync("All separate tasks successfully disposed.");
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
            foreach (var siegePair in SiegeInfo)
                output.Append($"{siegePair.Key} - {siegePair.Value}");
            await ReplyAsync(embed: new EmbedBuilder()
                .WithColor(GetColor(Context))
                .WithCurrentTimestamp()
                .WithDescription(output.ToString())
                .WithTitle("`SiegeInfo`")
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

            foreach (MarbleBotGuild guild in Servers)
            {
                if (guild.AnnouncementChannel != 0)
                {
                    var channel = Context.Client.GetGuild(guild.Id).GetTextChannel(guild.AnnouncementChannel);
                    var msg = await channel.SendMessageAsync(embed: builder.Build());
                    if (major) await msg.PinAsync();
                }
            }
        }
    }
}
