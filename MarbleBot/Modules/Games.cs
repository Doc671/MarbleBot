using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using MarbleBot.Extensions;

namespace MarbleBot.Modules
{
    public class Games : ModuleBase<SocketCommandContext>
    {
        /// <summary>
        /// Game commands
        /// </summary>

        //public async Task _jumble2()
        //{
        //    if (jumbleActive && Context.Message.Content.ToLower() == jumbleWord.ToLower()) {
        //        await ReplyAsync("**" + Context.User.Username + "** guessed the word! Well done!");
        //        jumbleActive = false;
        //    }
        //}

        [Command("jumble")]
        [Summary("Gives the user help. Not done yet.")]
        public async Task _jumble([Remainder] string answer = "")
        {
            string word = "";
            //int timeout = 0;
            if (Global.jumbleActive)
            {
                if (answer == "") await ReplyAsync("A game of jumble is already active!");
                else {
                    for (int i = 0; i < answer.Length - 1; i++) {
                        if (answer[i] == ' ') answer.Replace("", string.Empty);
                    }
                    if (answer.ToLower() == word.ToLower()) {
                        await ReplyAsync("**" + Context.User.Username + "** guessed the word! Well done!");
                        Global.jumbleActive = false;
                    }
                    else await ReplyAsync("Incorrect.");
                }
            } else {
                Global.jumbleActive = true;
                string[] wordList = new string[60];
                int a = 0;
                using (StreamReader stream = new StreamReader("Jumble.csv")) {
                    while (!stream.EndOfStream) {
                        string list = stream.ReadLine();
                        wordList[a] = list;
                        Console.WriteLine(list);
                        a += 1;
                    }
                    word = wordList[Global.rand.Next(0, wordList.Length)];
                }

                char[] wordArray = word.ToCharArray();
                Console.WriteLine(wordArray);

                for (a = 0; a < word.Length - 1; a++) {
                    int b = Global.rand.Next(0, word.Length - 1);
                    char temp = wordArray[a];
                    wordArray[a] = wordArray[b];
                    wordArray[b] = temp;
                }
                string output = new string(wordArray);
                output = output.ToLower();
                await ReplyAsync("Guess what the word is: **" + output + "**.");
                //do {
                //    var guess = Context.Channel.GetMessagesAsync(1);
                //    guess2 = guess.ToString();
                //    if (guess2.ToLower() == word.ToLower())
                //    {
                //        await ReplyAsync("**" + Context.Message.Author + "** guessed the word! Well done!");
                //        jumbleActive = false;
                //    }
                //    timeout++;
                //    if (timeout > 10000) {
                //        break;
                //    }
                //} while (guess2.ToLower() != word.ToLower());
                //if (timeout > 10000) {
                //    await ReplyAsync("Game over! Nobody could guess the word in time!");
                //    jumbleActive = false;
                //}
                //else if (guess2.ToLower() == word.ToLower())
                //{
                //    await ReplyAsync("**" + Context.User.Username + "** guessed the word! Well done!");
                //    jumbleActive = false;
                //}
                //timeout = 0;
            }
        }

