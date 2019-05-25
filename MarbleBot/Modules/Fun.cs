using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MarbleBot.Extensions;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static MarbleBot.Global;

namespace MarbleBot.Modules
{
    /// <summary> Fun non-game commands. </summary>
    public class Fun : MarbleBotModule
    {
        [Command("7ball")]
        [Summary("Predicts the outcome to a user-defined event.")]
#pragma warning disable IDE0060 // Remove unused parameter
        public async Task SevenBallCommandAsync([Remainder] string input)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            await Context.Channel.TriggerTypingAsync();
            string outcome = Rand.Next(0, 13) switch
            {
                0 => "no.",
                1 => "looking negative.",
                2 => "probably not.",
                3 => "it is very doubtful.",
                4 => "my visions are cloudy, try again another time.",
                5 => "do you *really* want to know?",
                6 => "I forgot.",
                7 => "possibly.",
                8 => "it is highly likely.",
                9 => "I believe so.",
                10 => "it is certain.",
                11 => "and the sign points to... yes!",
                12 => "and the sign points to... no!",
                _ => "probably not, but there is still a chance..."
            };
            await ReplyAsync($":seven: |  **{Context.User.Username }**, {outcome}");
        }

        [Command("advice")]
        [Alias("progress", "sage")]
        [Summary("Gives advice on progression.")]
        public async Task AdviceCommandAsync()
        {
            await Context.Channel.TriggerTypingAsync();
            var user = GetUser(Context);
            string msg;
            if (user.Items.ContainsKey(78))
                msg = new StringBuilder().Append("Combine the terror-inducing essence of the bosses you have just ")
                    .AppendLine("defeated with another similar, yet different liquid. Be careful with the product.")
                    .Append("\n**TO ADVANCE:** Craft the **Essence of Corruption** (ID `079`).")
                    .ToString();
            else if (user.Items.ContainsKey(66) || user.Items.ContainsKey(71) || user.Items.ContainsKey(74) || user.Items.ContainsKey(80))
                msg = new StringBuilder().Append("Your equipment will prove very useful in the upcoming battles.")
                    .AppendLine("Seek the Chest of sentience and the Scary Face to test your newfound power.")
                    .Append("\n**TO ADVANCE:** Obtain the **Raw Essence of Horror** (ID `078`) from Chest or Scary Face.")
                    .ToString();
            else if (user.Items.ContainsKey(81))
                msg = new StringBuilder().Append("There is a way to increase the offensive capabilities of a marble.")
                    .AppendLine("Form a covering of spikes, made of iron, steel or even infernite.")
                    .Append("\n**TO ADVANCE:** Craft **Iron Spikes** (ID `066`), **Steel Spikes** (ID `071`) or **Infernite Spikes** (ID `074`).")
                    .ToString();
            else if (user.Items.ContainsKey(63))
                msg = new StringBuilder().Append("The world now contains a plethora of treasures for you to gather.")
                    .AppendLine("Craft the drill of chromium to allow you to extract the ore of the Violet Volcanoes.")
                    .Append("\n**TO ADVANCE:** Craft the **Chromium Drill** (ID `081`).")
                    .ToString();
            else if (user.Items.ContainsKey(62))
                msg = new StringBuilder().Append("Before you can successfully take on the new terrors roaming the land, ")
                    .AppendLine("you must first improve your equipment. Use Destroyer's plating to craft your own shield.")
                    .Append("\n**TO ADVANCE:** Craft the **Coating of Destruction** (ID `063`).")
                    .ToString();
            else if (user.Stage == 2)
                msg = new StringBuilder().Append("The cyborg's defeat has both given you new options and caught the attention of ")
                    .AppendLine("even more powerful foes. Head to its remains and gather the resources to upgrade your workstation.")
                    .Append("\n**TO ADVANCE:** Craft the **Crafting Station Mk.II** (ID `062`).")
                    .ToString();
            else if (user.Items.ContainsKey(53) && user.Items.ContainsKey(57))
                msg = new StringBuilder().Append("You have done very well, and have forged the best with the resources available ")
                    .AppendLine("to you. There is more to this world, however. Gather your allies and seek the cyborg Destroyer.")
                    .Append("\n**TO ADVANCE:** Defeat Destroyer. Item `091` may provide assistance.")
                    .ToString();
            else if (user.Items.ContainsKey(53))
                msg = new StringBuilder().Append("The Trebuchet Array is a potent weapon, albeit rather inaccurate. To assist ")
                    .AppendLine("in your battles, create the Rocket Boots, which shall help you evade their menacing attacks.")
                    .Append("\n**TO ADVANCE:** Craft the **Rocket Boots** (ID `057`).")
                    .ToString();
            else if (user.Items.ContainsKey(17))
                msg = new StringBuilder().Append("With your workstation, forge the Trebuchet Array from the different woods found ")
                    .AppendLine("in the forest. You will have to create three separate trebuchets first, then combine them.")
                    .Append("\n**TO ADVANCE:** Craft the **Trebuchet Array** (ID `053`).")
                    .ToString();
            else if (user.LastScavenge.DayOfYear != 1 || user.LastScavenge.Year != 2019)
                msg = new StringBuilder().Append("The items you have gathered are likely unable to be used in their current form. ")
                    .AppendLine("You must find a way to obtain a Crafting Station.")
                    .Append("\n**TO ADVANCE:** Obtain the **Crafting Station Mk.I** (ID `017`) via dailies.")
                    .ToString();
            else if (user.NetWorth > 1000)
                msg = new StringBuilder().Append("Well done. Your next goal is to gather for items at Canary Beach and Tree Wurld. ")
                    .AppendLine("Use `mb/scavenge help` if you are unsure of how to proceed.")
                    .Append("\n**TO ADVANCE:** Successfully complete a Scavenge.")
                    .ToString();
            else msg = new StringBuilder().Append($"Welcome! Your first task is to gain {UoM}1000! If you need help ")
                    .AppendLine("earning money, try using `mb/daily`, `mb/race` or `mb/siege`.")
                    .Append($"\n**TO ADVANCE:** Obtain {UoM}1000.")
                    .ToString();
            await ReplyAsync(embed: new EmbedBuilder()
                .WithColor(GetColor(Context))
                .WithCurrentTimestamp()
                .WithDescription(msg)
                .WithTitle($"Advice: {Context.User.Username}")
                .Build());
        }

