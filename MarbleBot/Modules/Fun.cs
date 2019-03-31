﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MarbleBot.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MarbleBot.Modules
{
    /// <summary> Fun non-game commands. </summary>
    public class Fun : ModuleBase<SocketCommandContext>
    {
        [Command("7ball")]
        [Summary("Predicts an outcome of a user-defined event.")]
        public async Task SevenBallCommandAsync([Remainder] string input)
        {
            await Context.Channel.TriggerTypingAsync();
            int choice = Global.Rand.Next(0, 13);
            string outcome = "";
            switch (choice) {
                case 0: outcome = "no."; break;
                case 1: outcome = "looking negative."; break;
                case 2: outcome = "probably not."; break;
                case 3: outcome = "it is very doubtful."; break;
                case 4: outcome = "my visions are cloudy, try again another time."; break;
                case 5: outcome = "do you *really* want to know?"; break;
                case 6: outcome = "I forgot."; break;
                case 7: outcome = "possibly."; break;
                case 8: outcome = "it is highly likely."; break;
                case 9: outcome = "I believe so."; break;
                case 10: outcome = "it is certain."; break;
                case 11: outcome = "and the sign points to... yes!"; break;
                case 12: outcome = "and the sign points to... no!"; break;
                case 13: outcome = "probably not, but there is still a chance..."; break;
            }
            await ReplyAsync(":seven: |  **" + Context.User.Username + "**, " + outcome);
        }

        [Command("autoresponse")]
        [Summary("Things to do with autoresponses.")]
        [RequireOwner]
        public async Task AutoresponseCommandAsync(string option) {
            switch (option) {
                case "time": await ReplyAsync(string.Format("Last Use: {0}\nCurrent Time: {1}", Global.ARLastUse.ToString(), DateTime.UtcNow.ToString())); break;
                case "update": {
                    Global.Autoresponses = new Dictionary<string, string>();
                    using (var ar = new StreamReader("Autoresponses.txt")) {
                        while (!ar.EndOfStream) {
                            var arar = ar.ReadLine().Split(';');
                            Global.Autoresponses.Add(arar[0], arar[1]);
                        }
                    }
                    await ReplyAsync("Dictionary update complete!");
                    break;
                }
                default: break;
            }
        }

        [Command("best")]
        [Summary("Picks a random person to call the best.")]
        public async Task BestCommandAsync()
        {
            await Context.Channel.TriggerTypingAsync();
            if (!(Context.IsPrivate)) {
                if (Context.Guild.MemberCount > 1) {
                    string[] names = new string[Context.Guild.MemberCount];
                    SocketGuildUser[] users = Context.Guild.Users.ToArray();
                    for (int i = 0; i < Context.Guild.MemberCount - 1; i++) {
                        names[i] = users[i].Username;
                    }
                    await ReplyAsync("**" + names[Global.Rand.Next(0, Context.Guild.MemberCount - 1)] + "** is the best!");
                } else Trace.WriteLine("oof");
            } else await ReplyAsync("That command doesn't work here!");
        }

        [Command("bet")]
        [Summary("Bets on a marble.")]
        public async Task BetCommandAsync(string no)
        {
            await Context.Channel.TriggerTypingAsync();
            int noOfMarbles = int.Parse(no);
            if (noOfMarbles > 100) await ReplyAsync("The number you gave is too large. It needs to be 100 or below.");
            else if (noOfMarbles < 1) await ReplyAsync("The number you gave is too small.");
            string[,] marbles = new string[10, 10];
            using (StreamReader stream = new StreamReader("Marbles.csv")) {
                int a = 0;
                while (!stream.EndOfStream) {
                    string[] row = stream.ReadLine().Split(',');
                    for (int b = 0; b < row.Length - 1; b++) {
                        marbles[a, b] = row[b];
                    }
                    a++;
                }
            }
            int choice = Global.Rand.Next(0, noOfMarbles);
            int d = choice / 10;
            int c = choice - (d * 10);
            await ReplyAsync($"**{Context.User.Username}**, I bet that **{marbles[d - 1, c - 1]}** will win!");
        }

        [Command("buyhat")]
        [Summary("Fakes buying an Uglee Hat.")]
        [Remarks("Not CM")]
        public async Task BuyHatCommandAsync()
        {
            if (Context.Guild.Id != Global.CM) {
                await Context.Channel.TriggerTypingAsync();
                var price = Global.Rand.Next(0, int.MaxValue);
                var hatNo = Global.Rand.Next(0, 69042);
                await ReplyAsync($"That'll be **{price}** units of money please. Thank you for buying Uglee Hat #**{hatNo}**!");
            }
        }

        [Command("choose")]
        [Summary("Chooses between several choices.")]
        public async Task ChooseCommandAsync([Remainder] string input)
        {
            await Context.Channel.TriggerTypingAsync();
            string[] choices = input.Split('|');
            int choice = Global.Rand.Next(0, choices.Length);
            if (Moderation.CheckSwear(input) || Moderation.CheckSwear(choices[choice])) {
                if (Context.IsPrivate) {
                    IGuildUser Doc671 = Context.Guild.GetUser(224267581370925056);
                    await ReplyAsync("Profanity detected. " + Doc671.Mention);
                }
                else Trace.WriteLine($"[{DateTime.UtcNow}] Profanity detected: {input}");
            } else {
                await ReplyAsync("**" + Context.User.Username + "**, I choose **" + choices[choice].Trim() + "**!");
            }
        }

        [Command("orange")]
        [Summary("Gives the user a random statement in Orange Language.")]
        [Remarks("Not CM")]
        public async Task OrangeCommandAsync()
        {
            await Context.Channel.TriggerTypingAsync();
            int choice = Global.Rand.Next(1, 6);
            string egnaro = "";
            switch (choice){
                case 1: egnaro = "!olleH"; break;
                case 2: egnaro = "!raotS taH ehT owt oG"; break;
                case 3: egnaro = "!pooS puoP knirD"; break;
                case 4: egnaro = ".depfeQ ,ytiC ogitreV ni evil I"; break;
                case 5: egnaro = "!haoW"; break;
                case 6: egnaro = "!ainomleM dna dnalkseD ,ytiC ogitreV :depfeQ ni seitic eerht era erehT"; break;
            }
            if (Context.IsPrivate || Context.Guild.Id == Global.THS || Context.Guild.Id == Global.MT || Context.Guild.Id == Global.VFC) await ReplyAsync(egnaro);
        }

        [Command("orangeify")]
        [Summary("Returns the user input in Orange Language.")]
        [Remarks("Not CM")]
        public async Task OrangeifyCommandAsync([Remainder] string input)
        {
            await Context.Channel.TriggerTypingAsync();
            string orangeified = "";
            int length = input.Length - 1;
            while (length >= 0) {
                orangeified += input[length];
                length--;
            }
            if (Context.IsPrivate || Context.Guild.Id == Global.THS || Context.Guild.Id == Global.MT || Context.Guild.Id == Global.VFC) {
                if (Moderation.CheckSwear(input) || Moderation.CheckSwear(orangeified)) {
                    if (Context.IsPrivate) {
                        IGuildUser Doc671 = Context.Guild.GetUser(224267581370925056);
                        await ReplyAsync("Profanity detected. " + Doc671.Mention);
                    } else Trace.WriteLine($"[{DateTime.UtcNow}] Profanity detected: {input}");
                } else {
                    await ReplyAsync(orangeified);
                }
            }
        }

        [Command("random")]
        [Summary("Returns a random number with user-defined bounds.")]
        public async Task RandomCommandAsync(string rStart, string rEnd)
        {
            await Context.Channel.TriggerTypingAsync();
            var start = rStart.ToInt();
            var end = rEnd.ToInt();
            if (start < 0 || end < 0) await ReplyAsync("Only use positive numbers!");
            else if (start > end) {
                try {
                    int randNumber = Global.Rand.Next(end, start);
                    await ReplyAsync(randNumber.ToString());
                } catch (FormatException) {
                    await ReplyAsync("Number too large/small.");
                    throw;
                }
            } else {
                try {
                    int randNumber = Global.Rand.Next(start, end);
                    await ReplyAsync(randNumber.ToString());
                } catch (FormatException) {
                    await ReplyAsync("Number too large/small.");
                    throw;
                }
            }
        }

        [Command("rank")]
        [Summary("Returns a randomised level and XP count.")]
        public async Task RankCommandAsync()
        {
            await Context.Channel.TriggerTypingAsync();
            EmbedBuilder builder = new EmbedBuilder();
            byte level = Convert.ToByte(Global.Rand.Next(0, 25));
            int xp = 0;

            switch (level) {
                case 0: xp = Global.Rand.Next(0, 99); break;
                case 1: xp = Global.Rand.Next(100, 254); break;
                case 2: xp = Global.Rand.Next(255, 474); break;
                case 3: xp = Global.Rand.Next(475, 769); break;
                case 4: xp = Global.Rand.Next(770, 1149); break;
                case 5: xp = Global.Rand.Next(1150, 1624); break;
                case 6: xp = Global.Rand.Next(1625, 2204); break;
                case 7: xp = Global.Rand.Next(2205, 2899); break;
                case 8: xp = Global.Rand.Next(2900, 3719); break;
                case 9: xp = Global.Rand.Next(3720, 4674); break;
                case 10: xp = Global.Rand.Next(4674, 5774); break;
                case 11: xp = Global.Rand.Next(5575, 7029); break;
                case 12: xp = Global.Rand.Next(7030, 8449); break;
                case 13: xp = Global.Rand.Next(8450, 10044); break;
                case 14: xp = Global.Rand.Next(10045, 11824); break;
                case 15: xp = Global.Rand.Next(11825, 13799); break;
                case 16: xp = Global.Rand.Next(13800, 15979); break;
                case 17: xp = Global.Rand.Next(15980, 18374); break;
                case 18: xp = Global.Rand.Next(18735, 20994); break;
                case 19: xp = Global.Rand.Next(20995, 23849); break;
                case 20: xp = Global.Rand.Next(23850, 26949); break;
                case 21: xp = Global.Rand.Next(26950, 30304); break;
                case 22: xp = Global.Rand.Next(30305, 33924); break;
                case 23: xp = Global.Rand.Next(33925, 37819); break;
                case 24: xp = Global.Rand.Next(37820, 41999); break;
                case 25: xp = Global.Rand.Next(42000, 46474); break;
            }

            var msgs = await Context.Channel.GetMessagesAsync(100).FlattenAsync();
            byte ranks = 0;
            string flavour;

            foreach (IMessage msg in msgs) {
                if (msg != null && msg.Content == "mb/rank" && msg.Author == Context.Message.Author) {
                    ranks++;
                } else if (msg == null) {
                    break;
                }
            }

            switch (ranks)
            {
                case 1: flavour = "Pretty cool, right?"; break;
                case 2: flavour = "100% legitimate"; break;
                case 3: flavour = "I have a feeling you doubt me. Why is that?"; break;
                case 4: flavour = "What? I'm telling the truth, I swear!"; break;
                case 5: flavour = "What do you mean: \"This is random!\"?"; break;
                case 6: flavour = "Stop! Now!"; break;
                case 7: flavour = "I mean, you're probably breaking a no-spam rule!"; break;
                case 8: flavour = "...or you're using this in a spam channel..."; break;
                case 9: flavour = "...or slowmode is on..."; break;
                case 10: flavour = "Please... don't expose me... ;-;"; break;
                case 11: flavour = "At least I tried to generate a level..."; break;
                case 12: flavour = ";;--;;"; break;
                case 13: flavour = "I want to cry now. I really do."; break;
                case 14: flavour = "...and I cry acid."; break;
                case 15: flavour = "Just kidding, I actually cry Poup Soop..."; break;
                case 16: flavour = "...which has acid in it..."; break;
                case 17: flavour = "agh"; break;
                case 18: flavour = "Why are you still going on?"; break;
                case 19: flavour = "Aren't you bored?"; break;
                case 20: flavour = "Don't you have anything better to do?"; break;
                case 21: flavour = "No? I suppose not. You've used this command 21 times in the past 100 messages, after all."; break;
                case 22: flavour = "Do you just want to see how far I'll go?"; break;
                case 23: flavour = "Fine. I'll stop then."; break;
                case 24: flavour = "Bye."; break;
                case 25: flavour = "Wasn't fun talking to you."; break;
                case 26: flavour = "ok really this is the last message"; break;
                default: flavour = ""; break;
            }

            builder.AddField("Level", level, true)
                .AddField("Total XP", xp, true)
                .WithColor(Global.GetColor(Context))
                .WithTimestamp(DateTime.UtcNow)
                .WithAuthor(Context.User)
                .WithFooter(flavour);

            await ReplyAsync(embed: builder.Build());
        }

        [Command("rate")]
        [Summary("Rates something out of 10.")]
        public async Task RateCommandAsync([Remainder] string input)
        {
            await Context.Channel.TriggerTypingAsync();
            string message = "";
            int rating;
            switch (input.ToLower())
            {
                // These inputs have custom ratings and messages:
                case "256 mg": rating = -2; message = "I Am In Confusial Why"; break;
                case "ddsc": rating = 0; break;
                case "desk": rating = 11; message = "what did you expect?"; break;
                case "desk176": rating = 11; message = "what did you expect?"; break;
                case "desks": goto case "desk";
                case "dGFibGU=": rating = -999; message = "do not mention that unholy creature near me"; break;
                case "dGFibGVz": rating = -999; message = "do not mention those unholy creatures near me"; break;
                case "doc671": rating = -1; message = "terrible at everything"; break;
                case "dockque the rockque": rating = -1; message = "AM NOT ROCKQUE YOU MELMON"; break;
                case "egnaro":  rating = 10; message = "!egnarO"; break;
                case "erango": rating = 0; message = "stoP noW"; break;
                case "marblebot's creator": input = "Doc671"; goto case "doc671";
                case "marblebot's dad": input = "Doc671"; goto case "doc671";
                case "my creator": input = "Doc671"; goto case "doc671";
                case "my dad": input = "Doc671"; goto case "doc671";
                case "orange": goto case "egnaro";
                case "orange day": rating = 10; message = "!yaD egnarO"; break;
                case "poos puop": rating = 10; message = "!pooS puoP knirD"; break;
                case "poup soop": goto case "poos puop";
                case "rockque": rating = -1; message = "I Hate Rocks"; break;
                case "rockques": goto case "rockque";
                case "table": rating = -999; message = "do not mention that unholy creature near me"; input = "dGFibGU="; break;
                case "tables": rating = -999; message = "do not mention those unholy creatures near me"; input = "dGFibGVz"; break;
                case "the hat stoar": rating = 10; message = "!raotS taH ehT owt oG"; break;
                case "vinh": rating = 10; message = "Henlo Cooooooooool Vinh"; break;
                case "yad egnaro": goto case "orange day";
                default: rating = Global.Rand.Next(0, 11); break;
            }
            switch (input.ToLower()) {
                // These have custom messages but no preset ratings:
                case "blueice57": message = "icccce"; break;
                case "confusial": message = "I Am In Confusial Why"; break;
                case "eknimpie": message = "EKNIMPIE YOUR A RAXIST"; break;
                case "flam": message = "Am Flam Flam Flam Flam Flam Flam"; break;
                case "flame": goto case "flam";
                case "flamevapour": goto case "flam";
                case "flurp": message = "FLURP I TO SIGN UP AND NOT BE"; break;
                case "george012": message = "henlo jorj"; break;
                case "icce": goto case "blueice57";
                case "+inf": message = "marbles will realize in +inf"; break;
                case "jgeoroegeos": goto case "george012";
                case "john": message = "Algodoo Marble for TROC!"; break;
                case "john dubuc": goto case "john";
                case "jorj": goto case "george012";
                case "ken": message = "#kenismelmon"; break;
                case "kenlimepie": goto case "ken";
                case "keylimepie": goto case "ken";
                case "luka": message = "LUKA\nYOU BLUMT"; break;
                case "marblebot": input = "myself"; goto case "myself";
                case "myself": message = "who am I?"; break;
                case "matheus": message = "marbles will realize in +inf"; break;
                case "matheus fazzion": goto case "matheus";
                case "meadow": message = "somebody toucha mei doe"; break;
                case "mei doe": goto case "meadow";
                case "melmon": message = "Whyn Arey Yoou A Melmon"; break;
                case "no u": message = "No Your"; break;
                case "no your": message = "No You're"; break;
                case "no you're": message = "N'Yuroe"; break;
                case "oh so muches": message = "Is To Much"; break;
                case "rackquette":  message = "WHACC"; break;
                case "sand dollar": message = "XXX"; break;
                case "shotgun": message = "Vinh Shotgun All"; break;
                case "you silly desk": message = "Now I\nCry"; break;
            }
            // Dann Annoy Me >:((((
            if (input.ToLower() == "dann" || input.ToLower() == "danny playz") {
                int choice = Global.Rand.Next(0, 2);
                if (choice == 1)  message = "you guys, are a rat kids";
                else message = "I don’t know who you are I don’t know what you want but if I don’t get my t-shirt tomorrow i will find you and I will rob you.";
                rating = Global.Rand.Next(9, 11);
            }

            string emoji;
            switch (rating)
            {
                // Emoji time!
                case -999: emoji = ":gun: :dagger: :bomb:"; break;
                case -1: emoji = ":gun:"; break;
                case 0: emoji = ":no_entry_sign:"; break;
                case 1: emoji = ":nauseated_face:"; break;
                case 2: emoji = ":rage:"; break;
                case 3: emoji = ":angry:"; break;
                case 4: emoji = ":slight_frown:"; break;
                case 5: emoji = ":neutral_face:"; break;
                case 6: emoji = ":slight_smile:"; break;
                case 7: emoji = ":grinning:"; break;
                case 8: emoji = ":thumbsup:"; break;
                case 9: emoji = ":white_check_mark:"; break;
                case 10: emoji = ":rofl:"; break;
                case 11: emoji = ":heart:"; break;
                default: emoji = ":thinking:"; break;
            }
            if (message == "") {
                switch (rating) {
                    // If there isn't already a custom message, pick one depending on rating:
                    case 0: message = "Excuse me, kind user, please cease your current course of action immediately."; break;
                    case 1: message = "Immediate desistance required."; break;
                    case 2: message = "I don't like it..."; break;
                    case 3: message = "angery"; break;
                    case 4: message = "ehhh..."; break;
                    case 5: message = "not bad... but not good either"; break;
                    case 6: message = "slightly above average... I guess..."; break;
                    case 7: message = "pretty cool, don't you think?"; break;
                    case 8: message = "yes"; break;
                    case 9: message = "approaching perfection"; break;
                    case 10: message = "PERFECT!!"; break;
                    default: message = "Uhhhhhhhh\nNot"; break;
                }
            }
            if (rating == -2)  await ReplyAsync("**" + Context.User.Username + "**, I rATE " + input + " UNd3FINED10. " + emoji + "\n(" + message + ")");
            else {
                if (Moderation.CheckSwear(input)) {
                    if (Context.IsPrivate) {
                        IGuildUser Doc671 = Context.Guild.GetUser(224267581370925056);
                        await ReplyAsync("Profanity detected. " + Doc671.Mention);
                    } else Trace.WriteLine($"[{DateTime.UtcNow}] Profanity detected: {input}");
                }
                else await ReplyAsync("**" + Context.User.Username + "**, I rate " + input + " **" + rating + "**/10. " + emoji + "\n(" + message + ")");
            }
        }

        [Command("repeat")]
        [Summary("Repeats the given message.")]
        public async Task RepeatCommandAsync([Remainder] string repeat)
        {
            await Context.Channel.TriggerTypingAsync();
            if (repeat == "Am Melmon" && (Context.Guild.Id == Global.THS || Context.Guild.Id == Global.MT)) {
                await ReplyAsync("No U");
            } else if (Moderation.CheckSwear(repeat)) {
                if (Context.IsPrivate) {
                    IGuildUser Doc671 = Context.Guild.GetUser(224267581370925056);
                    await ReplyAsync("Profanity detected. " + Doc671.Mention);
                }
                else Trace.WriteLine($"[{DateTime.UtcNow}] Profanity detected: {repeat}");
            } else {
                await ReplyAsync(repeat);
            }
        }

        [Command("reverse")]
        [Summary("Returns the user input reversed.")]
        public async Task ReverseCommandAsync([Remainder] string input)
        {
            await Context.Channel.TriggerTypingAsync();
            // Another version of orangeify, but for CM (can secretly be used elsewhere)
            string reverse = "";
            int length = input.Length - 1;
            while (length >= 0) {
                reverse += input[length];
                length--;
            }
            if (Moderation.CheckSwear(input) || Moderation.CheckSwear(reverse)) {
                if (Context.IsPrivate) {
                    IGuildUser Doc671 = Context.Guild.GetUser(224267581370925056);
                    await ReplyAsync("Profanity detected. " + Doc671.Mention);
                } else Trace.WriteLine($"[{DateTime.UtcNow}] Profanity detected: {input}");
            } else {
                await ReplyAsync(reverse);
            }
        }

        /*[Command("stats")]
        [Summary("Returns some stats.")]
        public async Task StatsCommandAsync()
        {
            string mensage = "";
            SocketGuildChannel[] channels = Context.Guild.TextChannels.ToArray();
            Array.Sort(channels, (x, y) => string.Compare(x.Name, y.Name));
            for (int i = 0; i < Context.Guild.Channels.Count - 1; i++) mensage += channels[i] + " ";
            await ReplyAsync(mensage);
        }*/

        [Command("vinhglish")]
        [Summary("Returns a Vinhglish word, its inventor and meaning.")]
        [Remarks("Not CM")]
        public async Task VinhglishCommandAsync([Remainder] string word = "")
        {
            int randNo = 0;
            bool wordSet = false;
            string[] wordList = new string[100];
            string[] invList = new string[100];
            string[] descList = new string[100];
            int a = 0;
            if (word == "") {
                using (StreamReader stream = new StreamReader("Vinhglish.csv")) {
                    while (!stream.EndOfStream) {
                        string list = stream.ReadLine();
                        string[] vocab = list.Split(',');
                        wordList[a] = vocab[0];
                        invList[a] = vocab[1];
                        descList[a] = vocab[2];
                        a++;
                    }
                }
                randNo = Global.Rand.Next(1, a);
            } else {
                using (StreamReader stream = new StreamReader("Vinhglish.csv")) {
                    while (!stream.EndOfStream) {
                        string list = stream.ReadLine();
                        string[] vocab = list.Split(',');
                        wordList[a] = vocab[0];
                        invList[a] = vocab[1];
                        descList[a] = vocab[2];
                        if (wordList[a].ToLower() == word.ToLower()) {
                            randNo = a;
                            stream.Close();
                            wordSet = true;
                            break;
                        }
                        //JGeoroegeos
                        a++;
                    }
                    if (!wordSet) randNo = Global.Rand.Next(1, a);
                }
            }
            if (Context.IsPrivate || Context.Guild.Id == Global.THS || Context.Guild.Id == Global.MT || Context.Guild.Id == Global.VFC)
                await ReplyAsync("**__" + wordList[randNo] + "__**\nInventor: " + invList[randNo] + "\nDescription: " + descList[randNo]);
        }
    }
}