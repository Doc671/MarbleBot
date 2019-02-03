using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MarbleBot.BaseClasses;
using MarbleBot.Extensions;

namespace MarbleBot.Modules
{
    public class General : ModuleBase<SocketCommandContext>
    {
        /// <summary>
        /// General commands that don't fit into any of the other classes
        /// </summary>

        [Command("help")]
        [Summary("Gives the user help.")]
        public async Task _help([Remainder] string command = "")
        {
            await Context.Channel.TriggerTypingAsync();
            if (command == "") {
                EmbedBuilder builder = new EmbedBuilder();

                builder.AddField("MarbleBot Help", "*by Doc671#1965*\nUse mb/help followed by a command name for more info!")
                    .WithTimestamp(DateTime.UtcNow);
                if (Context.IsPrivate) {
                    builder.AddField("Fun Commands", "\n7ball (predicts an outcome)\nbet (bets on a marble out of a chosen number)\nbuyhat (buys an Uglee Hat)\nchoose (chooses between options split with '|')\norange (gives a random statement in Orange Language)\norangeify (turns a message you say into Orange Language)\nrate (rates something out of 10)\nrandom (returns a random positive integer with defined bounds)\nrank (shows your level and total XP)\nrepeat (repeats a message you say)\nuptime (shows how long the bot has been running)\nvinhglish (shows the meaning and inventor of a Vinhglish word)")
                    .AddField("Economy Commands", "balance (returns how much money you or someone else has)\nbuy (buy an item)\ndaily (gives daily money)\nitem (view item info)\npoupsoop (calculates price total)\nprofile (returns profile of you or someone else)\nrichlist (shows 10 richest people)\nsell (sell an item)\nshop (view all items)")
                    .AddField("YouTube Commands", "searchchannel (searches for a channel)\nsearchvideo (searches for a video)")
                    .AddField("Games", "\nrace (participate in a marble race)\nsiege (participate in a Marble Siege)")
                    .WithColor(Color.DarkerGrey);
                } else {
                    switch (Context.Guild.Id) {
                        case Global.CM:
                            builder.AddField("Command List", "help (should be fairly obvious)")
                                .AddField("Fun Commands", "7ball (predicts an outcome)\nbest (picks a random user to call the best)\nbet (bets on a marble out of a chosen number)\nchoose (chooses between options split with '|')\nrate (rates something out of 10)\nrandom (returns a random positive integer with defined bounds)\nrank (shows your level and total XP)\nrepeat (repeats a message you say)\nreverse (reverses text)\nuptime (shows how long the bot has been running)")
                                .AddField("Economy Commands", "balance (returns how much money you or someone else has)\nbuy (buy an item)\ndaily (gives daily money)\nitem (view item info)\npoupsoop (calculates price total)\nprofile (returns profile of you or someone else)\nrichlist (shows 10 richest people)\nsell (sell an item)\nshop (view all items)")
                                .AddField("Role Commands", "give (gives a role)\ntake (takes a role)\nrolelist (lists all roles that can be given/taken)")
                                .AddField("YouTube Commands", "searchchannel (searches for a channel)\nsearchvideo (searches for a video)")
                                .AddField("Games", "\nrace (participate in a marble race)\nsiege (participate in a Marble Siege)")
                                .WithColor(Color.Teal);
                            break;
                        case Global.THS:
                            builder.AddField("Command List", "help (should be fairly obvious)")
                                .AddField("Fun Commands", "\n7ball (predicts an outcome)\nbest (picks a random user to call the best)\nbet (bets on a marble out of a chosen number)\nbuyhat (buys an Uglee Hat)\nchoose (chooses between options split with '|')\norange (gives a random statement in Orange Language)\norangeify (turns a message you say into Orange Language)\nrate (rates something out of 10)\nrandom (returns a random positive integer with defined bounds)\nrank (shows your level and total XP)\nrepeat (repeats a message you say)\nstaffcheck (checks the statuses of all staff members)\nuptime (shows how long the bot has been running)\nvinhglish (shows the meaning and inventor of a Vinhglish word)")
                                .AddField("Economy Commands", "balance (returns how much money you or someone else has)\nbuy (buy an item)\ndaily (gives daily money)\nitem (view item info)\npoupsoop (calculates price total)\nprofile (returns profile of you or someone else)\nrichlist (shows 10 richest people)\nsell (sell an item)\nshop (view all items)")
                                .AddField("Utility Commands", "serverinfo (displays information about the server)\nstaffcheck (checks the statuses of all staff members.")
                                .AddField("Role Commands", "give (gives a role)\ntake (takes a role)\nrolelist (lists all roles that can be given/taken)")
                                .AddField("YouTube Commands", "searchchannel (searches for a channel)\nsearchvideo (searches for a video)")
                                .AddField("Games", "\nrace (participate in a marble race)\nsiege (participate in a Marble Siege)")
                                .WithColor(Color.Orange);
                            break;
                        case Global.THSC:
                            builder.AddField("Command List", "help (should be fairly obvious)")
                                .AddField("Fun Commands", "\n7ball (predicts an outcome)\nbest (picks a random user to call the best)\nbet (bets on a marble out of a chosen number)\nbuyhat (buys an Uglee Hat)\nchoose (chooses between options split with '|')\norange (gives a random statement in Orange Language)\norangeify (turns a message you say into Orange Language)\nrate (rates something out of 10)\nrandom (returns a random positive integer with defined bounds)\nrank (shows your level and total XP)\nrepeat (repeats a message you say)\nstaffcheck (checks the statuses of all staff members)\nuptime (shows how long the bot has been running)\nvinhglish (shows the meaning and inventor of a Vinhglish word)")
                                .AddField("Economy Commands", "balance (returns how much money you or someone else has)\nbuy (buy an item)\ndaily (gives daily money)\nitem (view item info)\npoupsoop (calculates price total)\nprofile (returns profile of you or someone else)\nrichlist (shows 10 richest people)\nsell (sell an item)\nshop (view all items)")
                                .AddField("Utility Commands", "serverinfo (displays information about the server)\nstaffcheck (checks the statuses of all staff members.")
                                .AddField("Role Commands", "give (gives a role)\ntake (takes a role)\nrolelist (lists all roles that can be given/taken)")
                                .AddField("YouTube Commands", "searchchannel (searches for a channel)\nsearchvideo (searches for a video)")
                                .AddField("Games", "\nrace (participate in a marble race)\nsiege (participate in a Marble Siege)")
                                .WithColor(Color.Orange);
                            break;
                        case Global.MT:
                            builder.AddField("Command List", "help (should be fairly obvious)")
                                .AddField("Fun Commands", "\n7ball (predicts an outcome)\nbest (picks a random user to call the best)\nbet (bets on a marble out of a chosen number)\nbuyhat (buys an Uglee Hat)\nchoose (chooses between options split with '|')\norange (gives a random statement in Orange Language)\norangeify (turns a message you say into Orange Language)\nrate (rates something out of 10)\nrandom (returns a random positive integer with defined bounds)\nrank (shows your level and total XP)\nrepeat (repeats a message you say)\nstaffcheck (checks the statuses of all staff members)\nuptime (shows how long the bot has been running)\nvinhglish (shows the meaning and inventor of a Vinhglish word)")
                                .AddField("Economy Commands", "balance (returns how much money you or someone else has)\nbuy (buy an item)\ndaily (gives daily money)\nitem (view item info)\npoupsoop (calculates price total)\nprofile (returns profile of you or someone else)\nrichlist (shows 10 richest people)\nsell (sell an item)\nshop (view all items)")
                                .AddField("Utility Commands", "serverinfo (displays information about the server)\nstaffcheck (checks the statuses of all staff members.")
                                .AddField("Role Commands", "give (gives a role)\ntake (takes a role)\nrolelist (lists all roles that can be given/taken)")
                                .AddField("YouTube Commands", "searchchannel (searches for a channel)\nsearchvideo (searches for a video)")
                                .AddField("Games", "\nrace (participate in a marble race)\nsiege (participate in a Marble Siege)")
                                .WithColor(Color.DarkGrey);
                            break;
                        case Global.VFC:
                            builder.AddField("Command List", "help (should be fairly obvious)")
                                .AddField("Fun Commands", "\n7ball (predicts an outcome)\nbest (picks a random user to call the best)\nbet (bets on a marble out of a chosen number)\nbuyhat (buys an Uglee Hat)\nchoose (chooses between options split with '|')\norange (gives a random statement in Orange Language)\norangeify (turns a message you say into Orange Language)\nrate (rates something out of 10)\nrandom (returns a random positive integer with defined bounds)\nrank (shows your level and total XP)\nrepeat (repeats a message you say)\nstaffcheck (checks the statuses of all staff members)\nuptime (shows how long the bot has been running)\nvinhglish (shows the meaning and inventor of a Vinhglish word)")
                                .AddField("Economy Commands", "balance (returns how much money you or someone else has)\nbuy (buy an item)\ndaily (gives daily money)\nitem (view item info)\npoupsoop (calculates price total)\nprofile (returns profile of you or someone else)\nrichlist (shows 10 richest people)\nsell (sell an item)\nshop (view all items)")
                                .AddField("Utility Commands", "serverinfo (displays information about the server)\nstaffcheck (checks the statuses of all staff members.")
                                .AddField("Role Commands", "give (gives a role)\ntake (takes a role)\nrolelist (lists all roles that can be given/taken)")
                                .AddField("YouTube Commands", "searchchannel (searches for a channel)\nsearchvideo (searches for a video)")
                                .AddField("Games", "\nrace (participate in a marble race)\nsiege (participate in a Marble Siege)")
                                .WithColor(Color.Blue);
                            break;
                        default:
                            break;
                    }
                }
                await ReplyAsync("", false, builder.Build());
            } else {
                MBCommand bCommand = new MBCommand();
                string THSOnly = "This command cannot be used in Community Marble!";
                bCommand.Name = command;
                switch (command)
                {
                    // General
                    case "7ball": bCommand.Desc = "Predicts an outcome to an event."; bCommand.Usage = "mb/7ball <condition>"; bCommand.Example = "mb/7ball Will I break?"; break;
                    case "best": bCommand.Desc = "Picks a random user in the server to call the best."; bCommand.Usage = "mb/best"; break;
                    case "bet": bCommand.Desc = "Bets on a marble to win from a list of up to 100."; bCommand.Usage = "mb/bet [number of marbles]"; bCommand.Example = "mb/bet 30"; break;
                    case "buyhat": bCommand.Desc = "Picks a random user in the server to call the best."; bCommand.Usage = "mb/buyhat"; bCommand.Warning = THSOnly; break;
                    case "choose": bCommand.Desc = "Chooses between several choices"; bCommand.Usage = "mb/choose <choice1> | <choice2>"; bCommand.Example = "Example: `mb/choose Red | Yellow | Green | Blue"; break;
                    case "orange": bCommand.Desc = "Gives a random statement in Orange Language."; bCommand.Usage = "mb/orange"; bCommand.Warning = THSOnly; break;
                    case "orangeify": bCommand.Desc = "Translates text into Orange Language."; bCommand.Usage = "mb/orangeify <text>"; bCommand.Example = "mb/orangeify Drink Poup Soop!"; bCommand.Warning = THSOnly; break;
                    case "override": bCommand.Desc = "Nothing."; bCommand.Usage = "Don't even think about it."; break;
                    case "random": bCommand.Desc = "Gives a random number between user-defined bounds."; bCommand.Usage = "mb/random <number1> <number2>"; bCommand.Example = "mb/random 1 5"; break;
                    case "rank": bCommand.Desc = "Returns the XP and level of the user."; bCommand.Usage = "mb/rank"; break;
                    case "rate": bCommand.Desc = "Rates something between 0 and 10."; bCommand.Usage = "mb/rate <text>"; bCommand.Example = "mb/rate Marbles"; break;
                    case "repeat": bCommand.Desc = "Repeats given text."; bCommand.Usage = "mb/repeat <text>"; bCommand.Example = "mb/repeat Hello!"; break;
                    case "reverse": bCommand.Desc = "Reverses text."; bCommand.Usage = "mb/reverse <text>"; bCommand.Example = "mb/reverse Bowl"; break;
                    case "serverinfo": bCommand.Desc = "Displays information about a server."; bCommand.Usage = "mb/serverinfo"; break;
                    case "staffcheck": bCommand.Desc = "Displays a list of all staff members and their statuses."; bCommand.Usage = "mb/staffcheck"; break;
                    case "uptime": bCommand.Desc = "Displays how long the bot has been running."; bCommand.Usage = "mb/uptime"; break;
                    case "userinfo": bCommand.Desc = "Displays information about a user."; bCommand.Usage = "mb/userinfo <user>"; bCommand.Example = "mb/userinfo MarbleBot"; bCommand.Warning = "This command doesn't work!"; break;
                    case "vinhglish": bCommand.Desc = "Displays information about a Vinhglish word."; bCommand.Usage = "mb/vinglish OR mb/vinhglish <word>"; bCommand.Example = "mb/vinhglish Am Will You"; bCommand.Warning = THSOnly; break;

                    // Economy
                    case "balance": bCommand.Desc = "Returns how much money you or someone else has."; bCommand.Usage = "mb/balance <optional user>"; break;
                    case "buy": bCommand.Desc = "Buy items."; bCommand.Usage = "mb/buy <item ID> <# of items>"; bCommand.Example = "mb/buy 1 1"; break;
                    case "daily": bCommand.Desc = "Gives daily Units of Money (200 to the power of your streak minus one). You can only do this every 24 hours."; bCommand.Usage = "mb/balance"; break;
                    case "item": bCommand.Desc = "View information about an item."; bCommand.Usage = "mb/item <item ID>"; break;
                    case "poupsoop": bCommand.Desc = "Calculates the total price of Poup Soop."; bCommand.Usage = "mb/poupsoop <# Regular> | <# Limited> | <# Frozen> | <# Orange> | <# Electric> | <# Burning> | <# Rotten> | <# Ulteymut> | <# Variety Pack>"; bCommand.Example = "mb/poupsoop 3 | 1"; break;
                    case "profile": bCommand.Desc = "Returns the profile of you or someone else."; bCommand.Usage = "mb/profile <optional user>"; break;
                    case "richlist": bCommand.Desc = "Shows the ten richest people globally."; bCommand.Usage = "mb/richlist"; break;
                    case "sell": bCommand.Desc = "Sell items."; bCommand.Usage = "mb/sell <item ID> <# of items>"; bCommand.Example = "mb/sell 1 1"; break;
                    case "shop": bCommand.Desc = "Shows all items available for sale, their IDs and their prices."; bCommand.Usage = "mb/shop"; break;

                    // Roles
                    case "give": bCommand.Desc = "Gives a role if it is on the rolelist"; bCommand.Usage = "mb/give <role>"; bCommand.Example = "mb/give Owner"; break;
                    case "rolelist": bCommand.Desc = "Shows a list of roles that can be given/taken by `mb/give` and `mb/take`."; bCommand.Usage = "mb/rolelist"; break;
                    case "take": bCommand.Desc = "Takes a role if it is on the rolelist"; bCommand.Usage = "mb/take <role>"; bCommand.Example = "mb/take Criminal"; break;

                    // YT
                    case "cv": bCommand.Desc = "Posts a video in #community-videos."; bCommand.Usage = "mb/cv <video link> <optional description>"; bCommand.Example = "A thrilling race made with an incredible, one of a kind feature! https://www.youtube.com/watch?v=7lp80lBO1Vs"; bCommand.Warning = "This command only works in DMs!"; break;
                    case "searchchannel": bCommand.Desc = "Displays a list of channels that match the search criteria."; bCommand.Usage = "mb/searchchannel <channelname>"; bCommand.Example = "mb/searchchannel carykh"; break;
                    case "searchvideo": bCommand.Desc = "Displays a list of videos that match the search critera."; bCommand.Usage = "mb/searchvideo <videoname>"; bCommand.Example = "mb/searchvideo The Amazing Marble Race"; break;

                    // Games
                    case "race": bCommand.Desc = "Participate in a marble race!"; bCommand.Usage = "mb/race signup <marble name>, mb/race contestants, mb/race setcount auto, mb/race start, mb/race leaderboards <winners/mostUsed>, mb/race checkearn"; break;
                    case "siege": bCommand.Desc = "Participate in a Marble Siege boss battle!"; bCommand.Usage = "mb/siege signup <marble name>, mb/siege contestants, mb/siege start, mb/siege attack, mb/siege grab, mb/siege info, mb/siege boss <boss name>, mb/siege powerup <power-up name>"; break;
                }

                var message = new StringBuilder();

                message.Append("**__MarbleBot Help: `" + bCommand.Name + "` command__**\n*" + bCommand.Desc + "*\n\nUsage: `" + bCommand.Usage + "`");
                if (!(bCommand.Example == null)) message.Append("\nExample: `" + bCommand.Example + "`");
                if (!(bCommand.Warning == null)) message.Append("\n\n:warning: " + bCommand.Warning);

                await ReplyAsync(message.ToString());
            }
        }

