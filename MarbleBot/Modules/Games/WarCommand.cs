using Discord;
using Discord.Commands;
using MarbleBot.Core;
using MarbleBot.Extensions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarbleBot.Modules
{
    public partial class Games
    {
        [Group("war")]
        [Summary("Participate in a Marble War battle!")]
        [Remarks("Requires a channel in which slowmode is enabled.")]
        public class WarCommand : MarbleBotModule
        {
            [Command("signup")]
            [Alias("join")]
            [Summary("Sign up to the marble war!")]
            public async Task WarSignupCommandAsync(string itemId, [Remainder] string marbleName = "")
            {   
                await Context.Channel.TriggerTypingAsync();
                var item = GetItem(itemId);
                if (item.WarClass == 0)
                {
                    await ReplyAsync($"**{Context.User.Username}**, this item cannot be used as a weapon!");
                    return;
                }

                ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                if (marbleName.IsEmpty() || marbleName.Contains("@")) marbleName = Context.User.Username;
                else if (marbleName.Length > 100)
                {
                    await ReplyAsync($"**{Context.User.Username}**, your entry exceeds the 100 character limit.");
                    return;
                }
                else if (Global.WarInfo.ContainsKey(fileId))
                {
                    await ReplyAsync($"**{Context.User.Username}**, a battle is currently ongoing!");
                    return;
                }

                if (!File.Exists($"Data\\{fileId}war.csv")) File.Create($"Data\\{fileId}war.csv").Close();
                using (var marbleList = new StreamReader($"Data\\{fileId}war.csv"))
                {
                    if ((await marbleList.ReadToEndAsync()).Contains(Context.User.Id.ToString()))
                    {
                        await ReplyAsync("You've already joined!");
                        return;
                    }
                }
                marbleName = marbleName.Replace("\n", " ").Replace(",", ";");
                var builder = new EmbedBuilder()
                    .WithColor(GetColor(Context))
                    .WithCurrentTimestamp()
                    .AddField("Marble War: Signed up!", $"**{Context.User.Username}** has successfully signed up as **{marbleName}** with the weapon **{item.Name}**!");
                using (var fighters = new StreamWriter("Data\\WarMostUsed.txt", true))
                    await fighters.WriteLineAsync(marbleName);
                using (var marbleList = new StreamWriter($"Data\\{fileId}war.csv", true))
                    await marbleList.WriteLineAsync($"{marbleName},{Context.User.Id},{item.Id:000}");
                int alive;
                using (var marbleList = new StreamReader($"Data\\{fileId}war.csv"))
                    alive = (await marbleList.ReadToEndAsync()).Split('\n').Length;
                await ReplyAsync(embed: builder.Build());
                if (alive > 20)
                {
                    await ReplyAsync("The limit of 20 fighters has been reached!");
                    await WarStartCommandAsync();
                }
            }

            [Command("start")]
            [Alias("commence")]
            [Summary("Start the Marble War!")]
            public async Task WarStartCommandAsync()
            {
                await Context.Channel.TriggerTypingAsync();
                ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                var marbles = new List<WarMarble>();
                using (var marbleList = new StreamReader($"Data\\{fileId}war.csv"))
                {
                    while (!marbleList.EndOfStream)
                    {
                        var line = (await marbleList.ReadLineAsync()).Split(',');
                        var userId = ulong.Parse(line[1]);
                        var user = GetUser(Context, userId);
                        marbles.Add(new WarMarble(userId, 50, line[0], GetItem(line[2]),
                            user.Items.ContainsKey(63) && user.Items[63] > 1 ? GetItem("063") : GetItem("000"),
                            user.Items.Where(i => GetItem(i.Key.ToString("000")).Name.Contains("Spikes")).LastOrDefault().Key));
                    }
                }
                var war = new War();
                var t1Output = new StringBuilder();
                var t2Output = new StringBuilder();
                foreach (var marble in marbles)
                {
                    if (Global.Rand.Next(0, 2) > 0)
                    {
                        war.Team1.Add(marble);
                        var user = Context.Client.GetUser(marble.Id);
                        t1Output.AppendLine($"**{marble.Name}** [{user.Username}#{user.Discriminator}]");
                    }
                    else
                    {
                        war.Team2.Add(marble);
                        var user = Context.Client.GetUser(marble.Id);
                        t2Output.AppendLine($"**{marble.Name}** [{user.Username}#{user.Discriminator}]");
                    }
                }
                using (var teamNames = new StreamReader("Resources\\WarTeamNames.txt"))
                {
                    var nameArray = teamNames.ReadToEnd().Split('\n');
                    war.Team1Name = nameArray[Global.Rand.Next(0, nameArray.Length)];
                    do
                    {
                        war.Team2Name = nameArray[Global.Rand.Next(0, nameArray.Length)];
                    }
                    while (string.Compare(war.Team1Name, war.Team2Name, false) == 0);
                }
                if (war.AllMarbles.Count() % 2 > 0)
                {
                    if (war.Team1.Count > war.Team2.Count)
                    {
                        war.Team2.Add(new WarMarble(Global.BotId, 50, "MarbleBot", GetItem("095"), GetItem("071")));
                        t2Output.AppendLine("**MarbleBot** [MarbleBot#7194]");
                    }
                    else
                    {
                        war.Team1.Add(new WarMarble(Global.BotId, 50, "MarbleBot", GetItem("095"), GetItem("071")));
                        t1Output.AppendLine("**MarbleBot** [MarbleBot#7194]");
                    }
                }
                await ReplyAsync(embed: new EmbedBuilder()
                    .WithColor(GetColor(Context))
                    .WithCurrentTimestamp()
                    .WithDescription("Fun fact: this doesn't work yet")
                    .WithTitle("Let the battle commence!")
                    .AddField($"Team {war.Team1Name}", t1Output.ToString())
                    .AddField($"Team {war.Team2Name}", t2Output.ToString())
                    .Build());
                Global.WarInfo.Add(fileId, war);
                war.Actions = Task.Run(async () => { await war.WarActions(Context); });
            }

            [Command("stop")]
            [RequireOwner]
            public async Task WarStopCommandAsync()
            {
                Global.WarInfo[Context.IsPrivate ? Context.User.Id : Context.Guild.Id].Dispose();
                await ReplyAsync("War successfully stopped.");
            }
        }
    }
}