        [Command("best")]
        [Summary("Picks a random person to call the best.")]
        [RequireContext(ContextType.Guild)]
        public async Task BestCommandAsync()
        {
            await Context.Channel.TriggerTypingAsync();
            if (Context.Guild.MemberCount > 1)
            {
                string[] names = new string[Context.Guild.MemberCount];
                SocketGuildUser[] users = Context.Guild.Users.ToArray();
                for (int i = 0; i < Context.Guild.MemberCount - 1; i++)
                    names[i] = users[i].Username;
                await ReplyAsync($"**{names[Rand.Next(0, Context.Guild.MemberCount - 1)]}** is the best!");
            }
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
            using (StreamReader stream = new StreamReader($"Resources{Path.DirectorySeparatorChar}Marbles.csv"))
            {
                int a = 0;
                while (!stream.EndOfStream)
                {
                    string[] row = stream.ReadLine().Split(',');
                    for (int b = 0; b < row.Length - 1; b++)
                        marbles[a, b] = row[b];
                    a++;
                }
            }
            int choice = Rand.Next(0, noOfMarbles);
            int d = choice / 10;
            int c = choice - (d * 10);
            await ReplyAsync($"**{Context.User.Username}**, I bet that **{marbles[d - 1, c - 1]}** will win!");
        }

        [Command("buyhat")]
        [Summary("Fakes buying an Uglee Hat.")]
        [Remarks("Not CM")]
        public async Task BuyHatCommandAsync()
        {
            await Context.Channel.TriggerTypingAsync();
            if (Context.Guild.Id != CM)
            {
                await Context.Channel.TriggerTypingAsync();
                var price = Rand.Next(0, int.MaxValue);
                var hatNo = Rand.Next(0, 69042);
                await ReplyAsync($"That'll be **{price}** units of money please. Thank you for buying Uglee Hat #**{hatNo}**!");
            }
        }

