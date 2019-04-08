using Discord;
using Discord.Commands;
using MarbleBot.BaseClasses;
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
    /// <summary> Game commands. </summary>
    public class Games : MarbleBotModule
    {
        [Command("race")]
        [Summary("Participate in a marble race!")]
        public async Task RaceCommandAsync(string command = "", [Remainder] string option = "") {
            await Context.Channel.TriggerTypingAsync();
            ulong fileID = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
            EmbedBuilder builder = new EmbedBuilder()
                .WithColor(GetColor(Context))
                .WithCurrentTimestamp();

            switch (command.ToLower()) {
                case "signup": { 
                    var name = "";
                    if (option.IsEmpty() || option.Contains("@")) name = Context.User.Username;
                    else if (option.Length > 100) {
                        await ReplyAsync("Your entry exceeds the 100 character limit.");
                        break;
                    } else {
                        option = option.Replace("\n", " ").Replace(",", ";");
                        name = option;
                    }
                    builder.AddField("Marble Race: Signed up!", "**" + Context.User.Username + "** has successfully signed up as **" + name + "**!");
                    using (var racers = new StreamWriter("Resources\\RaceMostUsed.txt", true)) await racers.WriteLineAsync(name);
                    if (!File.Exists(fileID.ToString() + "race.csv")) File.Create(fileID.ToString() + "race.csv").Close();
                    byte alive = 0;
                    using (var marbleList = new StreamReader(fileID.ToString() + "race.csv", true)) {
                        while (!marbleList.EndOfStream) {
                            var line = await marbleList.ReadLineAsync();
                            if (!(line.IsEmpty())) alive++;
                        }
                        marbleList.Close();
                    }
                    using (var marbleList = new StreamWriter(fileID.ToString() + "race.csv", true)) {
                        await marbleList.WriteLineAsync(name + "," + Context.User.Id);
                        marbleList.Close();
                    }
                    await ReplyAsync(embed: builder.Build());
                    if (alive > 9) {
                        await ReplyAsync("The limit of 10 contestants has been reached!");
                        await RaceCommandAsync("start");
                    }
                    break;
                }
                case "join": goto case "signup";
                case "start": {
                    byte marbleCount = 0;
                    using (var marbleList = new StreamReader(fileID.ToString() + "race.csv")) {
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
                        using (var marbleList = new StreamReader(fileID.ToString() + "race.csv")) {
                            while (!marbleList.EndOfStream) {
                                var line = (await marbleList.ReadLineAsync()).Split(',');
                                marbles.Add(Tuple.Create(line[0], ulong.Parse(line[1])));
                            }
                            marbleList.Close();
                        }
                        Global.RaceAlive.Add(fileID, marbleCount);

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
                        using (var marbleList = new StreamWriter(fileID.ToString() + "race.csv", false)) {
                            await marbleList.WriteAsync("");
                            marbleList.Close();
                        }
                    }
                    break;
                }
                case "clear": {
                    if (Context.User.Id == 224267581370925056 || Context.IsPrivate) {
                        using (var marbleList = new StreamWriter(fileID.ToString() + "race.csv", false)) {
                            await marbleList.WriteAsync("");
                            await ReplyAsync("Contestant list successfully cleared!");
                            marbleList.Close();
                        }
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
                    break;
                }
                case "marbles": goto case "contestants";
                case "participants": goto case "contestants";
                case "leaderboard": {
                    switch (option.ToLower()) {
                        case "winners": {
                            var winners = new SortedDictionary<string, int>();
                            using (var win = new StreamReader("Resources\\RaceWinners.txt")) {
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
                            await ReplyAsync(embed: builder.Build());
                            break;
                        }
                        case "mostused": {
                            var winners = new SortedDictionary<string, int>();
                            using (var win = new StreamReader("Resources\\RaceMostUsed.txt")) {
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
                            await ReplyAsync(embed: builder.Build());
                            break;
                        }
                    }
                    break;
                }
                case "checkearn": {
                    var User = GetUser(Context);
                    var nextDaily = DateTime.UtcNow.Subtract(User.LastRaceWin);
                    var output = "";
                    if (nextDaily.TotalHours < 6) output = $"You can earn money from racing in **{GetDateString(User.LastRaceWin.Subtract(DateTime.UtcNow.AddHours(-6)))}**!";
                    else output = "You can earn money from racing now!";
                    builder.WithAuthor(Context.User)
                        .WithDescription(output);
                    await ReplyAsync(embed: builder.Build());
                    break;
                }
                case "remove": {
                    byte state = Context.User.Id == 224267581370925056 ? (byte)3 : (byte)0; // 0 - Not found, 1 - Found but not yours, 2 - Found & yours, 3 - Found & overridden
                    var wholeFile = new StringBuilder();
                    var id = 0ul;
                    using (var marbleList = new StreamReader(fileID.ToString() + "race.csv")) {
                        while (!marbleList.EndOfStream) {
                            var line = await marbleList.ReadLineAsync();
                            if (line.Split(',')[0] == option) {
                                if (ulong.Parse(line.Split(',')[1]) == Context.User.Id) {
                                    id = ulong.Parse(line.Split(',')[1]);
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
                        case 2: using (var marbleList = new StreamWriter(fileID.ToString() + "race.csv", false)) {
                                await marbleList.WriteAsync(wholeFile.ToString());
                                await ReplyAsync("Removed contestant **" + option + "**!");
                            }
                            break;
                        case 3: goto case 2;
                    }
                    break;
                }
                default: {
                    builder.AddField("How to play", 
                        "Use `mb/race signup <marble name>` to sign up as a marble!\nWhen everyone's done, use `mb/race start`! This happens automatically if 10 people have signed up.\n\nCheck who's participating with `mb/race contestants`!\n\nYou can earn Units of Money if you win! (6 hour cooldown)")
                        .WithTitle("Marble Race!");
                    await ReplyAsync(embed: builder.Build());
                    break;
                }
            }
        }

        [Command("scavenge")]
        [Summary("Scavenge for items!")]
        public async Task ScavengeCommandAsync([Remainder] string command = "")
        {
            await Context.Channel.TriggerTypingAsync();
            var embed = new EmbedBuilder()
                .WithColor(GetColor(Context))
                .WithCurrentTimestamp();
            var location = ScavengeLocation.CanaryBeach;
            
            var obj = GetUsersObj();
            var user = GetUser(Context, obj);

            switch (command.ToLower().RemoveChar(' ')) {
                case "canarybeach":
                    if (DateTime.UtcNow.Subtract(user.LastScavenge).TotalHours < 6) {
                        var sixHoursAgo = DateTime.UtcNow.AddHours(-6);
                        await ReplyAsync($"**{Context.User.Username}**, you need to wait for **{GetDateString(user.LastScavenge.Subtract(sixHoursAgo))}** until you can scavenge again.");
                    } else {
                        if (Global.ScavengeInfo.ContainsKey(Context.User.Id)) await ReplyAsync($"**{Context.User.Username}**, you are already scavenging!");
                        else {
                            Global.ScavengeInfo.Add(Context.User.Id, new Queue<Item>());
                            Global.ScavengeSessions.Add(Task.Run(async () => { await ScavengeSession(Context, location); }));
                            embed.WithDescription($"**{Context.User.Username}** has begun scavenging in **{Enum.GetName(typeof(ScavengeLocation), location)}**!")
                                .WithTitle("Item Scavenge Begin!");
                            await ReplyAsync(embed: embed.Build());
                        }
                    }
                    break;
                case "grab":
                    if (Global.ScavengeInfo.ContainsKey(Context.User.Id)) {
                        if (Global.ScavengeInfo[Context.User.Id] == null) await ReplyAsync($"**{Context.User.Username}**, there is no item to scavenge!");
                        else {
                            if (Global.ScavengeInfo[Context.User.Id].Count > 0) {
                                var item = Global.ScavengeInfo[Context.User.Id].Dequeue();
                                if (user.Items != null) {
                                    if (user.Items.ContainsKey(item.Id)) user.Items[item.Id]++;
                                    else user.Items.Add(item.Id, 1);
                                } else {
                                    user.Items = new SortedDictionary<int, int> {
                                        { item.Id, 1 }
                                    };
                                }
                                user.NetWorth += item.Price;
                                WriteUsers(obj, Context.User, user);
                                await ReplyAsync($"**{Context.User.Username}**, you have successfully added **{item.Name}** x**1** to your inventory!");
                            } else await ReplyAsync($"**{Context.User.Username}**, there is nothing to grab!");
                        }
                    } else await ReplyAsync($"**{Context.User.Username}**, you are not scavenging!");
                    break;
                case "info":
                    await ReplyAsync("Select a location to view the info of!");
                    break;
                case "infocanarybeach":
                    var output = new StringBuilder();
                    string json2;
                    using (var users = new StreamReader("Resources\\Items.json")) json2 = users.ReadToEnd();
                    var obj2 = JObject.Parse(json2);
                    var items = obj2.ToObject<Dictionary<string, Item>>();
                    foreach (var itemPair in items) {
                        if (itemPair.Value.ScavengeLocation == location)
                            output.AppendLine($"`[{int.Parse(itemPair.Key).ToString("000")}]` {itemPair.Value.Name}");
                    }
                    await ReplyAsync(embed: new EmbedBuilder()
                        .WithColor(GetColor(Context))
                        .WithCurrentTimestamp()
                        .WithDescription(output.ToString())
                        .WithTitle($"Scavenge Location Info: {Enum.GetName(typeof(ScavengeLocation), location)}")
                        .Build());
                    break;
                case "infotreewurld":
                    location = ScavengeLocation.TreeWurld;
                    goto case "infocanarybeach";
                case "locations":
                    embed.WithDescription("Canary Beach\nTree Wurld\n\nUse `mb/scavenge info <location name>` to see which items you can get!")
                        .WithTitle("Scavenge Locations");
                    await ReplyAsync(embed: embed.Build());
                    break;
                case "sell":
                    if (Global.ScavengeInfo.ContainsKey(Context.User.Id)) {
                        if (Global.ScavengeInfo[Context.User.Id] == null) await ReplyAsync($"**{Context.User.Username}**, there is no item to scavenge!");
                        else {
                            if (Global.ScavengeInfo[Context.User.Id].Count > 0) {
                                var item = Global.ScavengeInfo[Context.User.Id].Dequeue();
                                user.Balance += item.Price;
                                user.NetWorth += item.Price;
                                WriteUsers(obj, Context.User, user);
                                await ReplyAsync($"**{Context.User.Username}**, you have successfully sold **{item.Name}** x**1** for {Global.UoM}**{item.Price:n}**!");
                            } else await ReplyAsync($"**{Context.User.Username}**, there is nothing to sell!");
                        }
                    } else await ReplyAsync($"**{Context.User.Username}**, you are not scavenging!");
                    break;
                case "treewurld":
                    location = ScavengeLocation.TreeWurld;
                    goto case "canarybeach";
                default:
                    var helpP1 = "Use `mb/scavenge locations` to see where you can scavenge for items and use `mb/scavenge <location name>` to start a scavenge session!";
                    var helpP2 = "\n\nWhen you find an item, use `mb/scavenge sell` to sell immediately or `mb/scavenge grab` to put the item in your inventory!";
                    var helpP3 = "\n\nScavenge games last for 60 seconds - every 8 seconds there will be a 80% chance that you've found an item.";
                    embed.AddField("How to play", helpP1 + helpP2 + helpP3)
                        .WithTitle("Item Scavenge!");
                    await ReplyAsync(embed: embed.Build());
                    break;
            }
        }

        public async Task ScavengeSession(SocketCommandContext context, ScavengeLocation location)
        {
            var startTime = DateTime.UtcNow;
            var collectableItems = new List<Item>();
            string json;
            using (var users = new StreamReader("Resources\\Items.json")) json = users.ReadToEnd();
            var obj = JObject.Parse(json);
            var items = obj.ToObject<Dictionary<string, Item>>();
            foreach (var itemPair in items) {
                if (itemPair.Value.ScavengeLocation == location) {
                    var outputItem = itemPair.Value;
                    outputItem.Id = int.Parse(itemPair.Key);
                    collectableItems.Add(outputItem);
                }
            }
            do {
                await Task.Delay(8000);
                if (Global.Rand.Next(0, 5) < 4) {
                    var item = collectableItems[Global.Rand.Next(0, collectableItems.Count)];
                    Global.ScavengeInfo[Context.User.Id].Enqueue(item);
                    await ReplyAsync($"**{context.User.Username}**, you have found **{item.Name}** x**1**! Use `mb/scavenge grab` to keep it or `mb/scavenge sell` to sell it.");
                }
            } while (!(DateTime.UtcNow.Subtract(startTime).TotalSeconds > 63));

            string json2;
            using (var users = new StreamReader("Users.json")) json2 = users.ReadToEnd();
            var obj2 = JObject.Parse(json2);
            var user = GetUser(Context, obj2);
            user.LastScavenge = DateTime.UtcNow;
            foreach (var item in Global.ScavengeInfo[context.User.Id]) {
                if (user.Items.ContainsKey(item.Id)) user.Items[item.Id]++;
                else user.Items.Add(item.Id, 1);
            }
            WriteUsers(obj2, Context.User, user);

            Global.ScavengeInfo.Remove(context.User.Id);
            await ReplyAsync("The scavenge session is over! Any remaining items have been added to your inventory!");
        }

        [Command("siege")]
        [Summary("Participate in a Marble Siege boss battle!")]
        public async Task SiegeCommandAsync(string command = "", [Remainder] string option = "") {
            await Context.Channel.TriggerTypingAsync();
            ulong fileID = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
            bool IsBetween(int no, int lower, int upper) { return lower <= no && no <= upper; }
            EmbedBuilder builder = new EmbedBuilder()
                .WithColor(GetColor(Context))
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
                    if (!File.Exists(fileID.ToString() + "siege.csv")) File.Create(fileID.ToString() + "siege.csv").Close();
                    var found = false;
                    using (var marbleList = new StreamReader(fileID.ToString() + "siege.csv")) {
                        while (!marbleList.EndOfStream) {
                            var line = await marbleList.ReadLineAsync();
                            found = line.Contains(Context.User.Id.ToString());
                        }
                    }
                    if (found) {
                        await ReplyAsync("You've already joined!");
                        break;
                    }
                    option = option.Replace("\n", " ").Replace(",", ";");
                    name = option;
                    builder.AddField("Marble Siege: Signed up!", "**" + Context.User.Username + "** has successfully signed up as **" + name + "**!");
                    using (var Siegers = new StreamWriter("Resources\\SiegeMostUsed.txt", true)) await Siegers.WriteLineAsync(name);
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
                    await ReplyAsync(embed: builder.Build());
                    if (alive > 19) {
                        await ReplyAsync("The limit of 20 contestants has been reached!");
                        await SiegeCommandAsync("start");
                    }
                    break;
                }
                case "join": goto case "signup";
                case "start": {
                    if (Global.SiegeInfo.ContainsKey(fileID)) {
                        if (Global.SiegeInfo[fileID].Active) {
                            await ReplyAsync("A battle is currently ongoing!");
                            break;
                        }
                    }

                    // Get marbles
                    byte marbleCount = 0;
                    using (var marbleList = new StreamReader(fileID.ToString() + "siege.csv")) {
                        while (!marbleList.EndOfStream) {
                            var line = await marbleList.ReadLineAsync();
                            if (!line.IsEmpty()) marbleCount++;
                            var sLine = line.Split(',');
                            var marble = new Marble() {
                                Id = ulong.Parse(sLine[1]),
                                Name = sLine[0]
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
                        // Pick boss & set battle stats based on boss
                        if (option.Contains("override") && (Context.User.Id == 224267581370925056 || Context.IsPrivate)) {
                            switch (option.Split(' ')[1].ToLower()) {
                                case "pree": Global.SiegeInfo[fileID].Boss = Global.PreeTheTree; break;
                                case "preethetree": goto case "pree";
                                case "helpme": Global.SiegeInfo[fileID].Boss = Global.HelpMeTheTree; break;
                                case "help": goto case "help";
                                case "helpmethetree": goto case "helpme";
                                case "hattmann": Global.SiegeInfo[fileID].Boss = Global.HATTMANN; break;
                                case "hatt": Global.SiegeInfo[fileID].Boss = Global.HATTMANN; break;
                                case "orange": Global.SiegeInfo[fileID].Boss = Global.Orange; break;
                                case "erango": Global.SiegeInfo[fileID].Boss = Global.Erango; break;
                                case "octopheesh": Global.SiegeInfo[fileID].Boss = Global.Octopheesh; break;
                                case "green": Global.SiegeInfo[fileID].Boss = Global.Green; break;
                                case "destroyer": Global.SiegeInfo[fileID].Boss = Global.Destroyer; break;
                            }
                        } else {
                            var bossWeight = (int)Math.Round(Global.SiegeInfo[fileID].Marbles.Count * ((Global.Rand.NextDouble() * 5) + 1));
                            if (IsBetween(bossWeight, 0, 8)) Global.SiegeInfo[fileID].Boss = Global.PreeTheTree;
                            else if (IsBetween(bossWeight, 9, 16)) Global.SiegeInfo[fileID].Boss = Global.HelpMeTheTree;
                            else if (IsBetween(bossWeight, 17, 24)) Global.SiegeInfo[fileID].Boss = Global.HATTMANN;
                            else if (IsBetween(bossWeight, 25, 32)) Global.SiegeInfo[fileID].Boss = Global.Orange;
                            else if (IsBetween(bossWeight, 33, 40)) Global.SiegeInfo[fileID].Boss = Global.Erango;
                            else if (IsBetween(bossWeight, 41, 48)) Global.SiegeInfo[fileID].Boss = Global.Octopheesh;
                            else if (IsBetween(bossWeight, 49, 56)) Global.SiegeInfo[fileID].Boss = Global.Green;
                            else Global.SiegeInfo[fileID].Boss = Global.Destroyer;
                        }
                        int hp;
                        switch (Global.SiegeInfo[fileID].Boss.Difficulty) {
                            case Difficulty.Trivial: hp = 15; break;
                            case Difficulty.Simple: hp = 20; break;
                            case Difficulty.Easy: hp = 25; break;
                            case Difficulty.Decent: hp = 30; break;
                            case Difficulty.Moderate: hp = 35; break;
                            case Difficulty.Risky: hp = 40; break;
                            case Difficulty.Hard: hp = 45; break;
                            case Difficulty.Extreme: hp = 50; break;
                            case Difficulty.Insane: hp = 55; break;
                            case Difficulty.Demonic: hp = 60; break;
                            default: hp = 15; break;
                        }
                        foreach (var marble in Global.SiegeInfo[fileID].Marbles) {
                            marble.HP = hp;
                            marble.MaxHP = hp;
                        }
                        
                        // Siege Start
                        var cdown = await ReplyAsync("**3**");
                        await Task.Delay(1000);
                        await cdown.ModifyAsync(m => m.Content = "**2**");
                        await Task.Delay(1000);
                        await cdown.ModifyAsync(m => m.Content = "**1**");
                        await Task.Delay(1000);
                        await cdown.ModifyAsync(m => m.Content = "**BEGIN THE SIEGE!**");
                        Global.Sieges.Add(Task.Run(async () => { await SiegeBossActionsAsync(fileID); }));
                        var marbles = new StringBuilder();
                        var pings = new StringBuilder();
                        foreach (var marble in Global.SiegeInfo[fileID].Marbles) {
                            marbles.AppendLine($"**{marble.Name}** [{Context.Client.GetUser(marble.Id).Username}#{Context.Client.GetUser(marble.Id).Discriminator}]");
                            if (GetUser(Context, marble.Id).SiegePing) pings.Append($"<@{marble.Id}> ");
                        }
                        builder.WithTitle("The Siege has begun!")
                            .WithDescription("Get ready! Use `mb/siege attack` to attack and `mb/siege grab` to grab power-ups when they appear!")
                            .WithThumbnailUrl(Global.SiegeInfo[fileID].Boss.ImageUrl)
                            .AddField($"Marbles: **{Global.SiegeInfo[fileID].Marbles.Count}**", marbles.ToString())
                            .AddField($"Boss: **{Global.SiegeInfo[fileID].Boss.Name}**", string.Format("HP: **{0}**\nAttacks: **{1}**\nDifficulty: **{2}**", Global.SiegeInfo[fileID].Boss.HP, Global.SiegeInfo[fileID].Boss.Attacks.Length, Enum.GetName(typeof(Difficulty), Global.SiegeInfo[fileID].Boss.Difficulty)));
                        await ReplyAsync(embed: builder.Build());
                        if (pings.Length != 0) await ReplyAsync(pings.ToString());
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
                                if (marble.StatusEffect == MSE.Stun && DateTime.UtcNow.Subtract(marble.LastStun).TotalSeconds > 15) marble.StatusEffect = MSE.None;
                                if (marble.StatusEffect != MSE.Stun) {
                                    if (DateTime.UtcNow.Subtract(Global.SiegeInfo[fileID].LastMorale).TotalSeconds > 20 && Global.SiegeInfo[fileID].Morales > 0) {
                                        Global.SiegeInfo[fileID].Morales--;
                                        await ReplyAsync($"The effects of a Morale Boost power-up have worn off! The damage multiplier is now **{Global.SiegeInfo[fileID].DamageMultiplier}**!");
                                    }
                                    var dmg = Global.Rand.Next(1, 25);
                                    if (dmg > 20) dmg = Global.Rand.Next(21, 35);
                                    var title = "";
                                    var url = "";
                                    if (IsBetween(dmg, 1, 7)) {
                                        title = "Slow attack!";
                                        url = "https://cdn.discordapp.com/attachments/296376584238137355/548217423623356418/SiegeAttackSlow.png";
                                    } else if (IsBetween(dmg, 8, 14)) {
                                        title = "Fast attack!";
                                        url = "https://cdn.discordapp.com/attachments/296376584238137355/548217417847799808/SiegeAttackFast.png";
                                    } else if (IsBetween(dmg, 15, 20)) {
                                        title = "Brutal attack!";
                                        url = "https://cdn.discordapp.com/attachments/296376584238137355/548217407337005067/SiegeAttackBrutal.png";
                                    } else if (IsBetween(dmg, 21, 35)) {
                                        title = "CRITICAL attack!";
                                        url = "https://cdn.discordapp.com/attachments/296376584238137355/548217425359798274/SiegeAttackCritical.png";
                                    } else title = "Glitch attack!";
                                    dmg = marble.StatusEffect == MSE.Chill ? (int)Math.Round(dmg * Global.SiegeInfo[fileID].DamageMultiplier * 0.5) : (int)Math.Round(dmg * Global.SiegeInfo[fileID].DamageMultiplier);
                                    var clone = false;
                                    if (marble.Cloned) {
                                        clone = true;
                                        Global.SiegeInfo[fileID].Boss.HP -= dmg * 5;
                                        builder.AddField("Clones attack!", $"Each of the clones dealt **{dmg}** damage to the boss!");
                                        Global.SiegeInfo[fileID].Marbles.Find(m => m.Id == Context.User.Id).Cloned = false;
                                    }
                                    Global.SiegeInfo[fileID].Boss.HP -= dmg;
                                    Global.SiegeInfo[fileID].Marbles.Find(m => m.Id == Context.User.Id).DamageDealt += dmg;
                                    builder.WithTitle(title)
                                        .WithThumbnailUrl(url)
                                        .WithDescription(string.Format("**{0}** dealt **{1}** damage to **{2}**!", Global.SiegeInfo[fileID].Marbles.Find(m => m.Id == Context.User.Id).Name, dmg, Global.SiegeInfo[fileID].Boss.Name))
                                        .AddField("Boss HP", $"**{Global.SiegeInfo[fileID].Boss.HP}**/{Global.SiegeInfo[fileID].Boss.MaxHP}");
                                    await ReplyAsync(embed: builder.Build());
                                    if (clone && marble.Name[marble.Name.Length - 1] != 's') await ReplyAsync($"{marble.Name}'s clones disappeared!");
                                    else if (clone) await ReplyAsync($"{marble.Name}' clones disappeared!");
                                    if (Global.SiegeInfo[fileID].Boss.HP < 1) {
                                        await SiegeVictoryAsync(fileID);
                                    }
                                } else await ReplyAsync($"**{Context.User.Username}**, you are stunned and cannot attack!");
                            } else await ReplyAsync($"**{Context.User.Username}**, you are out and can no longer attack!");
                        } else await ReplyAsync($"**{Context.User.Username}**, you aren't in this Siege!");
                    } else await ReplyAsync("There is no currently ongoing Siege!");
                    break;
                }
                case "bonk": goto case "attack";
                case "grab": {
                    if (Global.SiegeInfo[fileID].Marbles.Any(m => m.Id == Context.User.Id)) {
                        if (Global.SiegeInfo[fileID].PowerUp.IsEmpty()) await ReplyAsync("There is no power-up to grab!");
                        else if (Global.SiegeInfo[fileID].Marbles.Find(m => m.Id == Context.User.Id).HP < 1) await ReplyAsync($"**{Context.User.Username}**, you are out and can no longer grab power-ups!");
                        else {
                            if (Global.Rand.Next(0, 3) == 0) {
                                Global.SiegeInfo[fileID].Marbles.Find(m => m.Id == Context.User.Id).PUHits++;
                                switch (Global.SiegeInfo[fileID].PowerUp) {
                                    case "Morale Boost":
                                        Global.SiegeInfo[fileID].Morales++;
                                        builder.WithTitle("POWER-UP ACTIVATED!")
                                            .WithDescription(string.Format("**{0}** activated **Morale Boost**! Damage multiplier increased to **{1}**!", Global.SiegeInfo[fileID].Marbles.Find(m => m.Id == Context.User.Id).Name, Global.SiegeInfo[fileID].DamageMultiplier));
                                        await ReplyAsync(embed: builder.Build());
                                        Global.SiegeInfo[fileID].SetPowerUp("");
                                        Global.SiegeInfo[fileID].LastMorale = DateTime.UtcNow;
                                        break;
                                    case "Clone":
                                        Global.SiegeInfo[fileID].Marbles.Find(m => m.Id == Context.User.Id).Cloned = true;
                                        builder.WithTitle("POWER-UP ACTIVATED!")
                                            .WithDescription(string.Format("**{0}** activated **Clone**! Five clones of {0} appeared!", Global.SiegeInfo[fileID].Marbles.Find(m => m.Id == Context.User.Id).Name));
                                        await ReplyAsync(embed: builder.Build());
                                        Global.SiegeInfo[fileID].SetPowerUp("");
                                        break;
                                    case "Summon":
                                        var choice = Global.Rand.Next(0, 2);
                                        string ally;
                                        string url;
                                        switch (choice) {
                                            case 0: ally = "Frigidium"; url = "https://cdn.discordapp.com/attachments/296376584238137355/543745898690379816/Frigidium.png"; break;
                                            case 1: ally = "Neptune"; url = "https://cdn.discordapp.com/attachments/296376584238137355/543745899591893012/Neptune.png"; break;
                                            default: ally = "MarbleBot"; url = ""; break;
                                        }
                                        var dmg = Global.Rand.Next(60, 85);
                                        Global.SiegeInfo[fileID].Boss.HP -= (int)Math.Round(dmg * Global.SiegeInfo[fileID].DamageMultiplier);
                                        if (Global.SiegeInfo[fileID].Boss.HP < 0) Global.SiegeInfo[fileID].Boss.HP = 0;
                                        builder.WithTitle("POWER-UP ACTIVATED!")
                                            .WithThumbnailUrl(url)
                                            .AddField("Boss HP", $"**{Global.SiegeInfo[fileID].Boss.HP}**/{Global.SiegeInfo[fileID].Boss.MaxHP}")
                                            .WithDescription($"**{Global.SiegeInfo[fileID].Marbles.Find(m => m.Id == Context.User.Id).Name}** activated **Summon**! **{ally}** came into the arena and dealt **{dmg}** damage to the boss!");
                                        await ReplyAsync(embed: builder.Build());
                                        Global.SiegeInfo[fileID].SetPowerUp("");
                                        break;
                                    case "Cure":
                                        var marble = Global.SiegeInfo[fileID].Marbles.Find(m => m.Id == Context.User.Id);
                                        var mse = Enum.GetName(typeof(MSE), marble.StatusEffect);
                                        Global.SiegeInfo[fileID].Marbles.Find(m => m.Id == Context.User.Id).StatusEffect = MSE.None;
                                        builder.WithTitle("Cured!")
                                            .WithDescription($"**{marble.Name}** has been cured of **{mse}**!");
                                        await ReplyAsync(embed: builder.Build());
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
                        using (var marbleList = new StreamWriter(fileID.ToString() + "siege.csv", false)) {
                            await marbleList.WriteAsync("");
                            await ReplyAsync("Contestant list successfully cleared!");
                            marbleList.Close();
                        }
                    }
                    break;
                }
                case "checkearn": {
                    var User = GetUser(Context);
                    var nextDaily = DateTime.UtcNow.Subtract(User.LastSiegeWin);
                    var output = "";
                    if (nextDaily.TotalHours < 6) output = string.Format("You can earn money from Sieges in **{0}**!", GetDateString(User.LastSiegeWin.Subtract(DateTime.UtcNow.AddHours(-6))));
                    else output = "You can earn money from Sieges now!";
                    builder.WithAuthor(Context.User)
                        .WithDescription(output);
                    await ReplyAsync(embed: builder.Build());
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
                                if (Context.IsPrivate) marbles.AppendLine($"**{mSplit[0].Trim('\n')}**");
                                else marbles.AppendLine($"**{mSplit[0].Trim('\n')}** [{user.Username}#{user.Discriminator}]");
                                cCount++;
                            }
                        }
                    }
                    if (marbles.ToString().IsEmpty()) await ReplyAsync("It looks like there aren't any contestants...");
                    else {
                        builder.AddField("Contestants", marbles.ToString());
                        builder.WithFooter("Contestant count: " + cCount)
                            .WithTitle("Marble Siege: Contestants");
                        await ReplyAsync(embed: builder.Build());
                    }
                    break;
                }
                case "marbles": goto case "contestants";
                case "participants": goto case "contestants";
                case "remove": {
                    byte state = Context.User.Id == 224267581370925056 ? (byte)3 : (byte)0; // 0 - Not found, 1 - Found but not yours, 2 - Found & yours, 3 - Found & overridden
                    var wholeFile = new StringBuilder();
                    var id = 0ul;
                    using (var marbleList = new StreamReader(fileID.ToString() + "siege.csv")) {
                        while (!marbleList.EndOfStream) {
                            var line = await marbleList.ReadLineAsync();
                            if (line.Split(',')[0] == option) {
                                if (ulong.Parse(line.Split(',')[1]) == Context.User.Id) {
                                    id = ulong.Parse(line.Split(',')[1]);
                                    state = 2;
                                } else {
                                    wholeFile.AppendLine(line);
                                    if (!(state == 2)) state = 1;
                                }
                            } else wholeFile.AppendLine(line);
                        }
                    }
                    switch (state) {
                        case 0: await ReplyAsync("Could not find the requested marble!"); break;
                        case 1: await ReplyAsync("This is not your marble!"); break;
                        case 2: using (var marbleList = new StreamWriter(fileID.ToString() + "siege.csv", false)) {
                                await marbleList.WriteAsync(wholeFile.ToString());
                                await ReplyAsync("Removed contestant **" + option + "**!");
                            }
                            break;
                        case 3: goto case 2;
                    }
                    break;
                }
                case "info": {
                    var marbles = new StringBuilder();
                    builder.WithTitle("Siege Info");
                    if (Global.SiegeInfo.ContainsKey(fileID)) {
                        var siege = Global.SiegeInfo[fileID];
                        foreach (var marble in siege.Marbles) {
                            marbles.AppendLine($"**{marble.Name}** (HP: **{marble.HP}**/{marble.MaxHP}, DMG: **{marble.DamageDealt}**) [{Context.Client.GetUser(marble.Id).Username}#{Context.Client.GetUser(marble.Id).Discriminator}]");
                        }
                        var PU = siege.PowerUp.IsEmpty() ? "None" : siege.PowerUp;
                        builder.AddField($"Boss: **{siege.Boss.Name}**", string.Format("\nHP: **{0}**/{3}\nAttacks: **{1}**\nDifficulty: **{2}**", siege.Boss.HP, siege.Boss.Attacks.Length, Enum.GetName(typeof(Difficulty), siege.Boss.Difficulty), siege.Boss.MaxHP))
                            .AddField($"Marbles: **{siege.Marbles.Count}**", marbles.ToString())
                            .WithDescription($"Damage Multiplier: **{siege.DamageMultiplier}**\nActive Power-up: **{PU}**")
                            .WithThumbnailUrl(siege.Boss.ImageUrl);
                    } else {
                        using (var marbleList = new StreamReader(fileID.ToString() + "siege.csv")) {
                            var allMarbles = (await marbleList.ReadToEndAsync()).Split('\n');
                            if (allMarbles.Length > 1) {
                                foreach (var marble in allMarbles) {
                                    if (marble.Length > 16) {
                                        var mSplit = marble.Split(',');
                                        var user = Context.Client.GetUser(ulong.Parse(mSplit[1].Trim('\n')));
                                        marbles.AppendLine($"**{mSplit[0].Trim('\n')}** [{user.Username}#{user.Discriminator}]");
                                    }
                                }
                            } else marbles.Append("No contestants have signed up!");
                        }
                        builder.AddField("Marbles", marbles.ToString());
                        builder.WithDescription("Siege not started yet.");
                    }
                    await ReplyAsync(embed: builder.Build());
                    break;
                }
                case "leaderboard": {
                    var winners = new SortedDictionary<string, int>();
                    using (var win = new StreamReader("Resources\\SiegeMostUsed.txt")) {
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
                    builder.WithTitle("Siege Leaderboard: Most Used")
                        .WithDescription(desc.ToString());
                    await ReplyAsync(embed: builder.Build());
                    break;
                }
                case "leaderboard mostused": goto case "leaderboard";
                case "mostused": goto case "leaderboard";
                case "boss": {
                    var boss = Boss.Empty;
                    var state = 1;
                    switch (option.ToLower().RemoveChar(' ')) {
                        case "preethetree": boss = Global.PreeTheTree; break;
                        case "pree": goto case "preethetree";
                        case "hattmann": boss = Global.HATTMANN; break;
                        case "orange": boss = Global.Orange; break;
                        case "green": boss = Global.Green; break;
                        case "destroyer": boss = Global.Destroyer; break;
                        case "helpmethetree": boss = Global.HelpMeTheTree; break;
                        case "helpme": goto case "helpmethetree";
                        case "erango": boss = Global.Erango; break;
                        case "octopheesh": boss = Global.Octopheesh; break;
                        case "frigidium": await ReplyAsync("No."); state = 3; break;
                        case "highwaystickman": goto case "frigidium";
                        case "outcast": goto case "frigidium";
                        case "doon": goto case "frigidium";
                        case "shardberg": goto case "frigidium";
                        case "iceelemental": goto case "frigidium";
                        case "snowjoke": goto case "frigidium";
                        case "pheesh": goto case "frigidium";
                        case "shark": goto case "frigidium";
                        case "pufferfish": goto case "frigidium";
                        case "neptune": goto case "frigidium";
                        case "lavachunk": goto case "frigidium";
                        case "pyromaniac": goto case "frigidium";
                        case "volcano": goto case "frigidium";
                        case "red": goto case "frigidium";
                        case "spaceman": goto case "frigidium";
                        case "rgvzdhjvewvy": goto case "frigidium";
                        case "corruptsoldier": goto case "frigidium";
                        case "corruptpurple": goto case "frigidium";
                        case "chest": goto case "frigidium";
                        case "scaryface": goto case "frigidium";
                        case "marblebot": goto case "frigidium";
                        case "overlord": await ReplyAsync("*Ahahahaha...\n\nYou are sorely mistaken.*"); state = 3; break;
                        case "vinemonster": await ReplyAsync("Excuse me?"); state = 3; break;
                        case "vinemonsters": goto case "vinemonster";
                        case "floatingore": goto case "vinemonster";
                        case "veronica": await ReplyAsync("Woah there! Calm down!"); state = 3; break;
                        case "rockgolem": goto case "veronica";
                        case "minideletion": goto case "veronica";
                        case "alphadeletion": goto case "veronica";
                        case "viii": goto case "veronica";
                        case "triacontapheesh": goto case "veronica";
                        case "hyperguard": goto case "veronica";
                        case "creator": await ReplyAsync("*No...\n\nThis is wrong...*"); state = 3; break;
                        case "doc671": await ReplyAsync("No spoilers here!"); state = 4; break;
                        default: state = 0; break;
                    }
                    if (state == 1) {
                        var attacks = new StringBuilder();
                        foreach (var attack in boss.Attacks) attacks.AppendLine($"**{attack.Name}** (Accuracy: {attack.Accuracy}%) [Damage: {attack.Damage}] <MSE: {Enum.GetName(typeof(MSE), attack.StatusEffect)}>");
                        builder.AddField("HP", $"**{boss.MaxHP}**")
                            .AddField("Attacks", attacks.ToString())
                            .AddField("Difficulty", $"**{Enum.GetName(typeof(Difficulty), boss.Difficulty)}** {(int)boss.Difficulty}/10")
                            .WithThumbnailUrl(boss.ImageUrl)
                            .WithTitle(boss.Name);
                        await ReplyAsync(embed: builder.Build());
                    } else if (state == 0) await ReplyAsync("Could not find the requested boss!");
                    break;
                }
                case "bossinfo": goto case "boss";
                case "bosslist":
                    Boss[] playableBosses = { Global.PreeTheTree, Global.HelpMeTheTree, Global.HATTMANN, Global.Orange, Global.Erango, Global.Octopheesh, Global.Green, Global.Destroyer };
                    foreach (var boss in playableBosses) {
                        builder.AddField($"{boss.Name}", $"Difficulty: **{Enum.GetName(typeof(Difficulty), boss.Difficulty)}**, HP: **{boss.MaxHP}**, Attacks: **{boss.Attacks.Count()}**");
                    }
                    builder.WithDescription("Use `mb/siege boss <boss name>` for more info!")
                        .WithTitle("Playable MS Bosses");
                    await ReplyAsync(embed: builder.Build());
                    break;
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
                        case "summon": powerup = "Summon";
                            desc = "Summons an ally to help against the boss.";
                            url = "https://cdn.discordapp.com/attachments/296376584238137355/541373120129531939/PUSummon.png";
                            break;
                        case "cure": powerup = "Cure";
                            desc = "Cures a marble of a status effect.";
                            url = "https://cdn.discordapp.com/attachments/296376584238137355/541373094724501524/PUCure.png";
                            break;
                    }
                    if (powerup.IsEmpty()) await ReplyAsync("Could not find the requested power-up!");
                    else {
                        builder.WithDescription(desc)
                            .WithThumbnailUrl(url)
                            .WithTitle(powerup);
                        await ReplyAsync(embed: builder.Build());
                    }
                    break;
                }
                case "power-up": goto case "powerup";
                case "power-upinfo": goto case "powerup";
                case "powerupinfo": goto case "powerup";
                case "puinfo": goto case "powerup";
                case "ping": {
                    var obj = GetUsersObj();
                    var user = GetUser(Context, obj);
                    switch (option) {
                        case "on": user.SiegePing = true; break;
                        case "off": user.SiegePing = false; break;
                        default: user.SiegePing = !user.SiegePing; break;
                    }
                    obj.Remove(Context.User.Id.ToString());
                    obj.Add(new JProperty(Context.User.Id.ToString(), JObject.FromObject(user)));
                    WriteUsers(obj);
                    if (user.SiegePing) await ReplyAsync($"**{Context.User.Username}**, you will now be pinged when a Siege that you are in starts.\n(type `mb/siege ping` to turn off)");
                    else await ReplyAsync($"**{Context.User.Username}**, you will no longer be pinged when a Siege that you are in starts.\n(type `mb/siege ping` to turn on)");
                    break;
                }
                default:
                    var sb = new StringBuilder();
                    sb.AppendLine("Use `mb/siege signup <marble name>` to sign up as a marble! (you can only sign up once)");
                    sb.AppendLine("When everyone's done, use `mb/siege start`! The Siege begins automatically when 20 people have signed up.\n");
                    sb.AppendLine("When the Siege begins, use `mb/siege attack` to attack the boss!");
                    sb.AppendLine("Power-ups occasionally appear. Use `mb/siege grab` to try and activate the power-up (1/3 chance).\n");
                    sb.AppendLine("Check who's participating with `mb/siege contestants` and view Siege information with `mb/siege info`!");
                    builder.AddField("How to play", sb.ToString())
                        .WithTitle("Marble Siege!");
                    await ReplyAsync(embed: builder.Build());
                    break;
            }
        }

        // Separate task dealing with time-based boss responses
        public async Task SiegeBossActionsAsync(ulong Id) {
            var startTime = DateTime.UtcNow;
            var timeout = false;
            do {
                await Task.Delay(15000);
                if (Global.SiegeInfo[Id].Boss.HP < 1) {
                    await SiegeVictoryAsync(Id);
                    break;
                } else if (DateTime.UtcNow.Subtract(startTime).TotalMinutes >= 10) {
                    timeout = true;
                    break;
                } else {
                    // Attack marbles
                    var rand = Global.Rand.Next(0, Global.SiegeInfo[Id].Boss.Attacks.Length);
                    var atk = Global.SiegeInfo[Id].Boss.Attacks[rand];
                    var builder = new EmbedBuilder()
                        .WithColor(GetColor(Context))
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
                                    builder.AddField($"**{marble.Name}** has been killed!", $"HP: 0/{marble.MaxHP}\nDamage Multiplier: {Global.SiegeInfo[Id].DamageMultiplier}");
                                } else {
                                    switch (atk.StatusEffect) {
                                        case MSE.Chill:
                                            marble.StatusEffect = MSE.Chill;
                                            builder.AddField($"**{marble.Name}** has been chilled! All attacks will deal half damage unless cured!", $"HP: {marble.HP}/{marble.MaxHP}\nStatus Effect: **Chill**");
                                            break;
                                        case MSE.Doom:
                                            marble.StatusEffect = MSE.Doom;
                                            builder.AddField($"**{marble.Name}** has been doomed and will die in ~45 seconds if not cured!", $"HP: {marble.HP}/{marble.MaxHP}\nStatus Effect: **Doom**");
                                            marble.DoomStart = DateTime.UtcNow;
                                            break;
                                        case MSE.Poison:
                                            marble.StatusEffect = MSE.Poison;
                                            builder.AddField($"**{marble.Name}** has been poisoned and will lose HP every ~15 seconds until cured/at 1 HP!", $"HP: {marble.HP}/{marble.MaxHP}\nStatus Effect: **Poison**");
                                            break;
                                        case MSE.Stun:
                                            marble.StatusEffect = MSE.Stun;
                                            builder.AddField($"**{marble.Name}** has been stunned and cannot attack for the next ~15 seconds!", $"HP: {marble.HP}/{marble.MaxHP}\nStatus Effect: **Stun**");
                                            marble.LastStun = DateTime.UtcNow;
                                            break;
                                        default: builder.AddField($"**{marble.Name}** has been damaged!", $"HP: {marble.HP}/{marble.MaxHP}"); break;
                                    }
                                }
                            }

                            // Perform status effects
                            switch (marble.StatusEffect) {
                                case MSE.Doom:
                                    if (DateTime.UtcNow.Subtract(marble.DoomStart).TotalSeconds > 45) {
                                        marble.HP = 0;
                                        builder.AddField($"**{marble.Name}** has died of Doom!", $"HP: **0**/{marble.MaxHP}\nDamage Multiplier: **{Global.SiegeInfo[Id].DamageMultiplier}**");
                                    }
                                    break;
                                case MSE.Poison:
                                    if (DateTime.UtcNow.Subtract(marble.LastPoisonTick).TotalSeconds > 15) {
                                        if (marble.HP < 1) break;
                                        marble.HP -= (int)Math.Round((double)marble.MaxHP / 10);
                                        marble.LastPoisonTick = DateTime.UtcNow;
                                        if (marble.HP < 2) {
                                            marble.HP = 1;
                                            marble.StatusEffect = MSE.None;
                                        }
                                        builder.AddField($"**{marble.Name}** has taken Poison damage!", $"HP: **{marble.HP}**/{marble.MaxHP}");
                                    }
                                    marble.LastPoisonTick = DateTime.UtcNow;
                                    break;
                            }
                        }
                    }
                    if (hits < 1) builder.AddField("Missed!", "No-one got hurt!");
                    await Context.Channel.SendMessageAsync("", false, builder.Build());

                    // Wear off Morale Boost
                    if (DateTime.UtcNow.Subtract(Global.SiegeInfo[Id].LastMorale).TotalSeconds > 20 && Global.SiegeInfo[Id].Morales > 0) {
                        Global.SiegeInfo[Id].Morales--;
                        await ReplyAsync($"The effects of a Morale Boost power-up have worn off! The damage multiplier is now **{Global.SiegeInfo[Id].DamageMultiplier}**!");
                    }

                    // Siege failure
                    if (Global.SiegeInfo[Id].Marbles.Aggregate(0, (agg, m) => { agg += m.HP; return agg; }) < 1) {
                        var marbles = new StringBuilder();
                        foreach (var marble in Global.SiegeInfo[Id].Marbles) {
                            marbles.AppendLine($"**{marble.Name}** (DMG: **{marble.DamageDealt}**, PU Hits: **{marble.PUHits}**) [{Context.Client.GetUser(marble.Id).Username}#{Context.Client.GetUser(marble.Id).Discriminator}]");
                        }
                        builder = new EmbedBuilder()
                            .WithColor(GetColor(Context))
                            .WithCurrentTimestamp()
                            .WithDescription($"All the marbles died!\n**{Global.SiegeInfo[Id].Boss.Name}** won!\nFinal HP: **{Global.SiegeInfo[Id].Boss.HP}**/{Global.SiegeInfo[Id].Boss.MaxHP}")
                            .AddField($"Fallen Marbles: **{Global.SiegeInfo[Id].Marbles.Count}**", marbles.ToString())
                            .WithThumbnailUrl(Global.SiegeInfo[Id].Boss.ImageUrl)
                            .WithTitle("Siege Failure!");
                        await Context.Channel.SendMessageAsync("", false, builder.Build());
                        break;
                    }
                    
                    // Cause new power-up to appear
                    if (Global.SiegeInfo[Id].PowerUp == "") {
                        rand = Global.Rand.Next(0, 8);
                        switch (rand) {
                            case 0: {
                                Global.SiegeInfo[Id].SetPowerUp("Morale Boost");
                                builder = new EmbedBuilder()
                                    .WithColor(GetColor(Context))
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
                                    .WithColor(GetColor(Context))
                                    .WithCurrentTimestamp()
                                    .WithDescription("A **Clone** power-up has spawned in the arena! Use `mb/siege grab` to try and activate it!")
                                    .WithThumbnailUrl(Global.SiegeInfo[Id].PUImageUrl)
                                    .WithTitle("Power-up spawned!");
                                await Context.Channel.SendMessageAsync("", false, builder.Build());
                                break;
                            }
                            case 2: {
                                Global.SiegeInfo[Id].SetPowerUp("Summon");
                                builder = new EmbedBuilder()
                                    .WithColor(GetColor(Context))
                                    .WithCurrentTimestamp()
                                    .WithDescription("A **Summon** power-up has spawned in the arena! Use `mb/siege grab` to try and activate it!")
                                    .WithThumbnailUrl(Global.SiegeInfo[Id].PUImageUrl)
                                    .WithTitle("Power-up spawned!");
                                await Context.Channel.SendMessageAsync("", false, builder.Build());
                                break;
                            }
                            case 3: {
                                if (Global.SiegeInfo[Id].Marbles.Any(m => m.StatusEffect != MSE.None)) {
                                    Global.SiegeInfo[Id].SetPowerUp("Cure");
                                    builder = new EmbedBuilder()
                                        .WithColor(GetColor(Context))
                                        .WithCurrentTimestamp()
                                        .WithDescription("A **Cure** power-up has spawned in the arena! Use `mb/siege grab` to try and activate it!")
                                        .WithThumbnailUrl(Global.SiegeInfo[Id].PUImageUrl)
                                        .WithTitle("Power-up spawned!");
                                    await Context.Channel.SendMessageAsync("", false, builder.Build());
                                }
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
            using (var marbleList = new StreamWriter(Id + "siege.csv", false)) {
                await marbleList.WriteAsync("");
                marbleList.Close();
            }
            if (timeout) await Context.Channel.SendMessageAsync("10 minute timeout reached! Siege aborted!");
        }

        public async Task SiegeVictoryAsync(ulong Id) {
            var siege = Global.SiegeInfo[Id];
            var builder = new EmbedBuilder()
                .WithColor(GetColor(Context))
                .WithCurrentTimestamp()
                .WithTitle("Siege Victory!")
                .WithDescription($"**{siege.Boss.Name}** has been defeated!");
            for (int i = 0; i < siege.Marbles.Count; i++) {
                var marble = siege.Marbles[i];
                var obj = GetUsersObj();
                var user = GetUser(Context, obj, marble.Id);
                int earnings = marble.DamageDealt + (marble.PUHits * 50);
                if (DateTime.UtcNow.Subtract(user.LastSiegeWin).TotalHours > 6) {
                    var output = new StringBuilder();
                    var didNothing = true;
                    if (marble.DamageDealt > 0) {
                        output.AppendLine($"Damage dealt: {Global.UoM}**{marble.DamageDealt:n}**");
                        didNothing = false;
                    }
                    if (marble.PUHits > 0) {
                        output.AppendLine($"Power-ups grabbed (x50): {Global.UoM}**{marble.PUHits * 50:n}**");
                        didNothing = false;
                    }
                    if (marble.HP > 0) {
                        earnings += 200;
                        output.AppendLine($"Alive bonus: {Global.UoM}**{200:n}**");
                        user.SiegeWins++;
                    }
                    if (output.Length > 0 && !didNothing) {
                        if (marble.HP > 0) user.LastSiegeWin = DateTime.UtcNow;
                        output.AppendLine($"__**Total: {Global.UoM}{earnings:n}**__");
                        user.Balance += earnings;
                        user.NetWorth += earnings;
                        builder.AddField($"**{Context.Client.GetUser(marble.Id).Username}**'s earnings", output.ToString());
                    }
                }
                obj.Remove(marble.Id.ToString());
                obj.Add(new JProperty(marble.Id.ToString(), JObject.FromObject(user)));
                WriteUsers(obj);
            }
            await ReplyAsync(embed: builder.Build());
            Global.SiegeInfo[Id].Boss.ResetHP();
            Global.SiegeInfo.Remove(Id);
            using (var marbleList = new StreamWriter(Id.ToString() + "siege.csv", false)) {
                await marbleList.WriteAsync("");
                marbleList.Close();
            }
        }

        [Command("boss")]
        [Summary("Alias for `mb/siege boss`.")]
        public async Task BossCommandAsync([Remainder] string searchTerm) => await SiegeCommandAsync("boss", searchTerm);

        [Command("bosslist")]
        [Summary("Alias for `mb/siege bosslist`.")]
        public async Task BossListCommandAsync() => await SiegeCommandAsync("bosslist");

        [Command("powerup")]
        [Summary("Alias for `mb/siege powerup`.")]
        public async Task PowerUpCommandAsync([Remainder] string searchTerm) => await SiegeCommandAsync("powerup", searchTerm);
    }
}