﻿using Discord;
using Discord.Commands;
using MarbleBot.Core;
using MarbleBot.Extensions;
using Newtonsoft.Json;
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
            private const GameType Type = GameType.Race;

            [Command("signup")]
            [Alias("join")]
            [Summary("Sign up to the marble race!")]
            public async Task RaceSignupCommandAsync([Remainder] string marbleName = "")
            => await SignupAsync(Context, Type, marbleName, 10, async () => { await RaceStartCommandAsync(); });

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
                int marbleCount = 0;
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
                    RaceAlive.GetOrAdd(fileId, marbleCount);

                    // Race start
                    builder.WithTitle("The race has started!");
                    var msg = await ReplyAsync(embed: builder.Build());
                    await Task.Delay(1500);
                    int alive = Context.IsPrivate ? RaceAlive[Context.User.Id] : RaceAlive[Context.Guild.Id];
                    int id = alive;
                    while (alive > 1)
                    {
                        int eliminated = 0;
                        do eliminated = Rand.Next(0, id);
                        while (string.Compare(marbles[eliminated].Item1, "///out", true) == 0);
                        var deathmsg = "";
                        var msgs = new List<string>();
                        int msgCount = 0;
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
                            string json;
                            using (var messageList = new StreamReader($"Resources{Path.DirectorySeparatorChar}RaceSpecialMessages.json"))
                                json = messageList.ReadToEnd();
                            var messageDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                            var marbleName = marbles[eliminated].Item1.ToLower().RemoveChar(' ');
                            if (messageDict.ContainsKey(marbleName)) builder.WithDescription($"*{messageDict[marbleName]}*");
                        }
                        marbles[eliminated] = ("///out", marbles[eliminated].Item2);
                        alive--;
                        await msg.ModifyAsync(_msg => _msg.Embed = builder.Build());
                        await Task.Delay(1500);
                    }

                    // Race finish
                    if (Context.IsPrivate) RaceAlive.TryRemove(Context.User.Id, out _);
                    else RaceAlive.TryRemove(Context.Guild.Id, out _);
                    var winnerID = 0ul;
                    for (int i = 0; i < marbles.Count; i++)
                    {
                        (string, ulong) marble = marbles[i];
                        if (marble.Item1 != "///out")
                        {
                            winnerID = marble.Item2;
                            builder.AddField($"**{marble.Item1}** wins!", marble.Item1 + " is the winner!");
                            if (id > 1)
                                using (var racers = new StreamWriter($"Data{Path.DirectorySeparatorChar}RaceWinners.txt", true)) await racers.WriteLineAsync(marble.Item1);
                            await msg.ModifyAsync(_msg => _msg.Embed = builder.Build());
                            await ReplyAsync($"**{marble.Item1}** won the race!");
                            break;
                        }
                    }

                    // Reward winner
                    var obj = GetUsersObject();
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
#pragma warning disable IDE0063 // Use simple 'using' statement
                    using (var marbleList = new StreamWriter($"Data{Path.DirectorySeparatorChar}{fileId}race.csv", false))
#pragma warning restore IDE0063 // Use simple 'using' statement
                        await marbleList.WriteAsync("");
                }
            }

            [Command("checkearn")]
            [Summary("Shows whether you can earn money from racing and if not, when.")]
            public async Task RaceCheckearnCommandAsync()
            => await CheckearnAsync(Context, Type);

            [Command("clear")]
            [Summary("Clears the list of racers.")]
            public async Task RaceClearCommandAsync()
            => await ClearAsync(Context, Type);

            [Command("contestants")]
            [Alias("marbles", "participants")]
            [Summary("Shows a list of all the contestants in the race.")]
            public async Task RaceContestantsCommandAsync()
            => await ContestantsAsync(Context, Type);

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
                        using (var win = new StreamReader($"Data{Path.DirectorySeparatorChar}RaceWinners.txt"))
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
                        using (var win = new StreamReader($"Data{Path.DirectorySeparatorChar}RaceMostUsed.txt"))
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

            [Command("remove")]
            [Summary("Removes a contestant from the contestant list.")]
            public async Task RaceRemoveCommandAsync([Remainder] string marbleToRemove)
            => await RemoveAsync(Context, Type, marbleToRemove);

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
