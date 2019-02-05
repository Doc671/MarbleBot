using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using MarbleBot.BaseClasses;
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
            Global.RaceAlive.Add(0, 0);
            Global.RaceAlive.Remove(0);
            if (Context.IsPrivate) fileID = Context.User.Id; else fileID = Context.Guild.Id;
            EmbedBuilder builder = new EmbedBuilder()
                .WithColor(Global.GetColor(Context))
                .WithCurrentTimestamp();
            switch (command.ToLower()) {
                case "signup": { 
                    var name = "";
                    if (option.IsEmpty() || option.Contains("@")) name = Context.User.Username;
                    else if (option.Length > 100) {
                        await ReplyAsync("Your entry exceeds the 100 character limit.");
                        break;
                    } else option = option.Replace("\n", " "); name = option;
                    builder.AddField("Marble Race: Signed up!", "**" + Context.User.Username + "** has successfully signed up as **" + name + "**!");
                    using (var racers = new StreamWriter("RaceMostUsed.txt", true)) await racers.WriteLineAsync(name);
                    if (!File.Exists(fileID.ToString() + "race.csv")) File.Create(fileID.ToString() + "race.csv");
                    byte alive = 0;
                    if (Context.IsPrivate) {
                        if (!Global.RaceAlive.ContainsKey(Context.User.Id)) Global.RaceAlive.Add(Context.User.Id, 1);
                        else Global.RaceAlive[Context.User.Id]++;
                        alive = Global.RaceAlive[Context.User.Id];
                    } else {
                        if (!Global.RaceAlive.ContainsKey(Context.Guild.Id)) Global.RaceAlive.Add(Context.Guild.Id, 1);
                        else Global.RaceAlive[Context.Guild.Id]++;
                        alive = Global.RaceAlive[Context.Guild.Id];
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
                    if (Context.IsPrivate) canStart = Global.RaceAlive.ContainsKey(Context.User.Id);
                    else canStart = Global.RaceAlive.ContainsKey(Context.Guild.Id);
                    byte marbleCount = 0;
                    using (var marbleList = new StreamReader(fileID.ToString() + "race.csv")) {
                        while (!marbleList.EndOfStream) {
                            var line = await marbleList.ReadLineAsync();
                            if (!line.IsEmpty()) marbleCount++;
                        }
                    }
                    if (marbleCount != 0) {
                        byte oldCount = 255;
                        if (Global.RaceAlive.ContainsKey(fileID)) {
                            oldCount = Global.RaceAlive[fileID];
                            Global.RaceAlive[fileID] = marbleCount;
                        } else Global.RaceAlive.Add(fileID, marbleCount);
                        if (marbleCount != oldCount) await ReplyAsync("Changed the contestant count to " + marbleCount + ".");
                        canStart = true;
                    }
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
                        builder.WithTitle("The race has started!");
                        var msg = await ReplyAsync("", false, builder.Build());
                        await Task.Delay(1500);
                        byte alive = 255;
                        if (Context.IsPrivate) alive = Global.RaceAlive[Context.User.Id];
                        else alive = Global.RaceAlive[Context.Guild.Id];
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
                            await Task.Delay(1500);
                        }
                        if (Context.IsPrivate) Global.RaceAlive.Remove(Context.User.Id);
                        else Global.RaceAlive.Remove(Context.Guild.Id);
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
                                User.RaceWins++;
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
                    }
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
                    if (option == "auto") {
                        byte marbleCount = 0;
                        using (var marbleList = new StreamReader(fileID.ToString() + "race.csv")) {
                            while (!marbleList.EndOfStream) {
                                var line = await marbleList.ReadLineAsync();
                                if (!line.IsEmpty()) marbleCount++;
                            }
                        }
                        if (Global.RaceAlive.ContainsKey(fileID)) Global.RaceAlive[fileID] = marbleCount;
                        else Global.RaceAlive.Add(fileID, marbleCount);
                        await ReplyAsync("Changed the contestant count to " + marbleCount + ".");
                    } else if (Context.User.Id == 224267581370925056 || Context.IsPrivate) {
                        var newcount = byte.Parse(option);
                        if (Global.RaceAlive.ContainsKey(fileID)) Global.RaceAlive[fileID] = newcount;
                        else Global.RaceAlive.Add(fileID, newcount);
                        await ReplyAsync("Changed the contestant count to " + newcount + ".");
                    }
                    break;
                }
                case "contestants": {
                    var marbles = new StringBuilder();
                    byte count = 0;
                    using (var marbleList = new StreamReader(fileID.ToString() + "race.csv")) {
                        var allMarbles = (await marbleList.ReadToEndAsync()).Split('\n');
                        foreach (var marble in allMarbles) {
                            if (marble.Length > 16) {
                                marbles.AppendLine(marble.Split(',')[0]);
                                count++;
                            }
                        }
                    }
                    if (marbles.ToString().IsEmpty()) await ReplyAsync("It looks like there aren't any contestants...");
                    else {
                        builder.AddField("Contestants", marbles.ToString());
                        builder.WithFooter("Contestant count: " + count)
                            .WithTitle("Marble Race: Contestants");
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
                case "remove": {
                    var found = false;
                    var wholeFile = new StringBuilder();
                    using (var marbleList = new StreamReader(fileID.ToString() + "race.csv")) {
                        while (!marbleList.EndOfStream) {
                            var line = await marbleList.ReadLineAsync();
                            if (line.Split(',')[0] == option) found = true;
                            else wholeFile.AppendLine(line);
                        }
                    }
                    if (found) {
                        using (var marbleList = new StreamWriter(fileID.ToString() + "race.csv")) {
                            await marbleList.WriteAsync(wholeFile.ToString());
                            Global.RaceAlive[fileID]--;
                            await ReplyAsync("Removed contestant **" + option + "**!");
                        }
                    }
                    else await ReplyAsync("Could not find the requested racer!");
                    break;
                }
                default: {
                    builder.AddField("How to play", "Use `mb/race signup <marble name>` to sign up as a marble!\nWhen everyone's done, use `mb/race start`! This happens automatically if 10 people have signed up.\n\nCheck who's participating with `mb/race contestants`!\n\nYou can earn Units of Money if you win! (6 hour cooldown)")
                        .WithTitle("Marble Race!");
                    await ReplyAsync("", false, builder.Build());
                    break;
                }
            }
        }

        [Command("siege")]
        [Summary("Participate in a Marble Siege battle")]
        public async Task _siege(string command = "", [Remainder] string option = "") {
            await Context.Channel.TriggerTypingAsync();
            ulong fileID = 0ul;
            if (Context.IsPrivate) fileID = Context.User.Id; else fileID = Context.Guild.Id;
            bool IsBetween(int no, int lower, int upper) { return lower <= no && no <= upper; }
            EmbedBuilder builder = new EmbedBuilder()
                .WithColor(Global.GetColor(Context))
                .WithCurrentTimestamp();
            switch (command.ToLower()) {
                case "signup": { 
                    var name = "";
                    if (option.IsEmpty() || option.Contains("@")) name = Context.User.Username;
                    else if (option.Length > 100) {
                        await ReplyAsync("Your entry exceeds the 100 character limit.");
                        break;
                    } else if (Global.SiegeInfo.ContainsKey(fileID)) {
                        if (Global.SiegeInfo[fileID].Active) {
                            await ReplyAsync("A battle is currently ongoing!");
                            break;
                        }
                    }
                    if (!File.Exists(fileID.ToString() + "siege.csv")) File.Create(fileID.ToString() + "siege.csv");
                    var found = false;
                    using (var marbleList = new StreamReader(fileID.ToString() + "siege.csv")) {
                        while (!marbleList.EndOfStream) {
                            var line = (await marbleList.ReadLineAsync()).Split(',');
                            found = line[1].Contains(Context.User.Id.ToString()) || line[1] == Context.User.Id.ToString();
                        }
                    }
                    if (found) {
                        await ReplyAsync("You've already joined!");
                        break;
                    }
                    option = option.Replace("\n", " "); name = option;
                    builder.AddField("Marble Siege: Signed up!", "**" + Context.User.Username + "** has successfully signed up as **" + name + "**!");
                    using (var Siegers = new StreamWriter("SiegeMostUsed.txt", true)) await Siegers.WriteLineAsync(name);
                    if (name.Contains(',')) {
                        var newName = new char[name.Length];
                        for (int i = 0; i < name.Length - 1; i++) {
                            if (name[i] == ',') newName[i] = ';';
                            else newName[i] = name[i];
                        }
                        name = new string(newName);
                    }
                    using (var marbleList = new StreamWriter(fileID.ToString() + "siege.csv", true)) {
                        await marbleList.WriteLineAsync(name + "," + Context.User.Id);
                        marbleList.Close();
                    }
                    byte alive = 0;
                    using (var marbleList = new StreamReader(fileID.ToString() + "siege.csv")) {
                        var allLines = (await marbleList.ReadToEndAsync()).Split('\n');
                        alive = (byte)allLines.Length;
                        marbleList.Close();
                    }
                    await ReplyAsync("", false, builder.Build());
                    if (alive > 19) {
                        await ReplyAsync("The limit of 20 contestants has been reached!");
                        await _siege("start");
                    }
                    break;
                }
                case "start": {
                    byte marbleCount = 0;
                    using (var marbleList = new StreamReader(fileID.ToString() + "siege.csv")) {
                        while (!marbleList.EndOfStream) {
                            var line = await marbleList.ReadLineAsync();
                            if (!line.IsEmpty()) marbleCount++;
                            var sLine = line.Split(',');
                            var marble = new Marble() {
                                Id = ulong.Parse(sLine[1]),
                                Name = sLine[0],
                                HP = 20
                            };
                            if (Global.SiegeInfo.ContainsKey(fileID)) {
                                if (!Global.SiegeInfo[fileID].Marbles.Contains(marble)) Global.SiegeInfo[fileID].Marbles.Add(marble);
                            } else Global.SiegeInfo.Add(fileID, new Siege(new Marble[] { marble }));
                        }
                    }
                    if (marbleCount == 0) {
                        await ReplyAsync("It doesn't look like anyone has signed up!");
                    } else {
                        Global.SiegeInfo[fileID].Active = true;
                        if (IsBetween(Global.SiegeInfo[fileID].Marbles.Count, 0, 3)) Global.SiegeInfo[fileID].Boss = Global.PreeTheTree;
                        else if (IsBetween(Global.SiegeInfo[fileID].Marbles.Count, 4, 7)) Global.SiegeInfo[fileID].Boss = Global.HATTMANN;
                        else if (IsBetween(Global.SiegeInfo[fileID].Marbles.Count, 8, 11)) Global.SiegeInfo[fileID].Boss = Global.Orange;
                        else if (IsBetween(Global.SiegeInfo[fileID].Marbles.Count, 12, 15)) Global.SiegeInfo[fileID].Boss = Global.Green;
                        else Global.SiegeInfo[fileID].Boss = Global.Destroyer;
                        var cdown = await ReplyAsync("**3**");
                        await Task.Delay(1000);
                        await cdown.ModifyAsync(m => m.Content = "**2**");
                        await Task.Delay(1000);
                        await cdown.ModifyAsync(m => m.Content = "**1**");
                        await Task.Delay(1000);
                        await cdown.ModifyAsync(m => m.Content = "**BEGIN THE SIEGE!**");
                        Global.Sieges.Add(Task.Run(async () => { await SiegeBossActions(fileID); }));
                        var marbles = new StringBuilder();
                        foreach (var marble in Global.SiegeInfo[fileID].Marbles) {
                            marbles.AppendLine($"**{marble.Name}** [{Context.Client.GetUser(marble.Id).Username}#{Context.Client.GetUser(marble.Id).Discriminator}]");
                        }
                        builder.WithTitle("The Siege has begun!")
                            .WithDescription("Get ready! Use `mb/siege attack` to attack and `mb/siege grab` to grab power-ups when they appear!")
                            .WithThumbnailUrl(Global.SiegeInfo[fileID].Boss.ImageUrl)
                            .AddField($"Marbles: **{Global.SiegeInfo[fileID].Marbles.Count}**", marbles.ToString())
                            .AddField($"Boss: **{Global.SiegeInfo[fileID].Boss.Name}**", string.Format("HP: **{0}**\nAttacks: **{1}**\nDifficulty: **{2}**", Global.SiegeInfo[fileID].Boss.HP, Global.SiegeInfo[fileID].Boss.Attacks.Length, Enum.GetName(typeof(Difficulty), Global.SiegeInfo[fileID].Boss.Difficulty)));
                        await ReplyAsync("", false, builder.Build());
                    }
                    break;
                }
                case "attack": {
                    if (Global.SiegeInfo.ContainsKey(fileID)) {
                        var allIDs = new List<ulong>();
                        foreach (var marble in Global.SiegeInfo[fileID].Marbles) allIDs.Add(marble.Id);
                        if (allIDs.Contains(Context.User.Id)) {
                            var marble = Global.SiegeInfo[fileID].Marbles.Find(m => m.Id == Context.User.Id);
                            if (marble.HP > 0) {
                                if (DateTime.UtcNow.Subtract(Global.SiegeInfo[fileID].LastMorale).TotalSeconds > 20 && Global.SiegeInfo[fileID].Morales > 0) {
                                    Global.SiegeInfo[fileID].Morales--;
                                    Global.SiegeInfo[fileID].DMGMultiplier /= 2;
                                    await ReplyAsync($"The effects of a Morale Boost power-up have worn off! The damage multiplier is now **{Global.SiegeInfo[fileID].DMGMultiplier}**!");
                                }
                                var dmg = Global.Rand.Next(1, 25);
                                if (dmg > 20) dmg = Global.Rand.Next(21, 35);
                                var title = "";
                                if (IsBetween(dmg, 1, 7)) title = "Slow attack!";
                                else if (IsBetween(dmg, 8, 14)) title = "Fast attack!";
                                else if (IsBetween(dmg, 15, 20)) title = "Brutal attack!";
                                else if (IsBetween(dmg, 21, 35)) title = "CRITICAL attack!";
                                else title = "Glitch attack!";
                                dmg = (int)Math.Round(dmg * Global.SiegeInfo[fileID].DMGMultiplier);
                                var clone = false;
                                if (marble.Cloned) {
                                    clone = true;
                                    Global.SiegeInfo[fileID].Boss.HP -= dmg * 5;
                                    builder.AddField("Clones attack!", $"Each of the clones dealt **{dmg}** damage to the boss!");
                                    Global.SiegeInfo[fileID].Marbles.Find(m => m.Id == Context.User.Id).Cloned = false;
                                }
                                Global.SiegeInfo[fileID].Boss.HP -= dmg;
                                Global.SiegeInfo[fileID].Marbles.Find(m => m.Id == Context.User.Id).BossHits += dmg;
                                if (Global.SiegeInfo[fileID].Boss.HP < 1) {
                                    Global.SiegeInfo[fileID].Boss.HP = 0;
                                }
                                builder.WithTitle(title)
                                    .WithDescription(string.Format("**{0}** dealt **{1}** damage to **{2}**!", Global.SiegeInfo[fileID].Marbles.Find(m => m.Id == Context.User.Id).Name, dmg, Global.SiegeInfo[fileID].Boss.Name))
                                    .AddField("Boss HP", "**" + Global.SiegeInfo[fileID].Boss.HP + "**/" + Global.SiegeInfo[fileID].Boss.MaxHP);
                                await ReplyAsync("", false, builder.Build());
                                if (clone && marble.Name[marble.Name.Length - 1] != 's') await ReplyAsync($"{marble.Name}'s clones disappeared!");
                                else if (clone) await ReplyAsync($"{marble.Name}' clones disappeared!");
                                if (Global.SiegeInfo[fileID].Boss.HP < 1) {
                                    await SiegeVictory(fileID);
                                }
                            } else await ReplyAsync("**" + Context.User.Username + "**, you are out and can no longer attack!");
                        } else await ReplyAsync("**" + Context.User.Username + "**, you aren't in this Siege!");
                    } else await ReplyAsync("There is no currently ongoing Siege!");
                    break;
                }
                case "bonk": goto case "attack";
                case "grab": {
                    if (Global.SiegeInfo[fileID].Marbles.Any(m => m.Id == Context.User.Id)) {
                        if (Global.SiegeInfo[fileID].PowerUp.IsEmpty()) await ReplyAsync("There is no power-up to grab!");
                        else {
                            if (Global.Rand.Next(0, 3) == 0) {
                                Global.SiegeInfo[fileID].Marbles.Find(m => m.Id == Context.User.Id).PUHits++;
                                switch (Global.SiegeInfo[fileID].PowerUp) {
                                    case "Morale Boost":
                                        Global.SiegeInfo[fileID].DMGMultiplier *= 2;
                                        builder.WithTitle("POWER-UP ACTIVATED!")
                                            .WithDescription(string.Format("**{0}** activated **{1}**! Damage multiplier increased to **{2}**!", Global.SiegeInfo[fileID].Marbles.Find(m => m.Id == Context.User.Id).Name, Global.SiegeInfo[fileID].PowerUp, Global.SiegeInfo[fileID].DMGMultiplier));
                                        await ReplyAsync("", false, builder.Build());
                                        Global.SiegeInfo[fileID].SetPowerUp("");
                                        Global.SiegeInfo[fileID].Morales++;
                                        Global.SiegeInfo[fileID].LastMorale = DateTime.UtcNow;
                                        break;
                                    case "Clone":
                                        Global.SiegeInfo[fileID].Marbles.Find(m => m.Id == Context.User.Id).Cloned = true;
                                        builder.WithTitle("POWER-UP ACTIVATED!")
                                            .WithDescription(string.Format("**{0}** activated **{1}**! Five clones of {0} appeared!", Global.SiegeInfo[fileID].Marbles.Find(m => m.Id == Context.User.Id).Name, Global.SiegeInfo[fileID].PowerUp));
                                        await ReplyAsync("", false, builder.Build());
                                        Global.SiegeInfo[fileID].SetPowerUp("");
                                        break;
                                }
                            } else await ReplyAsync("You failed to grab the power-up!");
                        }
                    } else await ReplyAsync($"**{Context.User.Username}**, you aren't in this Siege!");
                    break;
                }
                case "clear": {
                    if (Context.User.Id == 224267581370925056 || Context.IsPrivate) {
                        using (var marbleList = new StreamWriter(fileID.ToString() + "siege.csv")) {
                            await marbleList.WriteAsync("");
                            await ReplyAsync("Contestant list successfully cleared!");
                            marbleList.Close();
                        }
                    }
                    break;
                }
                case "contestants": {
                    var marbles = new StringBuilder();
                    byte cCount = 0;
                    using (var marbleList = new StreamReader(fileID.ToString() + "siege.csv")) {
                        var allMarbles = (await marbleList.ReadToEndAsync()).Split('\n');
                        foreach (var marble in allMarbles) {
                            if (marble.Length > 16) {
                                var mSplit = marble.Split(',');
                                var user = Context.Client.GetUser(ulong.Parse(mSplit[1].Trim('\n')));
                                marbles.AppendLine($"**{mSplit[0].Trim('\n')}** [{user.Username}#{user.Discriminator}]");
                                cCount++;
                            }
                        }
                    }
                    if (marbles.ToString().IsEmpty()) await ReplyAsync("It looks like there aren't any contestants...");
                    else {
                        builder.AddField("Contestants", marbles.ToString());
                        builder.WithFooter("Contestant count: " + cCount)
                            .WithTitle("Marble Siege: Contestants");
                        await ReplyAsync("", false, builder.Build());
                    }
                    break;
                }
                case "marbles": goto case "contestants";
                case "participants": goto case "contestants";
                case "info": {
                    var marbles = new StringBuilder();
                    builder.WithTitle("Siege Info");
                    if (Global.SiegeInfo.ContainsKey(fileID)) {
                        var siege = Global.SiegeInfo[fileID];
                        foreach (var marble in siege.Marbles) {
                            marbles.AppendLine($"**{marble.Name}** (HP: **{marble.HP}**/20, DMG: **{marble.BossHits}**) [{Context.Client.GetUser(marble.Id).Username}#{Context.Client.GetUser(marble.Id).Discriminator}]");
                        }
                        var PU = siege.PowerUp.IsEmpty() ? "None" : siege.PowerUp;
                        builder.AddField($"Boss: **{siege.Boss.Name}**", string.Format("\nHP: **{0}**/{3}\nAttacks: **{1}**\nDifficulty: **{2}**", siege.Boss.HP, siege.Boss.Attacks.Length, Enum.GetName(typeof(Difficulty), siege.Boss.Difficulty), siege.Boss.MaxHP))
                            .AddField($"Marbles: **{siege.Marbles.Count}**", marbles.ToString())
                            .WithDescription($"Damage Multiplier: **{siege.DMGMultiplier}**\nActive Power-up: **{PU}**")
                            .WithThumbnailUrl(siege.Boss.ImageUrl);
                    } else { 
                        using (var marbleList = new StreamReader(fileID.ToString() + "siege.csv")) {
                            var allMarbles = (await marbleList.ReadToEndAsync()).Split('\n');
                            foreach (var marble in allMarbles) {
                                if (marble.Length > 16) {
                                    var mSplit = marble.Split(',');
                                    var user = Context.Client.GetUser(ulong.Parse(mSplit[1].Trim('\n')));
                                    marbles.AppendLine($"**{mSplit[0].Trim('\n')}** [{user.Username}#{user.Discriminator}]");
                                }
                            }
                        }
                        builder.AddField("Marbles", marbles.ToString())
                            .WithDescription("Siege not started yet.");
                    }
                    await ReplyAsync("", false, builder.Build());
                    break;
                }
                case "boss": {
                    var boss = Boss.Empty;
                    var found = true;
                    switch (option.ToLower().RemoveChar(' ')) {
                        case "preethetree": boss = Global.PreeTheTree; break;
                        case "hattmann": boss = Global.HATTMANN; break;
                        case "orange": boss = Global.Orange; break;
                        case "green": boss = Global.Green; break;
                        case "destroyer": boss = Global.Destroyer; break;
                        default: found = false; break;
                    }
                    if (found) {
                        var attacks = new StringBuilder();
                        foreach (var attack in boss.Attacks) attacks.AppendLine($"**{attack.Name}** (Accuracy: {attack.Accuracy}%) [Damage: {attack.Damage}]");
                        builder.AddField("HP", $"**{boss.MaxHP}**")
                            .AddField("Attacks", attacks.ToString())
                            .AddField("Difficulty", $"**{Enum.GetName(typeof(Difficulty), boss.Difficulty)}** {(int)boss.Difficulty}/10")
                            .WithThumbnailUrl(boss.ImageUrl)
                            .WithTitle(boss.Name);
                        await ReplyAsync("", false, builder.Build());
                    } else await ReplyAsync("Could not find the requested boss!");
                    break;
                }
                case "bossinfo": goto case "boss";
                case "powerup": {
                    var powerup = "";
                    var desc = "";
                    var url = "";
                    switch (option.ToLower().RemoveChar(' ')) {
                        case "clone": powerup = "Clone";
                            desc = "Spawns five clones of a marble which all attack with the marble then die.";
                            url = "https://cdn.discordapp.com/attachments/296376584238137355/541373091495018496/PUClone.png";
                            break;
                        case "moraleboost": powerup = "Morale Boost";
                            desc = "Doubles the Damage Multiplier for 20 seconds.";
                            url = "https://cdn.discordapp.com/attachments/296376584238137355/541373099090903070/PUMoraleBoost.png";
                            break;
                    }
                    if (powerup.IsEmpty()) await ReplyAsync("Could not find the requested power-up!");
                    else {
                        builder.WithDescription(desc)
                            .WithThumbnailUrl(url)
                            .WithTitle(powerup);
                        await ReplyAsync("", false, builder.Build());
                    }
                    break;
                }
                case "power-up": goto case "powerup";
                case "power-upinfo": goto case "powerup";
                case "powerupinfo": goto case "powerup";
                case "puinfo": goto case "powerup";
                default: {
                    var sb = new StringBuilder();
                    sb.AppendLine("Use `mb/siege signup <marble name>` to sign up as a marble! (you can only sign up once)");
                    sb.AppendLine("When everyone's done, use `mb/siege start`! The Siege begins automatically when 20 people have signed up.\n");
                    sb.AppendLine("When the Siege begins, use `mb/siege attack` to attack the boss!");
                    sb.AppendLine("Power-ups occasionally appear. Use `mb/siege grab` to try and activate the power-up (1/3 chance).\n");
                    sb.AppendLine("Check who's participating with `mb/siege contestants` and view Siege information with `mb/siege info`!");
                    builder.AddField("How to play", sb.ToString())
                        .WithTitle("Marble Siege!");
                    await ReplyAsync("", false, builder.Build());
                    break;
                }
            }
        }

        // Separate task dealing with time-based boss responses
        public async Task SiegeBossActions(ulong Id) {
            var StartTime = DateTime.UtcNow;
            var timeout = false;
            do {
                await Task.Delay(15000);
                if (Global.SiegeInfo[Id].Boss.HP < 1) {
                    await SiegeVictory(Id);
                    break;
                } else if (DateTime.UtcNow.Subtract(StartTime).TotalMinutes >= 10) {
                    timeout = true;
                    break;
                } else {
                    var rand = Global.Rand.Next(0, Global.SiegeInfo[Id].Boss.Attacks.Length);
                    var atk = Global.SiegeInfo[Id].Boss.Attacks[rand];
                    var builder = new EmbedBuilder()
                        .WithColor(Global.GetColor(Context))
                        .WithCurrentTimestamp()
                        .WithDescription($"**{Global.SiegeInfo[Id].Boss.Name}** used **{atk.Name}**!")
                        .WithThumbnailUrl(Global.SiegeInfo[Id].Boss.ImageUrl)
                        .WithTitle($"WARNING: {atk.Name.ToUpper()} INBOUND!");
                    var hits = 0;
                    foreach (var marble in Global.SiegeInfo[Id].Marbles) {
                        if (marble.HP > 0) {
                            var likelihood = Global.Rand.Next(0, 100);
                            if (!(likelihood > atk.Accuracy)) {
                                marble.HP -= atk.Damage;
                                hits++;
                                if (marble.HP < 1) {
                                    marble.HP = 0;
                                    builder.AddField($"**{marble.Name}** has been killed!", $"HP: {marble.HP}/20");
                                } else builder.AddField($"**{marble.Name}** has been damaged!", $"HP: {marble.HP}/20");
                            }
                        }
                    }
                    if (hits < 1) builder.AddField("Missed!", "No-one got hurt!");
                    await Context.Channel.SendMessageAsync("", false, builder.Build());
                    if (DateTime.UtcNow.Subtract(Global.SiegeInfo[Id].LastMorale).TotalSeconds > 20 && Global.SiegeInfo[Id].Morales > 0) {
                        Console.WriteLine(DateTime.UtcNow.Subtract(Global.SiegeInfo[Id].LastMorale).TotalSeconds);
                        Global.SiegeInfo[Id].Morales--;
                        Global.SiegeInfo[Id].DMGMultiplier /= 2;
                        await ReplyAsync($"The effects of a Morale Boost power-up have worn off! The damage multiplier is now **{Global.SiegeInfo[Id].DMGMultiplier}**!");
                    }
                    if (Global.SiegeInfo[Id].Marbles.Aggregate(0, (agg, m) => { agg += m.HP; return agg; }) < 1) {
                        builder = new EmbedBuilder()
                            .WithColor(Global.GetColor(Context))
                            .WithCurrentTimestamp()
                            .WithDescription($"All the marbles died!\n**{Global.SiegeInfo[Id].Boss.Name}** won!")
                            .WithThumbnailUrl(Global.SiegeInfo[Id].Boss.ImageUrl)
                            .WithTitle("Siege Failure!");
                        await Context.Channel.SendMessageAsync("", false, builder.Build());
                        break;
                    }
                    if (Global.SiegeInfo[Id].PowerUp == "") {
                        rand = Global.Rand.Next(0, 4);
                        switch (rand) {
                            case 0: {
                                Global.SiegeInfo[Id].SetPowerUp("Morale Boost");
                                builder = new EmbedBuilder()
                                    .WithColor(Global.GetColor(Context))
                                    .WithCurrentTimestamp()
                                    .WithDescription("A **Morale Boost** power-up has spawned in the arena! Use `mb/siege grab` to try and activate it!")
                                    .WithThumbnailUrl(Global.SiegeInfo[Id].PUImageUrl)
                                    .WithTitle("Power-up spawned!");
                                await Context.Channel.SendMessageAsync("", false, builder.Build());
                                break;
                            }
                            case 1: {
                                Global.SiegeInfo[Id].SetPowerUp("Clone");
                                builder = new EmbedBuilder()
                                    .WithColor(Global.GetColor(Context))
                                    .WithCurrentTimestamp()
                                    .WithDescription("A **Clone** power-up has spawned in the arena! Use `mb/siege grab` to try and activate it!")
                                    .WithThumbnailUrl(Global.SiegeInfo[Id].PUImageUrl)
                                    .WithTitle("Power-up spawned!");
                                await Context.Channel.SendMessageAsync("", false, builder.Build());
                                break;
                            }
                            default: break;
                        }
                    }
                }
            } while (Global.SiegeInfo[Id].Boss.HP > 0 || !timeout || Global.SiegeInfo[Id].Marbles.Aggregate(0, (agg, m) => { agg += m.HP; return agg; }) > 0);
            if (timeout || Global.SiegeInfo[Id].Marbles.Aggregate(0, (agg, m) => { agg += m.HP; return agg; }) < 1) {
                Global.SiegeInfo[Id].Boss.ResetHP();
                Global.SiegeInfo.Remove(Id);
            }
            using (var marbleList = new StreamWriter(Id + "siege.csv")) {
                await marbleList.WriteAsync("");
                marbleList.Close();
            }
            if (timeout) await Context.Channel.SendMessageAsync("10 minute timeout reached! Siege aborted!");
        }

        public async Task SiegeVictory(ulong Id) {
            var siege = Global.SiegeInfo[Id];
            var builder = new EmbedBuilder()
                .WithColor(Global.GetColor(Context))
                .WithCurrentTimestamp()
                .WithTitle("Siege Victory!")
                .WithDescription($"**{siege.Boss.Name}** has been defeated!");
            for (int i = 0; i < siege.Marbles.Count; i++) {
                var marble = siege.Marbles[i];
                var json = "";
                using (var users = new StreamReader("Users.json")) json = users.ReadToEnd();
                var obj = JObject.Parse(json);
                var User = Global.GetUser(Context, obj, marble.Id);
                int earnings = marble.BossHits + (marble.PUHits * 50);
                if (DateTime.UtcNow.Subtract(User.LastSiegeWin).TotalHours > 6) {
                    var output = new StringBuilder();
                    output.AppendLine($"Damage dealt: {Global.UoM}**{marble.BossHits:n}**");
                    if (marble.PUHits > 0) output.AppendLine($"Power-ups grabbed (x50): {Global.UoM}**{marble.PUHits * 50:n}**");
                    if (marble.HP > 0) {
                        earnings += 200;
                        output.AppendLine($"Alive bonus: {Global.UoM}**{200:n}**");
                        User.SiegeWins++;
                        if (earnings == 200) {
                            earnings = 0;
                            output.AppendLine($"Did nothing: {Global.UoM}**-{200:n}**");
                        }
                    }
                    output.AppendLine($"__**Total: {Global.UoM}{earnings:n}**__");
                    User.Balance += earnings;
                    User.NetWorth += earnings;
                    builder.AddField($"**{Context.Client.GetUser(marble.Id).Username}**'s earnings", output.ToString());
                }
                if (marble.HP > 0 && earnings > 0) User.LastSiegeWin = DateTime.UtcNow;
                obj.Remove(marble.Id.ToString());
                obj.Add(new JProperty(marble.Id.ToString(), JObject.FromObject(User)));
                using (var users = new StreamWriter("Users.json")) {
                    using (var users2 = new JsonTextWriter(users)) {
                        var Serialiser = new JsonSerializer() { Formatting = Formatting.Indented };
                        Serialiser.Serialize(users2, obj);
                    }
                }
            }
            await ReplyAsync("", false, builder.Build());
            Global.SiegeInfo[Id].Boss.ResetHP();
            Global.SiegeInfo.Remove(Id);
            using (var marbleList = new StreamWriter(Id.ToString() + "siege.csv")) {
                await marbleList.WriteAsync("");
                marbleList.Close();
            }
        }
    }
}