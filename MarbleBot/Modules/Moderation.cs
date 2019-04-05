using Discord;
using Discord.Commands;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace MarbleBot.Modules
{   
    /// <summary> Moderation commands. </summary>
    public class Moderation : MarbleBotModule
    {
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

        [Command("clear-recent-spam")]
        [Summary("Clears recent empty messages")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        public async Task ClearRecentSpamCommandAsync(string rserver, string rchannel) {
            Trace.WriteLine("hi");
            var server = ulong.Parse(rserver);
            var channel = ulong.Parse(rchannel);
            var msgs = await Context.Client.GetGuild(server).GetTextChannel(channel).GetMessagesAsync(100).FlattenAsync();
            var srvr = new EmbedAuthorBuilder();
            if (server == THS){
                var _THS = Context.Client.GetGuild(THS);
                srvr.WithName(_THS.Name);
                srvr.WithIconUrl(_THS.IconUrl);
            } else if (server == CM) {
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
            foreach (var msg in msgs) {
                var IsLett = !(char.TryParse(msg.Content.Trim('`'), out char e));
                if (!IsLett) IsLett = (char.IsLetter(e) || e == '?' || e == '^' || char.IsNumber(e));
                if (!(IsLett) && channel != 252481530130202624) {
                    builder.AddField(msg.Author.Mention, msg.Content);
                    await msg.DeleteAsync();
                }
            }
            if (server == THS)  {
                var logs = Context.Client.GetGuild(THS).GetTextChannel(327132239257272327);
                await logs.SendMessageAsync("", false, builder.Build());
            } else if (server == CM) {
                var logs = Context.Client.GetGuild(CM).GetTextChannel(387306347936350213);
                await logs.SendMessageAsync("", false, builder.Build());
            }
        }


        // Checks if a string contains a swear; returns true if profanity is present
        public static bool CheckSwear(string msg)
        {
            StreamReader FS;
            if (File.Exists("C:/Folder/NarrationPt8_data/e00/d00/ListOfBand.txt")) FS = new StreamReader("C:/Folder/NarrationPt8_data/e00/d00/ListOfBand.txt");
            else FS = new StreamReader("ListOfBand.txt");
            string swears = FS.ReadLine(); // Imports list of... obscene language
            FS.Close();
            string[] swearList = swears.Split(',');
            var swearPresent = false;
            foreach (var swear in swearList) {
                if (msg.Contains(swear)) {
                    swearPresent = true;
                    break;
                }
            }
            if (swearPresent) Trace.WriteLine($"Profanity detected, violation: {msg}");
            return swearPresent;
        }
    }
}
