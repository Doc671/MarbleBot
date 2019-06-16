using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MarbleBot.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using static MarbleBot.Global;

namespace MarbleBot.Modules
{
    /// <summary> Owner-only commands. >:) </summary>
    public class Sneak : MarbleBotModule
    {
        [Command("autoresponse")]
        [Summary("Things to do with autoresponses.")]
        [RequireOwner]
        public async Task AutoresponseCommandAsync(string option)
        {
            switch (option)
            {
                case "time": await ReplyAsync($"Last Use: {ARLastUse.ToString()}\nCurrent Time: {DateTime.UtcNow.ToString()}"); break;
                case "update":
                    {
                        Autoresponses = new Dictionary<string, string>();
                        using (var arFile = new StreamReader($"Resources{Path.DirectorySeparatorChar}Autoresponses.txt"))
                        {
                            while (!arFile.EndOfStream)
                            {
                                var arPair = arFile.ReadLine().Split(';');
                                Autoresponses.Add(arPair[0], arPair[1]);
                            }
                        }
                        await ReplyAsync("Dictionary update complete!");
                        break;
                    }
                default: break;
            }
        }

        [Command("dailytimeout")]
        [Alias("dt")]
        [Summary("Changes daily timeout (in hours).")]
        [RequireOwner]
        public async Task DailyTimeoutCommandAsync(string rawHours)
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

        [Command("melmon")]
        [Summary("melmon")]
        [RequireOwner]
        public async Task MelmonCommandAsync(string melmon, [Remainder] string msg)
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

        [Command("update")]
        [Summary("Releases update info to all bot channels.")]
        [RequireOwner]
        public async Task UpdateCommandAsync(string _major, [Remainder] string info)
        {
            var major = string.Compare(_major, "major", true) == 0;

            var builder = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithCurrentTimestamp()
                .WithDescription(info)
                .WithTitle("MarbleBot Update");

            foreach (MBServer server in Servers.Value)
            {
                if (server.AnnouncementChannel != 0)
                {
                    var channel = Context.Client.GetGuild(server.Id).GetTextChannel(server.AnnouncementChannel);
                    var msg = await channel.SendMessageAsync(embed: builder.Build());
                    if (major) await msg.PinAsync();
                }
            }
        }

        [Command("seezun")]
        [RequireOwner]
        public async Task SeezunCommandAsync(string seezun)
        {
            string json;
            using (var itemFile = new StreamReader($"Resources{Path.DirectorySeparatorChar}Items.json")) json = itemFile.ReadToEnd();
            var obj = JObject.Parse(json);
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
        public async Task SetGameCommandAsync([Remainder] string game) => await Context.Client.SetGameAsync(game);

        [Command("setstatus")]
        [RequireOwner]
        public async Task SetStatusCommandAsync(string status)
        => await Context.Client.SetStatusAsync(status switch {
                "idle" => UserStatus.Idle,
                "dnd" => UserStatus.DoNotDisturb,
                "invisible" => UserStatus.Invisible,
                _ => UserStatus.Online
            });

        [Command("siegedict")]
        [RequireOwner]
        public async Task SiegeDictCommandAsync()
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
    }
}
