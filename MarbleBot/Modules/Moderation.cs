using Discord;
using Discord.Commands;
using MarbleBot.Core;
using MarbleBot.Extensions;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MarbleBot.Modules
{
    /// <summary> Moderation commands. </summary>
    public class Moderation : MarbleBotModule
    {
        [Command("addrole")]
        [Summary("Adds a role to the role list.")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task AddRoleCommandAsync([Remainder] string searchTerm)
        {
            if (!Context.Guild.Roles.Any(r => string.Compare(r.Name, searchTerm, true) == 0))
            {
                await ReplyAsync("Could not find the role!");
                return;
            }
            var id = Context.Guild.Roles.Where(r => string.Compare(r.Name, searchTerm, true) == 0).First().Id;
            var newServer = false;
            MBServer server;
            if (Global.Servers.Value.Any(s => s.Id == Context.Guild.Id))
            {
                server = GetServer(Context);
                server.Roles.Add(id);
            }
            else
            {
                newServer = true;
                server = new MBServer(Context.Guild.Id, 0, 0, "607D8B", new ulong[] { 0 }, new ulong[] { 0 });
            }
            if (newServer) Global.Servers.Value.Add(server);
            WriteServers();
            await ReplyAsync("Succesfully updated.");
        }

        [Command("clear")]
        [Summary("Deletes the specified amount of messages.")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        public async Task ClearCommandAsync(uint amount)
        {
            await Context.Channel.TriggerTypingAsync();
            var messages = await Context.Channel.GetMessagesAsync((int)amount + 1).FlattenAsync();
            foreach (var msg in messages) await Context.Channel.DeleteMessageAsync(msg);
            const int delay = 5000;
            var m = await ReplyAsync($"{amount} message(s) have been deleted. This message will be deleted in {delay / 1000} seconds.");
            await Task.Delay(delay);
            await m.DeleteAsync();
        }

        [Command("clearchannel")]
        [Summary("Clears channels.")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task ClearChannelCommandAsync(string option)
        {
            var server = MBServer.Empty;
            var newServer = false;
            if (Global.Servers.Value.Any(s => s.Id == Context.Guild.Id))
                server = GetServer(Context);
            else
            {
                newServer = true;
                server.Id = Context.Guild.Id;
            }
            switch (option.ToLower().RemoveChar(' '))
            {
                case "announcement": server.AnnouncementChannel = 0; break;
                case "autoresponse": server.AutoresponseChannel = 0; break;
                case "usable": server.UsableChannels = new List<ulong>(); break;
                default: await ReplyAsync("Invalid option. Use `mb/help clearchannel` for more info."); return;
            }
            if (newServer) Global.Servers.Value.Add(server);
            WriteServers();
            await ReplyAsync("Succesfully cleared.");
        }

        [Command("clearrecentspam")]
        [Alias("clear-recent-spam")]
        [Summary("Clears recent empty messages")]
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task ClearRecentSpamCommandAsync(string rserver, string rchannel)
        {
            var server = ulong.Parse(rserver);
            var channel = ulong.Parse(rchannel);
            var msgs = await Context.Client.GetGuild(server).GetTextChannel(channel).GetMessagesAsync(100).FlattenAsync();
            var srvr = new EmbedAuthorBuilder();
            if (server == THS)
            {
                var _THS = Context.Client.GetGuild(THS);
                srvr.WithName(_THS.Name);
                srvr.WithIconUrl(_THS.IconUrl);
            }
            else if (server == CM)
            {
                var _CM = Context.Client.GetGuild(CM);
                srvr.WithName(_CM.Name);
                srvr.WithIconUrl(_CM.IconUrl);
            }
            var builder = new EmbedBuilder()
                .WithAuthor(srvr)
                .WithTitle("Bulk delete in " + Context.Client.GetGuild(server).GetTextChannel(channel).Mention)
                .WithColor(Color.Red)
                .WithDescription("Reason: Empty Messages")
                .WithFooter("ID: " + channel)
                .WithCurrentTimestamp();
            foreach (var msg in msgs)
            {
                var IsLett = !(char.TryParse(msg.Content.Trim('`'), out char e));
                if (!IsLett) IsLett = (char.IsLetter(e) || e == '?' || e == '^' || char.IsNumber(e));
                if (!(IsLett) && channel != 252481530130202624)
                {
                    builder.AddField(msg.Author.Mention, msg.Content);
                    await msg.DeleteAsync();
                }
            }
            if (server == THS)
            {
                var logs = Context.Client.GetGuild(THS).GetTextChannel(327132239257272327);
                await logs.SendMessageAsync("", false, builder.Build());
            }
            else if (server == CM)
            {
                var logs = Context.Client.GetGuild(CM).GetTextChannel(387306347936350213);
                await logs.SendMessageAsync("", false, builder.Build());
            }
        }


        // Checks if a string contains a swear; returns true if profanity is present
        public static async Task<bool> CheckSwearAsync(string msg)
        {
            string swears;
            using (var FS = new StreamReader("Keys\\ListOfBand.txt")) swears = await FS.ReadLineAsync();
            string[] swearList = swears.Split(',');
            var swearPresent = false;
            foreach (var swear in swearList)
            {
                if (msg.Contains(swear))
                {
                    swearPresent = true;
                    break;
                }
            }
            if (swearPresent) await Log($"Profanity detected, violation: {msg}");
            return swearPresent;
        }

        [Command("removerole")]
        [Summary("Removes a role from the role list.")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task RemoveRoleCommandAsync([Remainder] string searchTerm)
        {
            if (Global.Servers.Value.GetServer(Context, out MBServer server))
            {
                await ReplyAsync("Could not find the role!");
                return;
            }
            var id = Context.Guild.Roles.Where(r => string.Compare(r.Name, searchTerm, true) == 0).First().Id;
            if (!Context.Guild.Roles.Any(r => string.Compare(r.Name, searchTerm, true) == 0) ||
                !server.Roles.Contains(id))
            {
                await ReplyAsync("Could not find the role!");
                return;
            }
            server.Roles.Remove(id);
            WriteServers();
            await ReplyAsync("Succesfully updated.");   
        }

        [Command("setchannel")]
        [Summary("Sets a channel.")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task SetChannelAsync(string option, string rawChannel)
        {
            if (!ulong.TryParse(rawChannel.RemoveChar('<').RemoveChar('>').RemoveChar('#'), out ulong channel))
            {
                await ReplyAsync("Invalid channel!");
                return;
            }
            var newServer = false;
            if (!Global.Servers.Value.GetServer(Context, out MBServer server))
            {
                newServer = true;
                server.Id = Context.Guild.Id;
            }
            switch (option.ToLower().RemoveChar(' '))
            {
                case "announcement": server.AnnouncementChannel = channel; break;
                case "autoresponse": server.AutoresponseChannel = channel; break;
                case "usable": server.UsableChannels.Add(channel); break;
                default: await ReplyAsync("Invalid option. Use `mb/help setchannel` for more info."); return;
            }
            if (newServer) Global.Servers.Value.Add(server);
            WriteServers();
            await ReplyAsync("Successfully updated.");
        }

        [Command("setcolor")]
        [Alias("setcolour", "setembedcolor", "setembedcolour")]
        [Summary("Sets the embed colour of the server using a hexadecimal colour string.")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task SetColorCommandAsync(string input)
        {
            if (!uint.TryParse(input, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
            {
                await ReplyAsync("Invalid hexadecimal colour string.");
                return;
            }
            var newServer = false;
            if (!Global.Servers.Value.GetServer(Context, out MBServer server))
            {
                newServer = true;
                server.Id = Context.Guild.Id;
            }
            server.Color = input;
            if (newServer) Global.Servers.Value.Add(server);
            WriteServers();
            await ReplyAsync("Successfully updated.");
        }
    }
}
