using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using MarbleBot.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MarbleBot.Modules
{
    public class Games : ModuleBase<SocketCommandContext>
    {
        /// <summary>
        /// Game commands
        /// </summary>

        [Command("race")]
        [Summary("Players compete in a marble race!")]
        public async Task _race(string command = "", [Remainder] string option = "") {
            await Context.Channel.TriggerTypingAsync();
            ulong fileID = 0ul;
            Global.Alive.Add(0, 0);
            Global.Alive.Remove(0);
            if (Context.IsPrivate) fileID = Context.User.Id; else fileID = Context.Guild.Id;
            EmbedBuilder builder = new EmbedBuilder()
                .WithColor(Global.GetColor(Context))
                .WithCurrentTimestamp();
            switch (command.ToLower()) {
                case "signup": { 
                    var name = "";
                    if (option.IsEmpty() || option.Contains("@")) name = Context.User.Username;
                    else if (option.Length > 100) await ReplyAsync("Your entry exceeds the 100 character limit.");
                    else option = option.Replace("\n", " "); name = option;
                    builder.AddField("Signed up!", "**" + Context.User.Username + "** has successfully signed up as **" + name + "**!");
                    using (var racers = new StreamWriter("RaceMostUsed.txt", true)) await racers.WriteLineAsync(name);
                    if (!File.Exists(fileID.ToString() + "race.csv")) File.Create(fileID.ToString() + "race.csv");
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
                    if (name.Contains(',')) {
                        var newName = new char[name.Length];
                        for (int i = 0; i < name.Length - 1; i++) {
                            if (name[i] == ',') newName[i] = ';';
                            else newName[i] = name[i];
                        }
                        name = new string(newName);
                    }
                    using (var marbleList = new StreamWriter(fileID.ToString() + "race.csv", true)) {
                        await marbleList.WriteLineAsync(name + "," + Context.User.Id);
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
                        var marbles = new List<Tuple<string, ulong>>();
                        using (var marbleList = new StreamReader(fileID.ToString() + "race.csv")) {
                            while (!marbleList.EndOfStream) {
                                var line = (await marbleList.ReadLineAsync()).Split(',');
                                marbles.Add(Tuple.Create(line[0], ulong.Parse(line[1])));
                            }
                            marbleList.Close();
                        }
                        Global.RaceActive = true;
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
                                eliminated = Global.Rand.Next(0, id);
                            } while (marbles[eliminated].Item1 == "///out");
                            var deathmsg = "";
                            var msgs = new List<string>();
                            byte msgCount = 0;
                            using (var msgFile = new StreamReader("racedeathmsgs.txt")) {
                                while (!msgFile.EndOfStream) {
                                    msgCount++;
                                    msgs.Add(await msgFile.ReadLineAsync());
                                }
                            }
                            int choice = Global.Rand.Next(0, (msgCount - 1));
                            deathmsg = msgs[choice];
                            builder.AddField("**" + marbles[eliminated].Item1 + "** is eliminated!", marbles[eliminated].Item1 + " " + deathmsg + " and is now out of the competition!");
                            marbles[eliminated] = Tuple.Create("///out", marbles[eliminated].Item2);
                            alive--;
                            await msg.ModifyAsync(_msg => _msg.Embed = builder.Build());
                            Thread.Sleep(1500);
                        }
                        if (Context.IsPrivate) Global.Alive.Remove(Context.User.Id);
                        else Global.Alive.Remove(Context.Guild.Id);
                        var winnerID = 0ul;
                        foreach (var marble in marbles) {
                            if (marble.Item1 != "///out") {
                                winnerID = marble.Item2;
                                builder.AddField("**" + marble.Item1 + "** wins!", marble.Item1 + " is the winner!");
                                using (var racers = new StreamWriter("RaceWinners.txt", true)) await racers.WriteLineAsync(marble.Item1);
                                await msg.ModifyAsync(_msg => _msg.Embed = builder.Build());
                                await ReplyAsync("**" + marble.Item1 + "** won the race!");
                                break;
                            }
                        }
                        var json = "";
                        using (var users = new StreamReader("Users.json")) json = users.ReadToEnd();
                        var obj = JObject.Parse(json);
                        var User = Global.GetUser(Context, obj, winnerID);
                        if (DateTime.UtcNow.Subtract(User.LastRaceWin).TotalHours > 6) {
                            var noOfSameUser = 0;
                            foreach (var marble in marbles) if (marble.Item2 == winnerID) noOfSameUser++;
                            var gift = Convert.ToDecimal(Math.Round(((Convert.ToDouble(id) / noOfSameUser) - 1) * 100, 2));
                            if (gift > 0) {
                                User.Balance += gift;
                                User.NetWorth += gift;
                                User.LastRaceWin = DateTime.UtcNow;
                                obj.Remove(winnerID.ToString());
                                obj.Add(new JProperty(winnerID.ToString(), JObject.FromObject(User)));
                                using (var users = new StreamWriter("Users.json")) {
                                    using (var users2 = new JsonTextWriter(users)) {
                                        var Serialiser = new JsonSerializer() { Formatting = Formatting.Indented };
                                        Serialiser.Serialize(users2, obj);
                                    }
                                }
                                await ReplyAsync(string.Format("**{0}** won <:unitofmoney:372385317581488128>**{1:n}** for winning the race!", User.Name, gift));
                            }
                        }
                        using (var marbleList = new StreamWriter(fileID.ToString() + "race.csv")) {
                            await marbleList.WriteAsync("");
                            marbleList.Close();
                        }
                        Global.RaceActive = false;
                    }
                    break;
                }
                case "bet": {
                    /*using (var marbleList = new StreamReader(fileID.ToString() + "race.csv")) {
                        var marble = await marbleList.ReadLineAsync();
                    }*/
                    break;
                }
                case "clear": {
                    if (Context.User.Id == 224267581370925056 || Context.IsPrivate) {
                        using (var marbleList = new StreamWriter(fileID.ToString() + "race.csv")) {
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
                            else Global.Alive.Add(Context.Guild.Id, newcount);
                            await ReplyAsync("Changed the contestant count to " + newcount + ".");
                        }
                    }
                    break;
                }
                case "contestants": {
                    var marbles = new StringBuilder();
                    using (var marbleList = new StreamReader(fileID.ToString() + "race.csv")) {
                        while (!marbleList.EndOfStream) {
                            var allMarbles = (await marbleList.ReadLineAsync()).Split('\n');
                            foreach (var marble in allMarbles) marbles.Append(marble.Split(',')[0] + "\n");
                        }
                    }
                    if (marbles.ToString().IsEmpty()) await ReplyAsync("It looks like there aren't any contestants...");
                    else {
                        builder.AddField("Contestants", marbles.ToString());
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
                            using (var win = new StreamReader("RaceWinners.csv")) {
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
                            var desc = new StringBuilder();
                            foreach (var winner in winList) {
                                if (i < 11) {
                                    desc.Append(string.Format("{0}{1}: {2} {3}\n", new string[] { i.ToString(), i.Ordinal(), winner.Item1, winner.Item2.ToString() }));
                                    if (j < winners.Count) if (!(winList[j].Item2 == winner.Item2)) i++;
                                    j++;
                                } else break;
                            }
                            builder.WithTitle("Race Leaderboard: Winners")
                                .WithDescription(desc.ToString());
                            await ReplyAsync("", false, builder.Build());
                            break;
                        }
                        case "mostused": {
                            var winners = new SortedDictionary<string, int>();
                            using (var win = new StreamReader("RaceMostUsed.csv")) {
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
                            var desc = new StringBuilder();
                            foreach (var winner in winList) {
                                if (i < 11) {
                                    desc.Append(string.Format("{0}{1}: {2} {3}\n", new string[] { i.ToString(), i.Ordinal(), winner.Item1, winner.Item2.ToString() }));
                                    if (j < winners.Count) if (!(winList[j].Item2 == winner.Item2)) i++;
                                    j++;
                                }
                                else break;
                            }
                            builder.WithTitle("Race Leaderboard: Most Used")
                                .WithDescription(desc.ToString());
                            await ReplyAsync("", false, builder.Build());
                            break;
                        }
                    }
                    break;
                }
                case "checkearn": {
                    var User = Global.GetUser(Context);
                    var nextDaily = DateTime.UtcNow.Subtract(User.LastRaceWin);
                    var output = "";
                    if (nextDaily.TotalHours < 6) output = string.Format("You can earn money from racing in **{0}**!", Global.GetDateString(User.LastRaceWin.Subtract(DateTime.UtcNow.AddHours(-6))));
                    else output = "You can earn money from racing now!";
                    builder.WithAuthor(Context.User)
                        .WithDescription(output);
                    await ReplyAsync("", false, builder.Build());
                    break;
                }
                default: {
                    builder.AddField("How to play", "Use `mb/race signup [marble name]` to sign up as a marble!\nWhen everyone's done (or when 10 people have signed up), use `mb/race start`!\n\nCheck who's participating with `mb/race contestants`!\n\nYou can earn Units of Money if you win! (6 hour cooldown)")
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