        [Command("race")]
        [Summary("Players compete in a marble race!")]
        public async Task _race(string command = "", [Remainder] string option = null) {
            await Context.Channel.TriggerTypingAsync();
            ulong fileID = 0ul;
            Global.Alive.Add(0, 0);
            Global.Alive.Remove(0);

            if (Context.IsPrivate) fileID = Context.User.Id; else fileID = Context.Guild.Id;
            EmbedBuilder builder = new EmbedBuilder()
                .WithColor(Global.GetColor(Context))
                .WithCurrentTimestamp();
            switch (command) {
                case "signup": { 
                    var name = "";
                    if (option.IsEmpty()) name = Context.User.Username;
                    else if (option.Length > 100) await ReplyAsync("Your entry exceeds the 100 character limit.");
                    else option = option.Replace("\n", " "); name = option;
                    builder.AddField("Signed up!", "**" + Context.User.Username + "** has successfully signed up as **" + name + "**!");
                    using (var racers = new StreamWriter("RaceMostUsed.txt", true)) await racers.WriteLineAsync(name);
                    if (!File.Exists(fileID.ToString() + "race.txt")) File.Create(fileID.ToString() + "race.txt");
                    byte alive = 0;
                    if (Context.IsPrivate) {
                        if (!Global.Alive.ContainsKey(Context.User.Id)) Global.Alive.Add(Context.User.Id, 1);
                        else Global.Alive[Context.User.Id]++;
                        alive = Global.Alive[Context.User.Id];
                    } else {
                        if (!Global.Alive.ContainsKey(Context.Guild.Id)) Global.Alive.Add(Context.Guild.Id, 1);
                        else Global.Alive[Context.Guild.Id]++;
                        alive = Global.Alive[Context.Guild.Id];
                    }
                    using (var marbleList = new StreamWriter(fileID.ToString() + "race.txt", true)) {
                        await marbleList.WriteLineAsync(name);
                        marbleList.Close();
                    }
                    await ReplyAsync("", false, builder.Build());
                    if (alive > 9) {
                        await ReplyAsync("The limit of 10 contestants has been reached!");
                        await _race("start");
                    }
                    break;
                }
                case "start": {
                    var canStart = false;
                    if (Context.IsPrivate) canStart = Global.Alive.ContainsKey(Context.User.Id);
                    else canStart = Global.Alive.ContainsKey(Context.Guild.Id);
                    if (!canStart) {
                        await ReplyAsync("It doesn't look like anyone has signed up!");
                    } else {
                        string[] marbles = new string[10];
                        using (var marbleList = new StreamReader(fileID.ToString() + "race.txt")) {
                            var i = 0;
                            while (!marbleList.EndOfStream) {
                                marbles[i] = await marbleList.ReadLineAsync();
                                i++;
                            }
                            marbleList.Close();
                        }
                        Global.raceActive = true;
                        builder.WithTitle("The race has started!");
                        var msg = await ReplyAsync("", false, builder.Build());
                        Thread.Sleep(1500);
                        byte alive = 255;
                        if (Context.IsPrivate) alive = Global.Alive[Context.User.Id];
                        else alive = Global.Alive[Context.Guild.Id];
                        byte id = alive;
                        while (alive > 1) {
                            int eliminated = 0;
                            do {
                                eliminated = Global.rand.Next(0, id);
                            } while (marbles[eliminated] == "///out");
                            var deathmsg = "";
                            var msgs = new List<string>();
                            byte msgCount = 0;
                            using (var msgFile = new StreamReader("racedeathmsgs.txt")) {
                                while (!msgFile.EndOfStream) {
                                    msgCount++;
                                    msgs.Add(await msgFile.ReadLineAsync());
                                }
                            }
                            int choice = Global.rand.Next(0, (msgCount - 1));
                            deathmsg = msgs[choice];
                            builder.AddField("**" + marbles[eliminated] + "** is eliminated!", marbles[eliminated] + " " + deathmsg + " and is now out of the competition!");
                            marbles[eliminated] = "///out";
                            alive--;
                            await msg.ModifyAsync(_msg => _msg.Embed = builder.Build());
                            Thread.Sleep(1500);
                        }
                        if (Context.IsPrivate) Global.Alive.Remove(Context.User.Id);
                        else Global.Alive.Remove(Context.Guild.Id);
                        for (int a = 0; a < marbles.Length - 1; a++) {
                            if (marbles[a] != "///out" && !string.IsNullOrEmpty(marbles[a]) && !string.IsNullOrWhiteSpace(marbles[a])) {
                                builder.AddField("**" + marbles[a] + "** wins!", marbles[a] + " is the winner!");
                                using (var racers = new StreamWriter("RaceWinners.txt", true)) await racers.WriteLineAsync(marbles[a]);
                                await msg.ModifyAsync(_msg => _msg.Embed = builder.Build());
                                await ReplyAsync("**" + marbles[a] + "** won the race!");
                                break;
                            }
                        }
                        using (var marbleList = new StreamWriter(fileID.ToString() + "race.txt")) {
                            await marbleList.WriteAsync("");
                            marbleList.Close();
                        }
                        Global.raceActive = false;
                    }
                    break;
                }
                case "clear": {
                    if (Context.User.Id == 224267581370925056 || Context.IsPrivate) {
                        using (var marbleList = new StreamWriter(fileID.ToString() + "race.txt")) {
                            await marbleList.WriteAsync("");
                            await ReplyAsync("Contestant list successfully cleared!");
                            marbleList.Close();
                        }
                    }
                    break;
                }
                case "setcount": {
                    if (Context.User.Id == 224267581370925056 || Context.IsPrivate) {
                        var newcount = byte.Parse(option);
                        if (Context.IsPrivate) {
                            if (Global.Alive.ContainsKey(Context.User.Id)) Global.Alive[Context.User.Id] = newcount;
                            else Global.Alive.Add(Context.User.Id, newcount);
                            await ReplyAsync("Changed the contestant count to " + newcount + ".");
                        } else {
                            if (Global.Alive.ContainsKey(Context.Guild.Id)) Global.Alive[Context.Guild.Id] = newcount;
                            else Global.Alive.Add(Context.User.Id, newcount);
                            await ReplyAsync("Changed the contestant count to " + newcount + ".");
                        }
                    }
                    break;
                }
                case "contestants": {
                    var marbles = "";
                    using (var marbleList = new StreamReader(fileID.ToString() + "race.txt")) {
                        marbles = await marbleList.ReadToEndAsync();
                    }
                    if (marbles.IsEmpty()) await ReplyAsync("It looks like there aren't any contestants...");
                    else {
                        builder.AddField("Contestants", marbles);
                        await ReplyAsync("", false, builder.Build());
                    }
                    break;
                }
                case "marbles": goto case "contestants";
                case "participants": goto case "contestants";
                case "leaderboard": {
                    switch (option.ToLower()) {
                        case "winners": {
                            var winners = new SortedDictionary<string, int>();
                            using (var win = new StreamReader("RaceWinners.txt")) {
                                while (!win.EndOfStream) {
                                    var racerInfo = await win.ReadLineAsync();
                                    if (winners.ContainsKey(racerInfo)) winners[racerInfo]++;
                                    else winners.Add(racerInfo, 1);
                                }
                            }
                            var winList = new List<Tuple<string, int>>();
                            foreach (var winner in winners) {
                                winList.Add(Tuple.Create(winner.Key, winner.Value));
                            }
                            winList = (from winner in winList orderby winner.Item2 descending select winner).ToList();
                            int i = 1, j = 1;
                            var desc = "";
                            foreach (var winner in winList) {
                                if (i < 11) {
                                    desc += string.Format("{0}{1}: {2} {3}\n", new string[] { i.ToString(), i.Ordinal(), winner.Item1, winner.Item2.ToString() });
                                    if (j < winners.Count) if (!(winList[j].Item2 == winner.Item2)) i++;
                                    j++;
                                } else break;
                            }
                            builder.WithTitle("Race Leaderboard: Winners")
                                .WithDescription(desc);
                            await ReplyAsync("", false, builder.Build());
                            break;
                        }
                        case "mostused": {
                            var winners = new SortedDictionary<string, int>();
                            using (var win = new StreamReader("RaceMostUsed.txt")) {
                                while (!win.EndOfStream) {
                                    var racerInfo = await win.ReadLineAsync();
                                    if (winners.ContainsKey(racerInfo)) winners[racerInfo]++;
                                    else winners.Add(racerInfo, 1);
                                }
                            }
                            var winList = new List<Tuple<string, int>>();
                            foreach (var winner in winners) {
                                winList.Add(Tuple.Create(winner.Key, winner.Value));
                            }
                            winList = (from winner in winList orderby winner.Item2 descending select winner).ToList();
                            int i = 1, j = 1;
                            var desc = "";
                            foreach (var winner in winList) {
                                if (i < 11) {
                                    desc += string.Format("{0}{1}: {2} {3}\n", new string[] { i.ToString(), i.Ordinal(), winner.Item1, winner.Item2.ToString() });
                                    if (j < winners.Count) if (!(winList[j].Item2 == winner.Item2)) i++;
                                    j++;
                                }
                                else break;
                            }
                            builder.WithTitle("Race Leaderboard: Most Used")
                                .WithDescription(desc);
                            await ReplyAsync("", false, builder.Build());
                            break;
                        }
                    }
                    break;
                }
                default: {
                    builder.AddField("How to play", "Use `mb/race signup [marble name]` to sign up as a marble!\nWhen everyone's done (or when 10 people have signed up), use `mb/race start`!\n\nCheck who's participating with `mb/race contestants`!")
                        .WithTitle("Marble Race!");
                    await ReplyAsync("", false, builder.Build());
                    break;
                }
            }
        }

