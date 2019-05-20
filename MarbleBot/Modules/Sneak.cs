using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MarbleBot.Core;
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
        public async Task AutoresponseCommandAsync(string option) {
            switch (option) {
                case "time": await ReplyAsync($"Last Use: {ARLastUse.ToString()}\nCurrent Time: {DateTime.UtcNow.ToString()}"); break;
                case "update": {
                    Autoresponses = new Dictionary<string, string>();
                    using (var arFile = new StreamReader("Resources\\Autoresponses.txt")) {
                        while (!arFile.EndOfStream) {
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
            if (ushort.TryParse(rawHours, out ushort hours)) {
                DailyTimeout = hours;
                await ReplyAsync($"Successfully updated daily timeout to **{hours}** hours!");
            } else await ReplyAsync("Invalid number of hours!");
        }

        [Command("melmon")]
        [Summary("melmon")]
        [RequireOwner]
        public async Task MelmonCommandAsync(string melmon, [Remainder] string msg)
        {
            SocketGuild srvr = Context.Client.GetGuild(THS);
            ISocketMessageChannel chnl = srvr.GetTextChannel(THS);
            switch(melmon) {
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
        public async Task UpdateCommandAsync(string _major, [Remainder] string info) {
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

        [Command("setgame")]
        [RequireOwner]
        public async Task SetGameCommandAsync([Remainder] string game) => await Context.Client.SetGameAsync(game);

        [Command("setstatus")]
        [RequireOwner]
        public async Task SetStatusCommandAsync(string status)
        {
            switch (status) {
                case "online": await Context.Client.SetStatusAsync(UserStatus.Online); break;
                case "idle": await Context.Client.SetStatusAsync(UserStatus.Idle); break;
                case "dnd": await Context.Client.SetStatusAsync(UserStatus.DoNotDisturb); break;
                case "donotdisturb": goto case "dnd";
                case "invisible": await Context.Client.SetStatusAsync(UserStatus.Invisible); break;
            }
        }

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
