using Discord;
using Discord.Commands;
using System.IO;
using System.Threading.Tasks;
using System;

namespace MarbleBot.Modules
{
    public class Moderation : ModuleBase<SocketCommandContext>
    {
        /// <summary>
        /// Moderation commands
        /// </summary>

        [Command("clear")]
        [Summary("Deletes the specified amount of messages.")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        public async Task _clear(uint amount)
        {
            await Context.Channel.TriggerTypingAsync();
            var messages = await Context.Channel.GetMessagesAsync((int)amount + 1).Flatten();
            await Context.Channel.DeleteMessagesAsync(messages);
            const int delay = 5000;
            var m = await ReplyAsync($"{amount} message(s) have been deleted. This message will be deleted in {delay / 1000} seconds.");
            await Task.Delay(delay);
            await m.DeleteAsync();
        }

        [Command("clear-recent-spam")]
        [Summary("Clears recent empty messages")]
        [RequireOwner]
        public async Task _clearRecentSpam(string rserver, string rchannel) {
            Console.WriteLine("hi");
            var server = ulong.Parse(rserver);
            var channel = ulong.Parse(rchannel);
            var msgs = await Context.Client.GetGuild(server).GetTextChannel(channel).GetMessagesAsync(100).Flatten();
            var srvr = new EmbedAuthorBuilder();
            if (server == Global.THS){
                var THS = Context.Client.GetGuild(Global.THS);
                srvr.WithName(THS.Name);
                srvr.WithIconUrl(THS.IconUrl);
            } else if (server == Global.CM) {
                var CM = Context.Client.GetGuild(Global.CM);
                srvr.WithName(CM.Name);
                srvr.WithIconUrl(CM.IconUrl);
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
            if (server == Global.THS)  {
                var logs = Context.Client.GetGuild(Global.THS).GetTextChannel(327132239257272327);
                await logs.SendMessageAsync("", false, builder.Build());
            } else if (server == Global.CM) {
                var logs = Context.Client.GetGuild(Global.CM).GetTextChannel(387306347936350213);
                await logs.SendMessageAsync("", false, builder.Build());
            }
        }


        // Checks if a string contains a swear; returns true if profanity is present
        public static bool _checkSwear(string word)
        {
            StreamReader FS = new StreamReader("C:/Folder/NarrationPt8_data/e00/d00/ListOfBand.txt");
            string swears = FS.ReadLine(); // Imports list of... obscene language
            FS.Close();
            string[] swearList = swears.Split(',');
            bool swearPresent = false;
            for (int h = 0; h < swearList.Length - 1; h++) // Checks each word in the list
            {
                for (int i = 0; i < swearList[h].Length - 1; i++) // Checks each character in each word in the list
                {
                    int chars = 0;
                    bool[] pos = new bool[swearList[h].Length];
                    char[] swear = swearList[h].ToCharArray();
                    for (int j = 0; j < word.Length - 1; j++) // Checks each character in the input word
                    {
                        if (word[j] == swear[i])
                        {
                            chars++;
                            pos[i] = true;
                        }
                    }
                    if (chars >= swear.Length && pos[0] == true && pos[1] == true && pos[2] == true)
                    {
                        Console.WriteLine("Profanity detected, violation: " + word);
                        swearPresent = true;
                    }
                }
            }
            return swearPresent;
        }
    }
}