        //[Command("respond")]
        //[Summary("Time for a TWOW!")]
        //public async Task _respond([Remainder] string response)
        //{
        //    await ReplyAsync(":o: Recording your response. Please wait. :o:");
        //    using (TextWriter FS = new StreamWriter("Responses.csv", true))
        //    {
        //        response = response.Trim();
        //        char[] c = response.ToCharArray();
        //        byte wordCount = 1;
        //        for (int a = 0; a < c.Length - 1; a++)
        //        {
        //            if (c[a] == ' ') wordCount++;
        //        }
        //        FS.WriteLine(response + "," + wordCount + "," + Context.User.Username + "#" + Context.User.Discriminator);
        //        if (wordCount < 10)
        //        {
        //            await ReplyAsync(":white_check_mark: **Your response has been recorded!** :white_check_mark:\n\nYour response is recorded as: **" + response + "**\n\n" + "Your response has **" + wordCount + "** words. This is within the word limit, however, you can use more.");
        //        }
        //        else if (wordCount == 10)
        //        {
        //            await ReplyAsync(":white_check_mark: **Your response has been recorded!** :white_check_mark:\n\nYour response is recorded as: **" + response + "**\n\n" + "Your response has **" + wordCount + "** words. Perfect!");
        //        }
        //        else if (wordCount > 10)
        //        {
        //            await ReplyAsync(":white_check_mark: **Your response has been recorded!** :white_check_mark:\n\nYour response is recorded as: **" + response + "**\n\n" + "Your response has **" + wordCount + "** words. This is above the word limit. You are allowed to change your response.");
        //        }
        //        FS.Close();
        //    }
        //}

