using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MarbleBot.Modules
{
    /// <summary> Owner-only commands. >:) </summary>
    public class Sneak : ModuleBase<SocketCommandContext>
    {
        [Command("melmon")]
        [Summary("melmon")]
        [RequireOwner]
        public async Task MelmonCommandAsync(string melmon, [Remainder] string msg)
        {
            SocketGuild srvr = Context.Client.GetGuild(Global.THS);
            ISocketMessageChannel chnl = srvr.GetTextChannel(Global.THS);
            Trace.WriteLine("Time For MElmonry >:)");
            switch(melmon) {
                case "desk": await chnl.SendMessageAsync(msg); break;
                case "flam": chnl = srvr.GetTextChannel(224277892182310912); await chnl.SendMessageAsync(msg); break;
                case "ken": srvr = Context.Client.GetGuild(Global.CM); chnl = srvr.GetTextChannel(Global.CM); await chnl.SendMessageAsync(msg); break;
                case "adam": chnl = srvr.GetTextChannel(240570994211684352); await chnl.SendMessageAsync(msg); break;
                case "brady": chnl = srvr.GetTextChannel(237158048282443776); await chnl.SendMessageAsync(msg); break;
                default:
                    var split = melmon.Split(',');
                    ulong.TryParse(split[0], out ulong chnlId);
                    ulong.TryParse(split[1], out ulong srvrId);
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
            var major = _major == "major";

            var builder = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithCurrentTimestamp()
                .WithDescription(info)
                .WithTitle("MarbleBot Update");

            ISocketMessageChannel chnl = Context.Client.GetGuild(Global.CM).GetTextChannel(Global.BotChannels[1]);
            var msg = await chnl.SendMessageAsync("", false, builder.Build());
            if (major) await msg.PinAsync();

            chnl = Context.Client.GetGuild(Global.THS).GetTextChannel(Global.BotChannels[0]);
            msg = await chnl.SendMessageAsync("", false, builder.Build());
            if (major) await msg.PinAsync();

            chnl = Context.Client.GetGuild(Global.THSC).GetTextChannel(Global.BotChannels[2]);
            msg = await chnl.SendMessageAsync("", false, builder.Build());
            if (major) await msg.PinAsync();

            chnl = Context.Client.GetGuild(Global.VFC).GetTextChannel(Global.BotChannels[3]);
            msg = await chnl.SendMessageAsync("", false, builder.Build());
            if (major) await msg.PinAsync();

            chnl = Context.Client.GetGuild(Global.MT).GetTextChannel(Global.BotChannels[4]);
            msg = await chnl.SendMessageAsync("", false, builder.Build());
            if (major) await msg.PinAsync();
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
    }
}