        [Command("choose")]
        [Summary("Chooses between several provided choices.")]
        public async Task ChooseCommandAsync([Remainder] string input)
        {
            await Context.Channel.TriggerTypingAsync();
            string[] choices = input.Split('|');
            int choice = Rand.Next(0, choices.Length);
            if ((await Moderation.CheckSwearAsync(input)) || (await Moderation.CheckSwearAsync(choices[choice])))
            {
                if (Context.IsPrivate)
                {
                    IGuildUser Doc671 = Context.Guild.GetUser(224267581370925056);
                    await ReplyAsync($"Profanity detected. {Doc671.Mention}");
                }
                else await Log($"Profanity detected: {input}");
            }
            else await ReplyAsync($"**{Context.User.Username} **, I choose **{choices[choice].Trim()}**!");
        }

        [Command("orange")]
        [Summary("Gives the user a random statement in Orange Language.")]
        [Remarks("Not CM")]
        public async Task OrangeCommandAsync()
        {
            await Context.Channel.TriggerTypingAsync();
            await base.ReplyAsync((Rand.Next(1, 6)) switch
            {
                1 => "!olleH",
                2 => "!raotS taH ehT owt oG",
                3 => "!pooS puoP knirD",
                4 => ".depfeQ ,ytiC ogitreV ni evil I",
                5 => "!haoW",
                _ => "!ainomleM dna dnalkseD ,ytiC ogitreV :depfeQ ni seitic eerht era erehT"
            });
        }

        [Command("orangeify")]
        [Summary("Returns the user input in Orange Language.")]
        [Remarks("Not CM")]
        public async Task OrangeifyCommandAsync([Remainder] string input)
        {
            await Context.Channel.TriggerTypingAsync();
            var orangeified = new StringBuilder();
            int length = input.Length - 1;
            while (length >= 0)
            {
                orangeified.Append(input[length]);
                length--;
            }
            if ((await Moderation.CheckSwearAsync(input)) || (await Moderation.CheckSwearAsync(orangeified.ToString())))
            {
                if (Context.IsPrivate)
                {
                    IGuildUser Doc671 = Context.Guild.GetUser(224267581370925056);
                    await ReplyAsync($"Profanity detected. {Doc671.Mention}");
                }
                else await Log($"Profanity detected: {input}");
            }
            else await ReplyAsync(orangeified.ToString());
        }