        //[Command("edit")]
        //[Summary("Edit your response")]
        //public async Task _edit([Remainder] string response)
        //{
        //    await ReplyAsync(":o: Editing your response. Please wait. :o:");
        //    StreamReader FR = new StreamReader("Responses.csv");
        //    bool userFound = false;
        //    while (!FR.EndOfStream) {
        //        string[] line = FR.ReadLine().Split();
        //        for(int a = 0; a < line.Length - 1; a++) {
        //            if (line[a] == Context.User.Username + "#" + Context.User.Discriminator) {
        //                userFound = true;
        //                break;
        //            }
        //        }
        //    }
        //    if (userFound) {
        //        TextWriter FS = new StreamWriter("Responses.csv", true);
        //        response = response.Trim();
        //        char[] c = response.ToCharArray();
        //        byte wordCount = 1;
        //        for (int a = 0; a < c.Length - 1; a++)
        //        {
        //            if (c[a] == ' ') wordCount++;
        //        }
        //        FS.WriteLine(response + "," + wordCount + "," + Context.User.Username + "#" + Context.User.Discriminator);
        //        if (wordCount < 10)
        //        {
        //            await ReplyAsync(":white_check_mark: **Your new response has been recorded!** :white_check_mark:\n\nYour response is recorded as: " + response + "\n\n" + "Your response has " + wordCount + " words. This is within the word limit, however, you can use more.");
        //        }
        //        else if (wordCount == 10)
        //        {
        //            await ReplyAsync(":white_check_mark: **Your new response has been recorded!** :white_check_mark:\n\nYour response is recorded as: " + response + "\n\n" + "Your response has " + wordCount + " words. Perfect!");
        //        }
        //        else if (wordCount > 10)
        //        {
        //            await ReplyAsync(":white_check_mark: **Your new response has been recorded!** :white_check_mark:\n\nYour response is recorded as: " + response + "\n\n" + "Your response has " + wordCount + " words. This is above the word limit. You are allowed to change your response.");
        //        }
        //    } else {
        //        await ReplyAsync("I didn't find your response!");
        //    }
        //}
    }
}
