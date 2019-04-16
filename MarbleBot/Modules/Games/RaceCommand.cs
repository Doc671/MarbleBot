using Discord;
using Discord.Commands;
using MarbleBot.Extensions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarbleBot.Modules
{
    public partial class Games
    {
        [Group("race")]
        [Summary("Participate in a marble race!")]
        public class RaceCommand : MarbleBotModule
        {
            [Command("help")]
            [Summary("Race help.")]
            public async Task RaceHelpCommandAsync([Remainder] string _ = "")
                => await ReplyAsync(embed: new EmbedBuilder()
                    .AddField("How to play",
                        new StringBuilder()
                            .AppendLine("Use `mb/race signup <marble name>` to sign up as a marble!")
                            .AppendLine("When everyone's done, use `mb/race start`! This happens automatically if 10 people have signed up.\n")
                            .AppendLine("Check who's participating with `mb/race contestants`!\n")
                            .AppendLine("You can earn Units of Money if you win! (6 hour cooldown)")
                            .ToString())
                    .WithColor(GetColor(Context))
                    .WithCurrentTimestamp()
                    .WithTitle("Marble Race!")
                    .Build());

            [Command("signup")]
            [Alias("join")]
            [Summary("Sign up to the marble race!")]
            public async Task RaceSignupCommandAsync([Remainder] string marbleName)
            {
                await Context.Channel.TriggerTypingAsync();
                ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                var builder = new EmbedBuilder()
                    .WithColor(GetColor(Context))
                    .WithCurrentTimestamp();
                var name = "";
                if (marbleName.IsEmpty() || marbleName.Contains("@")) name = Context.User.Username;
                else if (marbleName.Length > 100) await ReplyAsync("Your entry exceeds the 100 character limit.");
                else {
                    marbleName = marbleName.Replace("\n", " ").Replace(",", ";");
                    name = marbleName;
                }
                builder.AddField("Marble Race: Signed up!", "**" + Context.User.Username + "** has successfully signed up as **" + name + "**!");
                using (var racers = new StreamWriter("Resources\\RaceMostUsed.txt", true)) await racers.WriteLineAsync(name);
                if (!File.Exists(fileId.ToString() + "race.csv")) File.Create(fileId.ToString() + "race.csv").Close();
                byte alive = 0;
                using (var marbleList = new StreamReader(fileId.ToString() + "race.csv", true)) {
                    while (!marbleList.EndOfStream) {
                        var line = await marbleList.ReadLineAsync();
                        if (!(line.IsEmpty())) alive++;
                    }
                    marbleList.Close();
                }
                using (var marbleList = new StreamWriter(fileId.ToString() + "race.csv", true)) {
                    await marbleList.WriteLineAsync(name + "," + Context.User.Id);
                    marbleList.Close();
                }
                await ReplyAsync(embed: builder.Build());
                if (alive > 9) {
                    await ReplyAsync("The limit of 10 contestants has been reached!");
                    await RaceStartCommandAsync();
                }
            }

            [Command("start")]
            [Alias("begin")]
            [Summary("Starts the marble race.")]
            public async Task RaceStartCommandAsync()
            {
                await Context.Channel.TriggerTypingAsync();
                ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                var builder = new EmbedBuilder()
                    .WithColor(GetColor(Context))
                    .WithCurrentTimestamp();
                byte marbleCount = 0;
                using (var marbleList = new StreamReader(fileId.ToString() + "race.csv")) {
                    while (!marbleList.EndOfStream) {
                        var line = await marbleList.ReadLineAsync();
                        if (!line.IsEmpty()) marbleCount++;
                    }
                }
                if (marbleCount == 0) {
                    await ReplyAsync("It doesn't look like anyone has signed up!");
                } else {
                    // Get marbles
                    var marbles = new List<Tuple<string, ulong>>();
                    using (var marbleList = new StreamReader(fileId.ToString() + "race.csv")) {
                        while (!marbleList.EndOfStream) {
                            var line = (await marbleList.ReadLineAsync()).Split(',');
                            marbles.Add(Tuple.Create(line[0], ulong.Parse(line[1])));
                        }
                        marbleList.Close();
                    }
                    Global.RaceAlive.Add(fileId, marbleCount);

                    // Race start
                    builder.WithTitle("The race has started!");
                    var msg = await ReplyAsync(embed: builder.Build());
                    await Task.Delay(1500);
                    byte alive = Context.IsPrivate ? Global.RaceAlive[Context.User.Id] : Global.RaceAlive[Context.Guild.Id];
                    byte id = alive;
                    while (alive > 1) {
                        int eliminated = 0;
                        do {
                            eliminated = Global.Rand.Next(0, id);
                        } while (marbles[eliminated].Item1 == "///out");
                        var deathmsg = "";
                        var msgs = new List<string>();
                        byte msgCount = 0;
                        using (var msgFile = new StreamReader("Resources\\RaceDeathMessages.txt")) {
                            while (!msgFile.EndOfStream) {
                                msgCount++;
                                msgs.Add(await msgFile.ReadLineAsync());
                            }
                        }
                        int choice = Global.Rand.Next(0, msgCount - 1);
                        deathmsg = msgs[choice];
                        var mName = marbles[eliminated].Item1.ToLower();
                        if (deathmsg.Contains("was") && (mName.Contains("you ") || mName.Contains("we ") || mName.Contains("they ")))
                            deathmsg = "were " + string.Concat(deathmsg.Skip(4));
                        builder.AddField($"**{marbles[eliminated].Item1}** is eliminated!", $"{marbles[eliminated].Item1} {deathmsg} and is now out of the competition!");
                        marbles[eliminated] = Tuple.Create("///out", marbles[eliminated].Item2);
                        alive--;
                        await msg.ModifyAsync(_msg => _msg.Embed = builder.Build());
                        await Task.Delay(1500);
                    }

                    // Race finish
                    if (Context.IsPrivate) Global.RaceAlive.Remove(Context.User.Id);
                    else Global.RaceAlive.Remove(Context.Guild.Id);
                    var winnerID = 0ul;
                    foreach (var marble in marbles) {
                        if (marble.Item1 != "///out") {
                            winnerID = marble.Item2;
                            builder.AddField("**" + marble.Item1 + "** wins!", marble.Item1 + " is the winner!");
                            if (id > 1) {
                                using (var racers = new StreamWriter("Resources\\RaceWinners.txt", true)) await racers.WriteLineAsync(marble.Item1);
                            }
                            await msg.ModifyAsync(_msg => _msg.Embed = builder.Build());
                            await ReplyAsync("**" + marble.Item1 + "** won the race!");
                            break;
                        }
                    }

                    // Reward winner
                    var obj = GetUsersObj();
                    var user = GetUser(Context, obj, winnerID);
                    if (DateTime.UtcNow.Subtract(user.LastRaceWin).TotalHours > 6) {
                        var noOfSameUser = 0;
                        foreach (var marble in marbles) if (marble.Item2 == winnerID) noOfSameUser++;
                        var gift = Convert.ToDecimal(Math.Round(((Convert.ToDouble(id) / noOfSameUser) - 1) * 100, 2));
                        if (gift > 0) {
                            user.Balance += gift;
                            user.NetWorth += gift;
                            user.LastRaceWin = DateTime.UtcNow;
                            user.RaceWins++;
                            obj.Remove(winnerID.ToString());
                            obj.Add(new JProperty(winnerID.ToString(), JObject.FromObject(user)));
                            WriteUsers(obj);
                            await ReplyAsync($"**{user.Name}** won <:unitofmoney:372385317581488128>**{gift:n}** for winning the race!");
                        }
                    }
                    using (var marbleList = new StreamWriter(fileId.ToString() + "race.csv", false)) {
                        await marbleList.WriteAsync("");
                        marbleList.Close();
                    }
                }
            }

            [Command("clear")]
            [Summary("Clears the list of racers.")]
            public async Task RaceClearCommandAsync()
            {
                await Context.Channel.TriggerTypingAsync();
                ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                if (Context.User.Id == 224267581370925056 || Context.IsPrivate) {
                    using (var marbleList = new StreamWriter(fileId.ToString() + "race.csv", false)) {
                        await marbleList.WriteAsync("");
                        await ReplyAsync("Contestant list successfully cleared!");
                        marbleList.Close();
                    }
                }
            }

            [Command("contestants")]
            [Alias("marbles", "participants")]
            [Summary("Shows a list of all the contestants in the race.")]
            public async Task RaceContestantsCommandAsync()
            {
                await Context.Channel.TriggerTypingAsync();
                ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                var builder = new EmbedBuilder()
                    .WithColor(GetColor(Context))
                    .WithCurrentTimestamp();
                var marbles = new StringBuilder();
                byte count = 0;
                using (var marbleList = new StreamReader(fileId.ToString() + "race.csv")) {
                    var allMarbles = (await marbleList.ReadToEndAsync()).Split('\n');
                    foreach (var marble in allMarbles) {
                        if (marble.Length > 16) {
                            var mSplit = marble.Split(',');
                            var user = Context.Client.GetUser(ulong.Parse(mSplit[1].Trim('\n')));
                            if (Context.IsPrivate) marbles.AppendLine($"**{mSplit[0].Trim('\n')}**");
                            else marbles.AppendLine($"**{mSplit[0].Trim('\n')}** [{user.Username}#{user.Discriminator}]");
                            count++;
                        }
                    }
                }
                if (marbles.ToString().IsEmpty()) await ReplyAsync("It looks like there aren't any contestants...");
                else {
                    builder.AddField("Contestants", marbles.ToString());
                    builder.WithFooter("Contestant count: " + count)
                        .WithTitle("Marble Race: Contestants");
                    await ReplyAsync(embed: builder.Build());
                }
            }

            [Command("leaderboard")]
            [Summary("Shows a leaderboard of most used marbles or winning marbles.")]
            public async Task RaceLeaderboardCommandAsync([Remainder] string option)
            {
                await Context.Channel.TriggerTypingAsync();
                ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                var builder = new EmbedBuilder()
                    .WithColor(GetColor(Context))
                    .WithCurrentTimestamp();
                if (option.ToLower().RemoveChar(' ') == "winners") {
                    var winners = new SortedDictionary<string, int>();
                    using (var win = new StreamReader("Resources\\RaceWinners.txt")) {
                        while (!win.EndOfStream) {
                            var racerInfo = await win.ReadLineAsync();
                            if (winners.ContainsKey(racerInfo)) winners[racerInfo]++;
                            else winners.Add(racerInfo, 1);
                        }
                    }
                    var winList = new List<Tuple<string, int>>();
                    foreach (var winner in winners)
                        winList.Add(Tuple.Create(winner.Key, winner.Value));
                    winList = (from winner in winList orderby winner.Item2 descending select winner).ToList();
                    int i = 1, j = 1;
                    var desc = new StringBuilder();
                    foreach (var winner in winList) {
                        if (i < 11) {
                            desc.Append($"{i}{i.Ordinal()}: {winner.Item1} {winner.Item2}\n");
                            if (j < winners.Count) if (!(winList[j].Item2 == winner.Item2)) i++;
                            j++;
                        } else break;
                    }
                    builder.WithTitle("Race Leaderboard: Winners")
                        .WithDescription(desc.ToString());
                    await ReplyAsync(embed: builder.Build());
                } else {
                    var winners = new SortedDictionary<string, int>();
                    using (var win = new StreamReader("Resources\\RaceMostUsed.txt")) {
                        while (!win.EndOfStream) {
                            var racerInfo = await win.ReadLineAsync();
                            if (winners.ContainsKey(racerInfo)) winners[racerInfo]++;
                            else winners.Add(racerInfo, 1);
                        }
                    }
                    var winList = new List<Tuple<string, int>>();
                    foreach (var winner in winners)
                        winList.Add(Tuple.Create(winner.Key, winner.Value));
                    winList = (from winner in winList orderby winner.Item2 descending select winner).ToList();
                    int i = 1, j = 1;
                    var desc = new StringBuilder();
                    foreach (var winner in winList) {
                        if (i < 11) {
                            desc.Append($"{i}{i.Ordinal()}: {winner.Item1} {winner.Item2}\n");
                            if (j < winners.Count) if (!(winList[j].Item2 == winner.Item2)) i++;
                            j++;
                        }
                        else break;
                    }
                    builder.WithTitle("Race Leaderboard: Most Used")
                        .WithDescription(desc.ToString());
                    await ReplyAsync(embed: builder.Build());
                }
            }

            [Command("checkearn")]
            [Summary("Shows whether you can earn money from racing and if not, when.")]
            public async Task RaceCheckearnTaskAsync()
            {
                await Context.Channel.TriggerTypingAsync();
                var user = GetUser(Context);
                var nextDaily = DateTime.UtcNow.Subtract(user.LastRaceWin);
                var output = nextDaily.TotalHours < 6 ? 
                    $"You can earn money from racing in **{GetDateString(user.LastRaceWin.Subtract(DateTime.UtcNow.AddHours(-6)))}**!"
                    : "You can earn money from racing now!";
                await ReplyAsync(embed: new EmbedBuilder()
                    .WithAuthor(Context.User)
                    .WithColor(GetColor(Context))
                    .WithCurrentTimestamp()
                    .WithDescription(output)
                    .Build());
            }

            [Command("remove")]
            [Summary("Removes a contestant from the contestant list.")]
            public async Task RaceRemoveCommandAsync(string marbleToRemove)
            {
                await Context.Channel.TriggerTypingAsync();
                ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                // 0 - Not found, 1 - Found but not yours, 2 - Found & yours, 3 - Found & overridden
                byte state = Context.User.Id == 224267581370925056 ? (byte)3 : (byte)0;
                var wholeFile = new StringBuilder();
                using (var marbleList = new StreamReader(fileId.ToString() + "race.csv")) {
                    while (!marbleList.EndOfStream) {
                        var line = await marbleList.ReadLineAsync();
                        if (line.Split(',')[0] == marbleToRemove) {
                            if (ulong.Parse(line.Split(',')[1]) == Context.User.Id) {
                                state = 2;
                            } else {
                                wholeFile.AppendLine(line);
                                if (!(state == 2)) state = 1;
                            }
                        } else wholeFile.AppendLine(line);
                    }
                }
                switch (state) {
                    case 0: await ReplyAsync("Could not find the requested racer!"); break;
                    case 1: await ReplyAsync("This is not your marble!"); break;
                    case 2:
                        using (var marbleList = new StreamWriter(fileId.ToString() + "race.csv", false)) {
                            await marbleList.WriteAsync(wholeFile.ToString());
                            await ReplyAsync("Removed contestant **" + marbleToRemove + "**!");
                        }
                        break;
                    case 3: goto case 2;
                }
            }
        }
    }
}
