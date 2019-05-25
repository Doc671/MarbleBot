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

using static MarbleBot.Global;

namespace MarbleBot.Modules
{
    public partial class Games
    {
        [Group("race")]
        [Summary("Participate in a marble race!")]
        public class RaceCommand : MarbleBotModule
        {
            [Command("signup")]
            [Alias("join")]
            [Summary("Sign up to the marble race!")]
            public async Task RaceSignupCommandAsync([Remainder] string marbleName = "")
            {
                await Context.Channel.TriggerTypingAsync();
                ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                var builder = new EmbedBuilder()
                    .WithColor(GetColor(Context))
                    .WithCurrentTimestamp();
                string name;
                if (marbleName.IsEmpty() || marbleName.Contains("@")) name = Context.User.Username;
                else if (marbleName.Length > 100)
                {
                    await ReplyAsync($"**{Context.User.Username}**, your entry exceeds the 100 character limit.");
                    return;
                }
                else
                {
                    marbleName = marbleName.Replace("\n", " ").Replace(",", ";");
                    name = marbleName;
                }
                builder.AddField("Marble Race: Signed up!", $"**{Context.User.Username}** has successfully signed up as **{name}**!");
                using (var racers = new StreamWriter("RaceMostUsed.txt", true))
                    await racers.WriteLineAsync(name);
                if (!File.Exists($"Data{Path.DirectorySeparatorChar}{fileId}race.csv")) File.Create($"Data{Path.DirectorySeparatorChar}{fileId}race.csv").Close();
                using (var marbleList = new StreamWriter($"Data{Path.DirectorySeparatorChar}{fileId}race.csv", true))
                    await marbleList.WriteLineAsync(name + "," + Context.User.Id);
                int alive;
                using (var marbleList = new StreamReader($"Data{Path.DirectorySeparatorChar}{fileId}race.csv", true))
                    alive = (await marbleList.ReadToEndAsync()).Split('\n').Length;
                await ReplyAsync(embed: builder.Build());
                if (alive > 9)
                {
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
                using (var marbleList = new StreamReader($"Data{Path.DirectorySeparatorChar}{fileId}race.csv"))
                {
                    while (!marbleList.EndOfStream)
                    {
                        var line = await marbleList.ReadLineAsync();
                        if (!line.IsEmpty()) marbleCount++;
                    }
                }
                if (marbleCount == 0)
                    await ReplyAsync("It doesn't look like anyone has signed up!");
                else
                {
                    // Get marbles
                    var marbles = new List<(string, ulong)>();
                    using (var marbleList = new StreamReader($"Data{Path.DirectorySeparatorChar}{fileId}race.csv"))
                    {
                        while (!marbleList.EndOfStream)
                        {
                            var line = (await marbleList.ReadLineAsync()).Split(',');
                            marbles.Add((line[0], ulong.Parse(line[1])));
                        }
                        marbleList.Close();
                    }
                    RaceAlive.Add(fileId, marbleCount);

                    // Race start
                    builder.WithTitle("The race has started!");
                    var msg = await ReplyAsync(embed: builder.Build());
                    await Task.Delay(1500);
                    byte alive = Context.IsPrivate ? RaceAlive[Context.User.Id] : RaceAlive[Context.Guild.Id];
                    byte id = alive;
                    while (alive > 1)
                    {
                        int eliminated = 0;
                        do eliminated = Rand.Next(0, id);
                        while (string.Compare(marbles[eliminated].Item1, "///out", true) == 0);
                        var deathmsg = "";
                        var msgs = new List<string>();
                        byte msgCount = 0;
                        using (var msgFile = new StreamReader($"Resources{Path.DirectorySeparatorChar}RaceDeathMessages.txt"))
                        {
                            while (!msgFile.EndOfStream)
                            {
                                msgCount++;
                                msgs.Add(await msgFile.ReadLineAsync());
                            }
                        }
                        int choice = Rand.Next(0, msgCount - 1);
                        deathmsg = msgs[choice];
                        var mName = marbles[eliminated].Item1.ToLower();
                        builder.AddField($"**{marbles[eliminated].Item1}** is eliminated!", $"{marbles[eliminated].Item1} {deathmsg} and is now out of the competition!");
                        if (alive == id && id > 1)
                        {
                            switch (marbles[eliminated].Item1.ToLower().RemoveChar(' '))
                            {
                                case "algodoo": builder.WithDescription("*Not surprised, to be honest...*"); break;
                                case "deletion": builder.WithDescription("*Deletion got deleted...*"); break;
                                case "desk": 
                                case "desk176": 
                                case "doc671": builder.WithDescription("*You Silly Desk*"); break;
                                case "gold": builder.WithDescription("*Ironic, isn't it?*"); break;
                                case "lorddeskument": goto case "desk";
                                case "sanddollar": builder.WithDescription("*Really, Sand Dollar? Again?*"); break;
                            }
                        }
                        marbles[eliminated] = ("///out", marbles[eliminated].Item2);
                        alive--;
                        await msg.ModifyAsync(_msg => _msg.Embed = builder.Build());
                        await Task.Delay(1500);
                    }

                    // Race finish
                    if (Context.IsPrivate) RaceAlive.Remove(Context.User.Id);
                    else RaceAlive.Remove(Context.Guild.Id);
                    var winnerID = 0ul;
                    for (int i = 0; i < marbles.Count; i++)
                    {
                        (string, ulong) marble = marbles[i];
                        if (marble.Item1 != "///out")
                        {
                            winnerID = marble.Item2;
                            builder.AddField($"**{marble.Item1}** wins!", marble.Item1 + " is the winner!");
                            if (id > 1)
                                using (var racers = new StreamWriter("RaceWinners.txt", true)) await racers.WriteLineAsync(marble.Item1);
                            await msg.ModifyAsync(_msg => _msg.Embed = builder.Build());
                            await ReplyAsync($"**{marble.Item1}** won the race!");
                            break;
                        }
                    }

                    // Reward winner
                    var obj = GetUsersObj();
                    var user = GetUser(Context, obj, winnerID);
                    if (DateTime.UtcNow.Subtract(user.LastRaceWin).TotalHours > 6)
                    {
                        var noOfSameUser = 0;
                        foreach (var marble in marbles) if (marble.Item2 == winnerID) noOfSameUser++;
                        var gift = Convert.ToDecimal(Math.Round(((Convert.ToDouble(id) / noOfSameUser) - 1) * 100, 2));
                        if (gift > 0)
                        {
                            if (user.Items.ContainsKey(83)) gift *= 3;
                            user.Balance += gift;
                            user.NetWorth += gift;
                            user.LastRaceWin = DateTime.UtcNow;
                            user.RaceWins++;
                            obj.Remove(winnerID.ToString());
                            obj.Add(new JProperty(winnerID.ToString(), JObject.FromObject(user)));
                            WriteUsers(obj);
                            await ReplyAsync($"**{user.Name}** won {UoM}**{gift:n2}** for winning the race!");
                        }
                    }
                    using (var marbleList = new StreamWriter($"Data{Path.DirectorySeparatorChar}{fileId}race.csv", false))
                    {
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
                if (Context.User.Id == 224267581370925056 || Context.IsPrivate)
                {
                    using (var marbleList = new StreamWriter($"Data{Path.DirectorySeparatorChar}{fileId}race.csv", false))
                    {
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
                using (var marbleList = new StreamReader($"Data{Path.DirectorySeparatorChar}{fileId}race.csv"))
                {
                    var allMarbles = (await marbleList.ReadToEndAsync()).Split('\n');
                    foreach (var marble in allMarbles)
                    {
                        if (marble.Length > 16)
                        {
                            var mSplit = marble.Split(',');
                            var user = Context.Client.GetUser(ulong.Parse(mSplit[1].Trim('\n')));
                            if (Context.IsPrivate) marbles.AppendLine($"**{mSplit[0].Trim('\n')}**");
                            else marbles.AppendLine($"**{mSplit[0].Trim('\n')}** [{user.Username}#{user.Discriminator}]");
                            count++;
                        }
                    }
                }
                if (marbles.ToString().IsEmpty()) await ReplyAsync("It looks like there aren't any contestants...");
                else
                {
                    builder.AddField("Contestants", marbles.ToString());
                    builder.WithFooter("Contestant count: " + count)
                        .WithTitle("Marble Race: Contestants");
                    await ReplyAsync(embed: builder.Build());
                }
            }

            [Command("leaderboard")]
            [Summary("Shows a leaderboard of most used marbles or winning marbles.")]
            public async Task RaceLeaderboardCommandAsync(string option, string rawNo = "1")
            {
                await Context.Channel.TriggerTypingAsync();
                if (int.TryParse(rawNo, out int no))
                {
                    ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                    var builder = new EmbedBuilder()
                        .WithColor(GetColor(Context))
                        .WithCurrentTimestamp();
                    if (string.Compare(option.RemoveChar(' '), "winners", true) == 0)
                    {
                        var winners = new SortedDictionary<string, int>();
                        using (var win = new StreamReader("RaceWinners.txt"))
                        {
                            while (!win.EndOfStream)
                            {
                                var racerInfo = await win.ReadLineAsync();
                                if (winners.ContainsKey(racerInfo)) winners[racerInfo]++;
                                else winners.Add(racerInfo, 1);
                            }
                        }
                        var winList = new List<(string, int)>();
                        foreach (var winner in winners)
                            winList.Add((winner.Key, winner.Value));
                        winList = (from winner in winList orderby winner.Item2 descending select winner).ToList();
                        builder.WithTitle("Race Leaderboard: Winners")
                            .WithDescription(Leaderboard(winList, no));
                        await ReplyAsync(embed: builder.Build());
                    }
                    else
                    {
                        var winners = new SortedDictionary<string, int>();
                        using (var win = new StreamReader("RaceMostUsed.txt"))
                        {
                            while (!win.EndOfStream)
                            {
                                var racerInfo = await win.ReadLineAsync();
                                if (winners.ContainsKey(racerInfo)) winners[racerInfo]++;
                                else winners.Add(racerInfo, 1);
                            }
                        }
                        var winList = new List<(string, int)>();
                        foreach (var winner in winners)
                            winList.Add((winner.Key, winner.Value));
                        winList = (from winner in winList orderby winner.Item2 descending select winner).ToList();
                        builder.WithTitle("Race Leaderboard: Most Used")
                            .WithDescription(Leaderboard(winList, no));
                        await ReplyAsync(embed: builder.Build());
                    }
                }
                else await ReplyAsync("This is not a valid number! Format: `mb/race leaderboard <winners/mostused> <optional number>`");
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
                byte state = Context.User.Id == 224267581370925056 ? (byte)3 : (byte)0;
                var wholeFile = new StringBuilder();
                using (var marbleList = new StreamReader($"Data{Path.DirectorySeparatorChar}{fileId}race.csv"))
                {
                    while (!marbleList.EndOfStream)
                    {
                        var line = await marbleList.ReadLineAsync();
                        if (string.Compare(line.Split(',')[0], marbleToRemove, true) == 0)
                        {
                            if (ulong.Parse(line.Split(',')[1]) == Context.User.Id) state = 2;
                            else
                            {
                                wholeFile.AppendLine(line);
                                if (!(state == 2)) state = 1;
                            }
                        }
                        else wholeFile.AppendLine(line);
                    }
                }
                switch (state)
                {
                    case 0: await ReplyAsync("Could not find the requested racer!"); break;
                    case 1: await ReplyAsync("This is not your marble!"); break;
                    case 2:
                        using (var marbleList = new StreamWriter($"Data{Path.DirectorySeparatorChar}{fileId}race.csv", false))
                        {
                            await marbleList.WriteAsync(wholeFile.ToString());
                            await ReplyAsync($"Removed contestant **{marbleToRemove}**!");
                        }
                        break;
                    case 3: goto case 2;
                }
            }

            [Command("")]
            [Alias("help")]
            [Priority(-1)]
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
        }
    }
}
