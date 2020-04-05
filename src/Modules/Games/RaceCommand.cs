﻿using Discord;
using Discord.Commands;
using MarbleBot.Common;
using MarbleBot.Extensions;
using MarbleBot.Modules.Games.Services;
using MarbleBot.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace MarbleBot.Modules.Games
{
    [Group("race")]
    [Summary("Participate in a marble race!")]
    public class RaceCommand : GameModule
    {
        private const GameType Type = GameType.Race;

        public RaceCommand(BotCredentials botCredentials, GamesService gamesService, RandomService randomService) : base(botCredentials, gamesService, randomService)
        {
        }

        [Command("signup")]
        [Alias("join")]
        [Summary("Sign up to the marble race!")]
        public async Task RaceSignupCommand([Remainder] string marbleName = "")
        => await Signup(Type, marbleName, 10, async () => { await RaceStartCommand(); });

        [Command("start", RunMode = RunMode.Async)]
        [Alias("begin")]
        [Summary("Starts the marble race.")]
        public async Task RaceStartCommand()
        {
            ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;

            if (!File.Exists($"Data{Path.DirectorySeparatorChar}{fileId}.race"))
            {
                await SendErrorAsync($"**{Context.User.Username}**, no-one is signed up!");
                return;
            }

            var builder = new EmbedBuilder()
                .WithColor(GetColor(Context))
                .WithCurrentTimestamp();
            int marbleCount = 0;
            var marbles = new List<(ulong id, string name)>();
            using (var marbleList = new StreamReader($"Data{Path.DirectorySeparatorChar}{fileId}.race"))
            {
                if (marbleList.BaseStream.Length == 0)
                {
                    await SendErrorAsync($"**{Context.User.Username}**, no-one is signed up!");
                    return;
                }

                var formatter = new BinaryFormatter();
                marbles = (List<(ulong, string)>)formatter.Deserialize(marbleList.BaseStream);
                marbleCount = marbles.Count;
            }

            if (marbleCount == 0)
            {
                await SendErrorAsync($"**{Context.User.Username}**, no-one is signed up!");
                return;
            }

            // Get the death messages
            var messages = new List<string>();
            using (var messageFile = new StreamReader($"Resources{Path.DirectorySeparatorChar}RaceDeathMessages.txt"))
            {
                while (!messageFile.EndOfStream)
                {
                    messages.Add((await messageFile.ReadLineAsync())!);
                }
            }

            // Race start
            builder.WithTitle("The race has started!");
            var msg = await ReplyAsync(embed: builder.Build());
            await Task.Delay(1500);

            for (int alive = marbleCount; alive > 1; alive--)
            {
                int eliminated = 0;
                do
                {
                    eliminated = _randomService.Rand.Next(0, marbleCount);
                }
                while (string.Compare(marbles[eliminated].name, "///out", true) == 0);
                string deathMessage;
                deathMessage = messages[_randomService.Rand.Next(0, messages.Count - 1)];
                string bold = marbles[eliminated].name.Contains('*') || marbles[eliminated].name.Contains('\\') ? "" : "**";
                builder.AddField($"{bold}{marbles[eliminated].name}{bold} is eliminated!", $"{marbles[eliminated].name} {deathMessage} and is now out of the competition!");

                // A special message may be displayed depending on the name of last place
                if (alive == marbleCount && marbleCount > 1)
                {
                    string json;
                    using (var messageList = new StreamReader($"Resources{Path.DirectorySeparatorChar}RaceSpecialMessages.json"))
                    {
                        json = messageList.ReadToEnd();
                    }

                    var messageDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                    var marbleName = marbles[eliminated].name.ToLower().RemoveChar(' ');
                    if (messageDict.ContainsKey(marbleName))
                    {
                        builder.WithDescription($"*{messageDict[marbleName]}*");
                    }
                }

                marbles[eliminated] = (marbles[eliminated].id, "///out");
                await msg.ModifyAsync(_msg => _msg.Embed = builder.Build());
                await Task.Delay(1500);
            }

            // Race finish
            var winningMarble = marbles.Find(m => string.Compare(m.name, "///out") != 0);
            string bold2 = winningMarble.name.Contains('*') || winningMarble.name.Contains('\\') ? "" : "**";
            builder.AddField($"{bold2}{winningMarble.name}{bold2} wins!", winningMarble.name + " is the winner!");
            if (marbleCount > 1)
            {
                using var racers = new StreamWriter($"Data{Path.DirectorySeparatorChar}RaceWinners.txt", true);
                await racers.WriteLineAsync(winningMarble.name);
            }
            await msg.ModifyAsync(_msg => _msg.Embed = builder.Build());
            await ReplyAsync($":trophy: | {bold2}{winningMarble.name}{bold2} won the race!");

            // Reward winner
            var user = MarbleBotUser.Find(winningMarble.id);
            if (DateTime.UtcNow.Subtract(user.LastRaceWin).TotalHours > 6)
            {
                var noOfSameUser = 0;
                foreach (var (id, name) in marbles)
                {
                    if (id == winningMarble.id)
                    {
                        noOfSameUser++;
                    }
                }

                var gift = (decimal)MathF.Round((((float)marbleCount / noOfSameUser) - 1) * 100, 2);
                if (gift > 0)
                {
                    if (user.Items.ContainsKey(83))
                    {
                        gift *= 3;
                    }

                    user.Balance += gift;
                    user.NetWorth += gift;
                    user.LastRaceWin = DateTime.UtcNow;
                    user.RaceWins++;
                    MarbleBotUser.UpdateUser(user);
                    await ReplyAsync($"**{user.Name}** won {UnitOfMoney}**{gift:n2}** for winning the race!");
                }
            }
            using (var marbleList = new StreamWriter($"Data{Path.DirectorySeparatorChar}{fileId}.race", false))
            {
                await marbleList.WriteAsync("");
            }
        }

        [Command("checkearn")]
        [Summary("Shows whether you can earn money from racing and if not, when.")]
        public async Task RaceCheckearnCommand()
        => await Checkearn(Type);

        [Command("clear")]
        [Summary("Clears the list of racers.")]
        public async Task RaceClearCommand()
        => await Clear(Type);

        [Command("contestants")]
        [Alias("marbles", "participants")]
        [Summary("Shows a list of all the contestants in the race.")]
        public async Task RaceContestantsCommand()
        => await ShowContestants(Type);

        [Command("leaderboard")]
        [Summary("Shows a leaderboard of most used marbles or winning marbles.")]
        public async Task RaceLeaderboardCommand(string option, string rawPage = "1")
        {
            if (int.TryParse(rawPage, out int page))
            {
                ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                var builder = new EmbedBuilder()
                    .WithColor(GetColor(Context))
                    .WithCurrentTimestamp();
                if (string.Compare(option.RemoveChar(' '), "winners", true) == 0)
                {
                    var winners = new SortedDictionary<string, int>();
                    using (var winnerFile = new StreamReader($"Data{Path.DirectorySeparatorChar}RaceWinners.txt"))
                    {
                        while (!winnerFile.EndOfStream)
                        {
                            var racerInfo = (await winnerFile.ReadLineAsync())!;
                            if (winners.ContainsKey(racerInfo))
                            {
                                winners[racerInfo]++;
                            }
                            else
                            {
                                winners.Add(racerInfo, 1);
                            }
                        }
                    }
                    var winList = new List<(string elementName, int value)>();
                    foreach (var winner in winners)
                    {
                        winList.Add((winner.Key, winner.Value));
                    }

                    winList = (from winner in winList orderby winner.value descending select winner).ToList();
                    builder.WithTitle("Race Leaderboard: Winners")
                        .WithDescription(Leaderboard(winList, page));
                    await ReplyAsync(embed: builder.Build());
                }
                else
                {
                    var winners = new SortedDictionary<string, int>();
                    using (var winnerFile = new StreamReader($"Data{Path.DirectorySeparatorChar}RaceMostUsed.txt"))
                    {
                        while (!winnerFile.EndOfStream)
                        {
                            var racerInfo = (await winnerFile.ReadLineAsync())!;
                            if (winners.ContainsKey(racerInfo))
                            {
                                winners[racerInfo]++;
                            }
                            else
                            {
                                winners.Add(racerInfo, 1);
                            }
                        }
                    }
                    var winList = new List<(string elementName, int value)>();
                    foreach (var winner in winners)
                    {
                        winList.Add((winner.Key, winner.Value));
                    }

                    winList = (from winner in winList orderby winner.value descending select winner).ToList();
                    builder.WithTitle("Race Leaderboard: Most Used")
                        .WithDescription(Leaderboard(winList, page));
                    await ReplyAsync(embed: builder.Build());
                }
            }
            else
            {
                await ReplyAsync("This is not a valid number! Format: `mb/race leaderboard <winners/mostused> <optional number>`");
            }
        }

        [Command("remove")]
        [Summary("Removes a contestant from the contestant list.")]
        public async Task RaceRemoveCommand([Remainder] string marbleToRemove)
        => await RemoveContestant(Type, marbleToRemove);

        [Command("help")]
        [Alias("")]
        [Priority(-1)]
        [Summary("Race help.")]
        public async Task RaceHelpCommand([Remainder] string _ = "")
            => await ReplyAsync(embed: new EmbedBuilder()
                .AddField("How to play", new StringBuilder()
                        .AppendLine("Use `mb/race signup <marble name>` to sign up as a marble!")
                        .AppendLine("When everyone's done, use `mb/race start`! This happens automatically if 10 marbles have signed up.\n")
                        .AppendLine("Check who's participating with `mb/race contestants`!\n")
                        .AppendLine("You can earn Units of Money if you win! (6 hour cooldown)")
                        .ToString())
                .WithColor(GetColor(Context))
                .WithCurrentTimestamp()
                .WithTitle("Marble Race!")
                .Build());
    }
}