        [Command("random")]
        [Summary("Returns a random number with user-defined bounds.")]
        public async Task RandomCommandAsync(string rStart, string rEnd)
        {
            await Context.Channel.TriggerTypingAsync();
            var start = rStart.ToInt();
            var end = rEnd.ToInt();
            if (start < 0 || end < 0) await ReplyAsync("Only use positive numbers!");
            else if (start > end)
            {
                try
                {
                    int randNumber = Rand.Next(end, start);
                    await ReplyAsync(randNumber.ToString());
                }
                catch (FormatException)
                {
                    await ReplyAsync("Number too large/small.");
                    throw;
                }
            }
            else
            {
                try
                {
                    int randNumber = Rand.Next(start, end);
                    await ReplyAsync(randNumber.ToString());
                }
                catch (FormatException)
                {
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
            byte level = Convert.ToByte(Rand.Next(0, 25));
            int xp = level * 100 * Rand.Next(1, 5);

            var msgs = await Context.Channel.GetMessagesAsync(100).FlattenAsync();
            byte ranks = 0;

            foreach (IMessage msg in msgs)
            {
                if (msg != null && msg.Content == "mb/rank" && msg.Author == Context.Message.Author)
                    ranks++;
                else if (msg == null)
                    break;
            }

            var flavour = ranks switch
            {
                1 => "Pretty cool, right?",
                2 => "100% legitimate",
                3 => "I have a feeling you doubt me. Why is that?",
                4 => "What? I'm telling the truth, I swear!",
                5 => "What do you mean: \"This is random!\"?",
                6 => "Stop! Now!",
                7 => "I mean, you're probably breaking a no-spam rule!",
                8 => "...or slowmode is on...",
                9 => "Please... don't expose me... ;-;",
                10 => "At least I tried to generate a level...",
                11 => "I want to cry now. I really do.",
                12 => "...and I cry acid.",
                13 => "Just kidding, I actually cry Poup Soop...",
                14 => "...which has acid in it...",
                15 => "Why are you still going?",
                16 => "Aren't you bored?",
                17 => "Don't you have anything better to do?",
                18 => "No? I suppose not.You've used this command 18 times in the past 100 messages, after all.",
                19 => "Hm.",
                20 => "You know... I do actually have something for you...",
                _ => $"Your stage is {GetUser(Context).Stage}!"
            };

            builder.AddField("Level", level, true)
                .AddField("Total XP", xp, true)
                .WithColor(GetColor(Context))
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
                case "desk":
                case "desk176":
                case "desks": rating = 11; message = "what did you expect?"; break;
                case "dGFibGU=": rating = -999; message = "do not mention that unholy creature near me"; break;
                case "dGFibGVz": rating = -999; message = "do not mention those unholy creatures near me"; break;
                case "doc671": rating = -1; message = "terrible at everything"; break;
                case "dockque the rockque": rating = -1; message = "AM NOT ROCKQUE YOU MELMON"; break;
                case "egnaro": rating = 10; message = "!egnarO"; break;
                case "erango": rating = 0; message = "stoP noW"; break;
                case "marblebot's creator":
                case "my creator": input = "Doc671"; goto case "doc671";
                case "orange": goto case "egnaro";
                case "orange day": rating = 10; message = "!yaD egnarO"; break;
                case "poos puop": rating = 10; message = "!pooS puoP knirD"; break;
                case "poup soop": goto case "poos puop";
                case "rockque":
                case "rockques": rating = -1; message = "I Hate Rocks"; break;
                case "table": rating = -999; message = "do not mention that unholy creature near me"; input = "dGFibGU="; break;
                case "tables": rating = -999; message = "do not mention those unholy creatures near me"; input = "dGFibGVz"; break;
                case "the hat stoar": rating = 10; message = "!raotS taH ehT owt oG"; break;
                case "vinh": rating = 10; message = "Henlo Cooooooooool Vinh"; break;
                case "yad egnaro": goto case "orange day";
                default: rating = Rand.Next(0, 11); break;
            }
            switch (input.ToLower())
            {
                // These have custom messages but no preset ratings:
                case "blueice57": message = "icccce"; break;
                case "confusial": message = "I Am In Confusial Why"; break;
                case "dann": message = "you guys, are a rat kids"; break;
                case "eknimpie": message = "EKNIMPIE YOUR A RAXIST"; break;
                case "flam":
                case "flame":
                case "flamevapour": message = "Am Flam Flam Flam Flam Flam Flam"; break;
                case "flurp": message = "FLURP I TO SIGN UP AND NOT BE"; break;
                case "george012": message = "henlo jorj"; break;
                case "icce": goto case "blueice57";
                case "+inf": message = "marbles will realize in +inf"; break;
                case "jgeoroegeos":
                case "jorj": goto case "george012";
                case "ken":
                case "kenlimepie":
                case "keylimepie": message = "#kenismelmon"; break;
                case "luka": message = "LUKA\nYOU BLUMT"; break;
                case "marblebot": input = "myself"; goto case "myself";
                case "myself": message = "who am I?"; break;
                case "matheus":
                case "matheus fazzion": message = "marbles will realize in +inf"; break;
                case "meadow":
                case "mei doe": message = "somebody toucha mei doe"; break;
                case "melmon": message = "Whyn Arey Yoou A Melmon"; break;
                case "no u": message = "No Your"; break;
                case "no your": message = "No You're"; break;
                case "no you're": message = "N'Yuroe"; break;
                case "oh so muches": message = "Is To Much"; break;
                case "rackquette": message = "WHACC"; break;
                case "sand dollar": message = "XXX"; break;
                case "shotgun": message = "Vinh Shotgun All"; break;
                case "you silly desk": message = "Now I\nCry"; break;
            }

            string emoji = rating switch
            {
                -999 => ":gun: :dagger: :bomb:",
                -1 => ":gun:",
                0 => ":no_entry_sign:",
                1 => ":nauseated_face:",
                2 => ":rage:",
                3 => ":angry:",
                4 => ":slight_frown:",
                5 => ":neutral_face:",
                6 => ":slight_smile:",
                7 => ":grinning:",
                8 => ":thumbsup:",
                9 => ":white_check_mark:",
                10 => ":rofl:",
                11 => ":heart:",
                _ => ":thinking:",
            };
            if (message == "")
            {
                switch (rating)
                {
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
            if (rating == -2) await ReplyAsync($"**{Context.User.Username}**, I rATE {input} UNd3FINED10. {emoji}\n({message})");
            else
            {
                if (await Moderation.CheckSwearAsync(input))
                {
                    if (Context.IsPrivate)
                    {
                        IGuildUser Doc671 = Context.Guild.GetUser(224267581370925056);
                        await ReplyAsync($"Profanity detected. {Doc671.Mention}");
                    }
                    else await Log($"Profanity detected: {input}");
                }
                else await ReplyAsync($"**{Context.User.Username}**, I rate {input} **{rating}**/10. {emoji}\n({message})");
            }
        }

        [Command("repeat")]
        [Summary("Repeats the given message.")]
        public async Task RepeatCommandAsync([Remainder] string repeat)
        {
            await Context.Channel.TriggerTypingAsync();
            if (repeat == "Am Melmon") await ReplyAsync("No U");
            else if (await Moderation.CheckSwearAsync(repeat))
            {
                if (Context.IsPrivate)
                {
                    IGuildUser Doc671 = Context.Guild.GetUser(224267581370925056);
                    await ReplyAsync($"Profanity detected. {Doc671.Mention}");
                }
                else await Log($"Profanity detected: {repeat}");
            }
            else await ReplyAsync(repeat);
        }

        [Command("reverse")]
        [Summary("Returns the user input reversed.")]
        public async Task ReverseCommandAsync([Remainder] string input)
        {
            await Context.Channel.TriggerTypingAsync();
            // Another version of orangeify, but for CM (can secretly be used elsewhere)
            var reverse = new StringBuilder();
            int length = input.Length - 1;
            while (length >= 0)
            {
                reverse.Append(input[length]);
                length--;
            }
            if ((await Moderation.CheckSwearAsync(input)) || (await Moderation.CheckSwearAsync(reverse.ToString())))
            {
                if (Context.IsPrivate)
                {
                    IGuildUser Doc671 = Context.Guild.GetUser(224267581370925056);
                    await ReplyAsync($"Profanity detected. {Doc671.Mention}");
                }
                else await Log($"Profanity detected: {input}");
            }
            else await ReplyAsync(reverse.ToString());
        }

        [Command("vinhglish")]
        [Summary("Returns a Vinhglish word, its inventor and meaning.")]
        [Remarks("Not CM")]
        public async Task VinhglishCommandAsync([Remainder] string word = "")
        {
            int randNo = 0;
            bool wordSet = false;
            var wordList = new string[100];
            var invList = new string[100];
            var descList = new string[100];
            int a = 0;
            if (word == "")
            {
                using (StreamReader stream = new StreamReader($"Resources{Path.DirectorySeparatorChar}Vinhglish.csv"))
                {
                    while (!stream.EndOfStream)
                    {
                        string list = stream.ReadLine();
                        string[] vocab = list.Split(',');
                        wordList[a] = vocab[0];
                        invList[a] = vocab[1];
                        descList[a] = vocab[2];
                        a++;
                    }
                }
                randNo = Rand.Next(1, a);
            }
            else
            {
                using (StreamReader stream = new StreamReader($"Resources{Path.DirectorySeparatorChar}Vinhglish.csv"))
                {
                    while (!stream.EndOfStream)
                    {
                        string list = stream.ReadLine();
                        string[] vocab = list.Split(',');
                        wordList[a] = vocab[0];
                        invList[a] = vocab[1];
                        descList[a] = vocab[2];
                        if (wordList[a].ToLower() == word.ToLower())
                        {
                            randNo = a;
                            stream.Close();
                            wordSet = true;
                            break;
                        }
                        a++;
                    }
                    if (!wordSet) randNo = Rand.Next(1, a);
                }
            }
            await ReplyAsync($"**__{wordList[randNo]}__**\nInventor: {invList[randNo]}\nDescription: {descList[randNo]}");
        }
    }
}