        //[Command("help2")]
        //[Summary("Test command for improved help.")]
        //public async Task _help2()
        //{
        //    var msg = await ReplyAsync("E");
        //    await msg.AddReactionAsync(Emote.Parse("⬅"));
        //    await msg.AddReactionAsync(Emote.Parse("➡"));
        //    await msg.ModifyAsync(_msg => _msg.Content = "F");
        //}

        [Command("cmds")]
        [Summary("Basically just help.")]
        public async Task _cmds([Remainder] string command = "")
        {
            await _help(command);
        }

        [Command("7ball")]
        [Summary("Predicts an outcome of a user-defined event.")]
        public async Task _7ball([Remainder] string input)
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
        [Summary("Things to do with autoresponses")]
        [RequireOwner]
        public async Task _autoresponses(string option) {
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
        [Summary("Picks a random person to call the best")]
        public async Task _best()
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
                } else Console.WriteLine("oof");
            } else await ReplyAsync("That command doesn't work here!");
        }

        [Command("bet")]
        [Summary("Bets on a marble")]
        public async Task _bet(string no)
        {
            await Context.Channel.TriggerTypingAsync();
            int noOfMarbles = (int)Math.Round(Convert.ToDouble(no));
            if (noOfMarbles > 100) await ReplyAsync("The number you gave is too large. It needs to be 100 or below.");
            else if (noOfMarbles < 1) await ReplyAsync("The number you gave is too small.");
            int a = 0;
            string[,] marbles = new string[10, 10];
            using (StreamReader stream = new StreamReader("Marbles.csv")) {
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
            await ReplyAsync("**" + Context.User.Username + "**, I bet that **" + marbles[(d - 1), (c - 1)] + "** will win!");
        }

        [Command("buyhat")]
        [Summary("A user buys an Uglee Hat!")]
        public async Task _buyHat()
        {
            await Context.Channel.TriggerTypingAsync();
            if (Context.IsPrivate || Context.Guild.Id == Global.THS || Context.Guild.Id == Global.THSC || Context.Guild.Id == Global.MT || Context.Guild.Id == Global.VFC) {
                await ReplyAsync("That'll be " + (Global.Rand.Next(0, 10000000)).ToString() + " units of money please. Thank you for buying Uglee Hat #" + (Global.Rand.Next(0, 69042)).ToString() + "!");
            }
        }

        [Command("choose")]
        [Summary("Chooses between several choices")]
        public async Task _choose([Remainder] string input)
        {
            await Context.Channel.TriggerTypingAsync();
            string[] choices = input.Split('|');
            int choice = Global.Rand.Next(0, choices.Length);
            if (Moderation._checkSwear(input) || Moderation._checkSwear(choices[choice])) {
                if (Context.IsPrivate) {
                    IGuildUser Doc671 = Context.Guild.GetUser(224267581370925056);
                    await ReplyAsync("Profanity detected. " + Doc671.Mention);
                }
                else Console.WriteLine("[" + DateTime.UtcNow + "] Profanity detected: " + input);
            } else {
                await ReplyAsync("**" + Context.User.Username + "**, I choose **" + choices[choice].Trim() + "**!");
            }
        }

        [Command("orange")]
        [Summary("Gives the user a random statement in Orange Language.")]
        public async Task _orange()
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
        [Summary("Gives the user a random statement in Orange Language.")]
        public async Task _orangeify([Remainder] string input)
        {
            await Context.Channel.TriggerTypingAsync();
            string orangeified = "";
            int length = input.Length - 1;
            while (length >= 0) {
                orangeified += input[length];
                length--;
            }
            if (Context.IsPrivate || Context.Guild.Id == Global.THS || Context.Guild.Id == Global.MT || Context.Guild.Id == Global.VFC) {
                if (Moderation._checkSwear(input) || Moderation._checkSwear(orangeified)) {
                    if (Context.IsPrivate) {
                        IGuildUser Doc671 = Context.Guild.GetUser(224267581370925056);
                        await ReplyAsync("Profanity detected. " + Doc671.Mention);
                    } else Console.WriteLine("[" + DateTime.UtcNow + "] Profanity detected: " + input);
                } else {
                    await ReplyAsync(orangeified);
                }
            }
        }

        [Command("override")]
        [Summary("Desk Is Hacc")]
        public async Task _override(string command)
        {
            if (Context.User.Id == 224267581370925056) {
                await ReplyAsync("Overriding command blockages...");
                await Task.Delay(3000);
                await ReplyAsync("Overriding complete!");
                await Task.Delay(1000);
                await ReplyAsync("Performing " + command + " command...");
                await Task.Delay(2000);
                switch(command) {
                    case "buyhat": await _buyHat(); break;
                    case "orange": await _orange(); break;
                    case "haha": await ReplyAsync(Global.ARLastUse.ToString()); break;
                    default: await ReplyAsync("Unknown command."); break;
                }
            }
            else await ReplyAsync("OVERRIDE FAILURE. INSUFFICIENT PERMISSIONS.");
        }

        [Command("raid")]
        [Summary("Joke command - pretends user is raiding.")]
        public async Task _repeat()
        {
            await ReplyAsync("INITIATING ANTI-RAID PROTOCOL.");
        }

        [Command("random")]
        [Summary("Returns a random number with user-defined bounds.")]
        public async Task _random(string rStart, string rEnd)
        {
            await Context.Channel.TriggerTypingAsync();
            var start = rStart.ToInt();
            var end = rEnd.ToInt();
            if (start < 0 || end < 0) {
                await ReplyAsync("Only use positive numbers!");
            } else if (start > end) {
                try  {
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
        [Summary("Returns a randomised level and XP count")]
        public async Task _rank()
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
                case 5: flavour = "What do you mean: \"The level's going down!\"?"; break;
                case 6: flavour = "Stop! Now!"; break;
                case 7: flavour = "I mean, you're probably breaking a no-spam rule!"; break;
                case 8: flavour = "...or you're using this in a spam channel..."; break;
                case 9: flavour = "Please... don't expose me... ;-;"; break;
                case 10: flavour = "At least I tried to generate a level..."; break;
                case 11: flavour = ";;--;;"; break;
                case 12: flavour = "I want to cry now. I really do."; break;
                case 13: flavour = "...and I cry acid."; break;
                case 14: flavour = "Just kidding, I actually cry Poup Soop..."; break;
                case 15: flavour = "...which has acid in it..."; break;
                case 16: flavour = "agh"; break;
                case 17: flavour = "Why are you still going on?"; break;
                case 18: flavour = "Aren't you bored?"; break;
                case 19: flavour = "Don't you have anything better to do?"; break;
                case 20: flavour = "No? I suppose not. You've used this command 20 times in the past 100 messages, after all."; break;
                case 21: flavour = "Do you just want to see how far I'll go?"; break;
                case 22: flavour = "Fine. I'll stop then."; break;
                case 23: flavour = "Bye."; break;
                case 24: flavour = "Wasn't fun talking to you."; break;
                case 25: flavour = "ok really this is the last message"; break;
                default: flavour = ""; break;
            }

            builder.AddField("Level", level, true)
                .AddField("Total XP", xp, true)
                .WithColor(Global.GetColor(Context))
                .WithTimestamp(DateTime.UtcNow)
                .WithTitle(Context.User.Username + "#" + Context.User.Discriminator)
                .WithFooter(flavour);

            await ReplyAsync("", false, builder.Build());
        }

        [Command("rate")]
        [Summary("Rates something /10")]
        public async Task _rate([Remainder] string input)
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
                case "doc671": rating = -1; message = "terrible at everything"; break;
                case "dockque the rockque": rating = -1; message = "AM NOT ROCKQUE YOU MELMON"; break;
                case "erango": rating = 0; message = "stoP noW"; break;
                case "marblebot's creator": input = "Doc671"; goto case "doc671";
                case "marblebot's dad": input = "Doc671"; goto case "doc671";
                case "my creator": input = "Doc671"; goto case "doc671";
                case "my dad": input = "Doc671"; goto case "doc671";
                case "orange": rating = 10; message = "!egnarO"; break;
                case "poup soop": rating = 10; message = "!pooS puoP knirD"; break;
                case "rockque": rating = -1; message = "I Hate Rocks"; break;
                case "rockques": goto case "rockque";
                case "table": rating = -999; message = "do not mention that unholy creature near me"; input = "dGFibGU="; break;
                case "tables": rating = -999; message = "do not mention those unholy creatures near me"; input = "dGFibGVz"; break;
                case "the hat stoar": rating = 10; message = "!raotS taH ehT owt oG"; break;
                case "vinh": rating = 10; message = "Henlo Cooooooooool Vinh"; break;
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
                case "jorj": goto case "george012";
                case "ken": message = "#kenismelmon"; break;
                case "kenlimepie": goto case "ken";
                case "keylimepie": goto case "ken";
                case "marblebot": input = "myself"; goto case "myself";
                case "myself": message = "who am I?"; break;
                case "matheus": message = "marbles will realize in +inf"; break;
                case "matheus fazzion": goto case "matheus";
                case "meadow": message = "somebody toucha mei doe"; break;
                case "mei doe": goto case "meadow";
                case "melmon": message = "Wnhy Arey Yoou A Melmon"; break;
                case "no u": message = "No Your"; break;
                case "no your": message = "No You're"; break;
                case "no you're": message = "N'Yuroe"; break;
                case "oh so muches": message = "Is To Much"; break;
                case "rackquette":  message = "WHACC"; break;
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
                if (Moderation._checkSwear(input)) {
                    if (Context.IsPrivate) {
                        IGuildUser Doc671 = Context.Guild.GetUser(224267581370925056);
                        await ReplyAsync("Profanity detected. " + Doc671.Mention);
                    } else Console.WriteLine("[" + DateTime.UtcNow + "] Profanity detected: " + input);
                }
                else await ReplyAsync("**" + Context.User.Username + "**, I rate " + input + " **" + rating + "**/10. " + emoji + "\n(" + message + ")");
            }
        }

        [Command("repeat")]
        [Summary("Repeats the message they say.")]
        public async Task _repeat([Remainder] string repeat)
        {
            await Context.Channel.TriggerTypingAsync();
            if (repeat == "Am Melmon" && (Context.Guild.Id == Global.THS || Context.Guild.Id == Global.MT)) {
                await ReplyAsync("No U");
            } else if (Moderation._checkSwear(repeat)) {
                if (Context.IsPrivate) {
                    IGuildUser Doc671 = Context.Guild.GetUser(224267581370925056);
                    await ReplyAsync("Profanity detected. " + Doc671.Mention);
                }
                else Console.WriteLine("[" + DateTime.UtcNow + "] Profanity detected: " + repeat);
            } else {
                await ReplyAsync(repeat);
            }
        }

        [Command("reverse")]
        [Summary("Gives the user a random statement in Orange Language.")]
        public async Task _reverse([Remainder] string input)
        {
            await Context.Channel.TriggerTypingAsync();
            // Another version of orangeify, but for CM (can secretly be used elsewhere)
            string reverse = "";
            int length = input.Length - 1;
            while (length >= 0) {
                reverse += input[length];
                length--;
            }
            if (Moderation._checkSwear(input) || Moderation._checkSwear(reverse)) {
                if (Context.IsPrivate) {
                    IGuildUser Doc671 = Context.Guild.GetUser(224267581370925056);
                    await ReplyAsync("Profanity detected. " + Doc671.Mention);
                } else Console.WriteLine("[" + DateTime.UtcNow + "] Profanity detected: " + input);
            } else {
                await ReplyAsync(reverse);
            }
        }

        [Command("serverinfo")]
        [Summary("Returns some stats")]
        public async Task _serverinfo()
        {
            await Context.Channel.TriggerTypingAsync();
            if (!Context.IsPrivate) {
                EmbedBuilder builder = new EmbedBuilder();
                int botUsers = 0;
                int onlineUsers = 0;
                SocketGuildUser[] users = Context.Guild.Users.ToArray();
                for (int i = 0; i < Context.Guild.Users.Count - 1; i++)
                {
                    if (users[i].IsBot) botUsers++;
                    if (users[i].Status.ToString().ToLower() == "online") onlineUsers++;
                }
                builder.WithThumbnailUrl(Context.Guild.IconUrl)
                    .WithTitle(Context.Guild.Name)
                    .AddField("Owner", Context.Guild.GetUser(Context.Guild.OwnerId).Username + "#" + Context.Guild.GetUser(Context.Guild.OwnerId).Discriminator, true)
                    .AddField("Voice Region", Context.Guild.VoiceRegionId, true)
                    .AddField("Text Channels", Context.Guild.TextChannels.Count, true)
                    .AddField("Voice Channels", Context.Guild.VoiceChannels.Count, true)
                    .AddField("Members", Context.Guild.Users.Count, true)
                    .AddField("Bots", botUsers, true)
                    .AddField("Online", onlineUsers)
                    .AddField("Roles", Context.Guild.Roles.Count, true)
                    .WithColor(Global.GetColor(Context))
                    .WithTimestamp(DateTime.UtcNow)
                    .WithFooter(Context.Guild.Id.ToString());
                await ReplyAsync("", false, builder.Build());
            } else await ReplyAsync("This is a DM, not a server!");
        }

        [Command("stats")]
        [Summary("Returns some stats")]
        public async Task _stats()
        {
            string mensage = "";
            SocketGuildChannel[] channels = Context.Guild.TextChannels.ToArray();
            Array.Sort(channels, (x, y) => string.Compare(x.Name, y.Name));
            for (int i = 0; i < Context.Guild.Channels.Count - 1; i++) mensage += channels[i] + " ";
            await ReplyAsync(mensage);
        }

        [Command("staffcheck")]
        [Summary("Checks which staff are online/idle/DND/offline.")]
        public async Task _staffCheck()
        {
            await Context.Channel.TriggerTypingAsync();
            if (!Context.IsPrivate) {
                IGuildUser Doc671 = Context.Guild.GetUser(224267581370925056);
                if (Context.Guild.Id == Global.CM) {
                    IGuildUser Erikfassett = Context.Guild.GetUser(161258738429329408);
                    IGuildUser JohnDubuc = Context.Guild.GetUser(161247044713840642);
                    IGuildUser TAR = Context.Guild.GetUser(186652039126712320);
                    IGuildUser Algorox = Context.Guild.GetUser(323680030724980736);
                    IGuildUser FlameVapour = Context.Guild.GetUser(193247613095641090);
                    IGuildUser[] users = { Doc671, Erikfassett, JohnDubuc, TAR, Algorox, FlameVapour};
                    string[] nicks = new string[users.Length];
                    string[] statuses = new string[users.Length];
                    int h = 0;
                    foreach (var user in users) {
                        nicks[h] = user.Nickname;
                        statuses[h] = user.Status.ToString();
                        h++;
                    }
                    for (int i = 0; i < users.Length - 1; i++) {
                        if (nicks[i].IsEmpty()) nicks[i] = users[i].Username;
                        if (statuses[i] == "DoNotDisturb") statuses[i] = "Do Not Disturb";
                    }
                    var output = new StringBuilder();
                    output.Append("**__Admins:__** \n" + nicks[0] + " (" + users[0].Username + "#" + users[0].Discriminator + "): **" + statuses[0]);
                    output.Append("**\n" + nicks[1] + " (" + users[1].Username + "#" + users[1].Discriminator + "): **" + statuses[1]);
                    output.Append("**\n" + nicks[2] + " (" + users[2].Username + "#" + users[2].Discriminator + "): **" + statuses[2]);
                    output.Append("**\n" + nicks[3] + " (" + users[3].Username + "#" + users[3].Discriminator + "): **" + statuses[3]);
                    output.Append("\n\n**__Mods:__** \n" + nicks[4] + " (" + users[4].Username + "#" + users[4].Discriminator + "): **" + statuses[4]);
                    output.Append("**\n" + nicks[5] + " (" + users[5].Username + "#" + users[5].Discriminator + "): **" + statuses[5]);
                    await ReplyAsync(output.ToString());
                } else if (Context.Guild.Id == Global.THS) {
                    IGuildUser FlameVapour = Context.Guild.GetUser(193247613095641090);
                    IGuildUser DannyPlayz = Context.Guild.GetUser(329532528031563777);
                    IGuildUser George012 = Context.Guild.GetUser(232618363975630849);
                    IGuildUser Kenlimepie = Context.Guild.GetUser(195529549855850496);
                    IGuildUser[] users = { Doc671, FlameVapour, DannyPlayz, George012, Kenlimepie };
                    string[] nicks = new string[users.Length];
                    string[] statuses = new string[users.Length];
                    int h = 0;
                    foreach (var user in users) {
                        nicks[h] = user.Nickname;
                        statuses[h] = user.Status.ToString();
                        h++;
                    }
                    for (int i = 0; i < users.Length - 1; i++) {
                        if (nicks[i].IsEmpty()) nicks[i] = users[i].Username;
                        if (statuses[i] == "DoNotDisturb") statuses[i] = "Do Not Disturb";
                    }
                    var output = new StringBuilder();
                    output.Append("**__Overlords:__** \n" + nicks[0] + " (" + users[0].Username + "#" + users[0].Discriminator + "): **" + statuses[0]);
                    output.Append("**\n" + nicks[1] + " (" + users[1].Username + "#" + users[1].Discriminator + "): **" + statuses[1]);
                    output.Append("\n\n**__Hat Stoar Employees:__** \n" + nicks[2] + " (" + users[2].Username + "#" + users[2].Discriminator + "): **" + statuses[2]);
                    output.Append("**\n" + nicks[3] + " (" + users[3].Username + "#" + users[3].Discriminator + "): **" + statuses[3]);
                    output.Append("**\n" + nicks[4] + " (" + users[4].Username + "#" + users[4].Discriminator + "): **" + statuses[4]);
                    await ReplyAsync(output.ToString());
                } else if (Context.Guild.Id == Global.MT) {
                    IGuildUser George012 = Context.Guild.GetUser(232618363975630849);
                    IGuildUser[] users = { Doc671, George012 };
                    string[] nicks = { users[0].Nickname, users[1].Nickname, };
                    string[] statuses = { users[0].Status.ToString(), users[1].Status.ToString() };
                    for (int i = 0; i < users.Length; i++) {
                        if (nicks[i].IsEmpty()) nicks[i] = users[i].Username;
                        if (statuses[i] == "DoNotDisturb") statuses[i] = "Do Not Disturb";
                    }
                    await ReplyAsync(nicks[0] + " (" + users[0].Username + "#" + users[0].Discriminator + "): **" + statuses[0] + "**\n" + nicks[1] + " (" + users[1].Username + "#" + users[1].Discriminator + "): **" + statuses[1] + "**");
                } else if (Context.Guild.Id == Global.VFC) {
                    IGuildUser Vinh = Context.Guild.GetUser(311360247740760064);
                    IGuildUser George012 = Context.Guild.GetUser(232618363975630849);
                    IGuildUser Kenlimepie = Context.Guild.GetUser(195529549855850496);
                    IGuildUser Meadow = Context.Guild.GetUser(370463333763121152);
                    IGuildUser Ayumi = Context.Guild.GetUser(189713815414374404);
                    IGuildUser BlueIce57 = Context.Guild.GetUser(310960432909254667);
                    IGuildUser Miles = Context.Guild.GetUser(170804546438692864);
                    IGuildUser[] users = { Vinh, Kenlimepie, Doc671, George012, Meadow, Ayumi, BlueIce57, Miles };
                    string[] nicks = new string[users.Length];
                    string[] statuses = new string[users.Length];
                    int h = 0;
                    foreach (var user in users) {
                        nicks[h] = user.Nickname;
                        statuses[h] = user.Status.ToString();
                        h++;
                    }
                    for (int i = 0; i < users.Length - 1; i++) {
                        if (nicks[i].IsEmpty()) nicks[i] = users[i].Username;
                        if (statuses[i] == "DoNotDisturb") statuses[i] = "Do Not Disturb";
                    }
                    var output = new StringBuilder();
                    output.Append("**__Owner:__** \n" + nicks[0] + " (" + users[0].Username + "#" + users[0].Discriminator + "): **" + statuses[0]);
                    output.Append("**\n\n**__Co-owners:__** \n" + nicks[1] + " (" + users[1].Username + "#" + users[1].Discriminator + "): **" + statuses[1]);
                    output.Append("**\n" + nicks[2] + " (" + users[2].Username + "#" + users[2].Discriminator + "): **" + statuses[2]);
                    output.Append("\n\n**__Admins:__** \n" + nicks[3] + " (" + users[3].Username + "#" + users[3].Discriminator + "): **" + statuses[3]);
                    output.Append("**\n" + nicks[4] + " (" + users[4].Username + "#" + users[4].Discriminator + "): **" + statuses[4]);
                    output.Append("\n\n**__Mods:__** \n" + nicks[5] + " (" + users[5].Username + "#" + users[5].Discriminator + "): **" + statuses[5]);
                    output.Append("**\n" + nicks[6] + " (" + users[6].Username + "#" + users[6].Discriminator + "): **" + statuses[6]);
                    output.Append("**\n" + nicks[7] + " (" + users[7].Username + "#" + users[7].Discriminator + "): **" + statuses[7]);
                    await ReplyAsync(output.ToString());
                }
            } else await ReplyAsync("There are no staff members in a DM!");
        }

        [Command("uptime")]
        [Summary("Returns the bot's uptime")]
        public async Task _uptime() {
            var timeDiff = DateTime.UtcNow.Subtract(Global.StartTime);
            await ReplyAsync("The bot has been running for **" + Global.GetDateString(timeDiff) + "**.");
        }

        [Command("userinfo")]
        [Summary("Returns info of a user")]
        public async Task _userinfo([Remainder] string username = "#")
        {
            await Context.Channel.TriggerTypingAsync();
            EmbedBuilder builder = new EmbedBuilder();
            SocketGuildUser user = Context.Guild.GetUser(Context.User.Id);
            char[] userCharArray = username.ToCharArray();
            //int likeness = 0;
            //bool chosen = false;
            if (username.Substring(0, 1) == "<") {
                if (ulong.TryParse(username.Trim('<').Trim('>').Trim('@'), out ulong ID) == true) {
                    ID = ulong.Parse(username.Trim('<').Trim('>').Trim('@'));
                }
                else Console.WriteLine("[" + DateTime.UtcNow + "] mb/userinfo - Parsing failed!");
                user = Context.Guild.GetUser(ID);
            }

            string status = "";
            switch (Context.User.Status.ToString()) {
                case "online": status = "Online"; break;
                case "idle": status = "Idle"; break;
                case "donotdisturb": status = "Do Not Disturb"; break;
                case "invisible": status = "Offline"; break;
                case "offline": status = "Offline"; break;
            }

            string nickname;
            if (user.Nickname.IsEmpty()) nickname = "None";
            else nickname = user.Nickname;

            builder.WithThumbnailUrl(Context.User.GetAvatarUrl())
                .WithTitle(Context.User.Username + "#" + Context.User.Discriminator)
                .AddField("Status", status)
                .AddField("Nickname", nickname, true)
                .AddField("Registered", user.CreatedAt)
                .AddField("Joined", user.JoinedAt, true)
                .AddField("Roles", user.Roles)
                .WithColor(Global.GetColor(Context))
                .WithTimestamp(DateTime.UtcNow)
                .WithFooter(Context.Guild.Id.ToString());

            await ReplyAsync("", false, builder.Build());
        }

        [Command("vinhglish")]
        [Summary("Returns a Vinhglish word, its inventor and meaning")]
        public async Task _vinhglish([Remainder] string word = "")
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
            if (Context.IsPrivate || Context.Guild.Id == Global.THS || Context.Guild.Id == Global.MT || Context.Guild.Id == Global.VFC) await ReplyAsync("**__" + wordList[randNo] + "__**\nInventor: " + invList[randNo] + "\nDescription: " + descList[randNo]);
        }

        [Command("melmon")]
        [Summary("melmon")]
        [RequireOwner]
        public async Task _melmon(string melmon, [Remainder] string msg)
        {
            SocketGuild srvr = Context.Client.GetGuild(Global.THS);
            ISocketMessageChannel chnl = srvr.GetTextChannel(Global.THS);
            Console.WriteLine("Time For MElmonry >:)");
            switch(melmon) {
                case "desk": await chnl.SendMessageAsync(msg); break;
                case "flam": chnl = srvr.GetTextChannel(224277892182310912); await chnl.SendMessageAsync(msg); break;
                case "ken": srvr = Program._client.GetGuild(Global.CM); chnl = srvr.GetTextChannel(Global.CM); await chnl.SendMessageAsync(msg); break;
                case "adam": chnl = srvr.GetTextChannel(240570994211684352); await chnl.SendMessageAsync(msg); break;
                case "brady": chnl = srvr.GetTextChannel(237158048282443776); await chnl.SendMessageAsync(msg); break;
            }
        }

        [Command("update")]
        [Summary("Releases update info to all bot channels")]
        [RequireOwner]
        public async Task _update([Remainder] string info) {
            var builder = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithCurrentTimestamp()
                .WithDescription(info)
                .WithTitle("MarbleBot Update");

            ISocketMessageChannel chnl = Context.Client.GetGuild(Global.CM).GetTextChannel(Global.BotChannels[1]);
            var msg = await chnl.SendMessageAsync("", false, builder.Build());
            await msg.PinAsync();

            chnl = Context.Client.GetGuild(Global.THS).GetTextChannel(Global.BotChannels[0]);
            msg = await chnl.SendMessageAsync("", false, builder.Build());
            await msg.PinAsync();

            chnl = Context.Client.GetGuild(Global.THSC).GetTextChannel(Global.BotChannels[2]);
            msg = await chnl.SendMessageAsync("", false, builder.Build());
            await msg.PinAsync();

            chnl = Context.Client.GetGuild(Global.VFC).GetTextChannel(Global.BotChannels[3]);
            msg = await chnl.SendMessageAsync("", false, builder.Build());
            await msg.PinAsync();

            chnl = Context.Client.GetGuild(Global.MT).GetTextChannel(Global.BotChannels[4]);
            msg = await chnl.SendMessageAsync("", false, builder.Build());
            await msg.PinAsync();
        }

        [Command("Uh")]
        [RequireOwner]
        public async Task _uh()
        {
            await Context.Channel.SendFileAsync("Images/BossPreeTheTree.png");
            await Context.Channel.SendFileAsync("Images/BossHATTMANN.png");
            await Context.Channel.SendFileAsync("Images/BossOrange.png");
            await Context.Channel.SendFileAsync("Images/BossGreen.png");
            await Context.Channel.SendFileAsync("Images/BossDestroyer.png");
            /*await Context.Channel.SendFileAsync("Images/PUClone.png");
            await Context.Channel.SendFileAsync("Images/PUCure.png");
            await Context.Channel.SendFileAsync("Images/PUHeal.png");
            await Context.Channel.SendFileAsync("Images/PUMoraleBoost.png");
            await Context.Channel.SendFileAsync("Images/PUOverclock.png");
            await Context.Channel.SendFileAsync("Images/PUSummon.png");
            var builder = new EmbedBuilder()
                .WithImageUrl("attachment://PUCure.png")
                .WithTitle("Hi");
            await Context.Channel.SendFileAsync("attachment://PUCure.png", embed: builder.Build());*/
        }

        /*[Command("domybidding")]
        [RequireOwner]
        public async Task _domybidding()
        {
            if (Context.User.Id == 224267581370925056) {
                EmbedBuilder builder = new EmbedBuilder();
                builder.WithAuthor(Context.User)
                    .WithDescription("**Message edited in <#224277892182310912>**")
                    .AddField("Before", "update video eventulaly")
                    .AddField("After", "update video eventually")
                    .WithColor(Color.DarkBlue)
                    .WithFooter("User ID: 224267581370925056 • Yesterday at 18:40");
                await ReplyAsync("", false, builder.Build());
            }
        }*/
    }
}