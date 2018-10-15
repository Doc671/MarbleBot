using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

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
                if (answer == "")
                {
                    await ReplyAsync("A game of jumble is already active!");
                }
                else
                {
                    for (int i = 0; i < answer.Length - 1; i++)
                    {
                        if (answer[i] == ' ')
                        {
                            answer.Replace("", string.Empty);
                        }
                    }
                    if (answer.ToLower() == word.ToLower())
                    {
                        await ReplyAsync("**" + Context.User.Username + "** guessed the word! Well done!");
                        Global.jumbleActive = false;
                    }
                    else
                    {
                        await ReplyAsync("Incorrect.");
                    }
                }
            }
            else
            {
                Global.jumbleActive = true;
                string[] wordList = new string[60];
                int a = 0;
                using (StreamReader stream = new StreamReader("Jumble.csv"))
                {
                    while (!stream.EndOfStream)
                    {
                        string list = stream.ReadLine();
                        wordList[a] = list;
                        Console.WriteLine(list);
                        a += 1;
                    }
                    word = wordList[Global.rand.Next(0, wordList.Length)];
                }

                char[] wordArray = word.ToCharArray();
                Console.WriteLine(wordArray);

                for (a = 0; a < word.Length - 1; a++)
                {
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
        public async Task _race(string command = "", [Remainder] string option = null)
        {
            await Context.Channel.TriggerTypingAsync();
            ulong fileID = 0ul;
            if (Context.IsPrivate) fileID = Context.User.Id; else fileID = Context.Guild.Id;
            Color coloure = Color.LightGrey;
            if (!Context.IsPrivate) {
                switch (Context.Guild.Id) {
                    case Global.CM: coloure = Color.Teal; break;
                    case Global.THS: coloure = Color.Orange; break;
                    case Global.THSC: coloure = Color.Orange; break;
                    case Global.MT: coloure = Color.DarkGrey; break;
                    case Global.VFC: coloure = Color.Blue; break;
                    default: coloure = Color.LightGrey; break;
                }
            }
            EmbedBuilder builder = new EmbedBuilder()
                .WithColor(coloure)
                .WithCurrentTimestamp();
            if (command == string.Empty && option == string.Empty && !Global.raceActive) {
                builder.AddField("How to play", "Use `mb/race signup [marble name]` to sign up as a marble!\nWhen everyone's done (or when 10 people have signed up), use `mb/race start`!")
                    .WithTitle("Marble Race!");
                await ReplyAsync("", false, builder.Build());
            } else if (command == "signup" && !Global.raceActive) {
                string name = "";
                if (option == " " || option == null || option == string.Empty) name = Context.User.Username;
                else name = option;
                builder.AddField("Signed up!", "**" + Context.User.Username + "** has successfully signed up as **" + name + "**!");
                if (!File.Exists(fileID.ToString() + "race.txt")) File.Create(fileID.ToString() + "race.txt");
                using (var marbleList = new StreamWriter(fileID.ToString() + "race.txt", true))
                {
                    await marbleList.WriteLineAsync(name);
                    marbleList.Close();
                }
                Global.id++;
                Global.alive++;
                await ReplyAsync("", false, builder.Build());
                if (Global.alive > 9) {
                    await ReplyAsync("The limit of 10 contestants has been reached!");
                    await _race("start");
                }
            } else if (command == "start") {
                string[] marbles = new string[10];
                using (var marbleList = new StreamReader(fileID.ToString() + "race.txt"))
                {
                    var i = 0;
                    while (!marbleList.EndOfStream)
                    {
                        marbles[i] = await marbleList.ReadLineAsync();
                        i++;
                    }
                    marbleList.Close();
                }
                Global.raceActive = true;
                builder.WithTitle("The race has started!");
                var msg = await ReplyAsync("", false, builder.Build());
                Thread.Sleep(1500);
                while (Global.alive > 1)
                {
                    int eliminated = 0;
                    do {
                        eliminated = Global.rand.Next(0, Global.id);
                    } while (marbles[eliminated] == "///out");
                    int choice = Global.rand.Next(0, 15);
                    string deathmsg = "";
                    switch (choice)
                    {
                        case 0: deathmsg = "was flung away by a spinner"; break;
                        case 1: deathmsg = "was too slow to get into the bowl"; break;
                        case 2: deathmsg = "kept getting hit by other marbles"; break;
                        case 3: deathmsg = "glitched out"; break;
                        case 4: deathmsg = "got scared, rolled away"; break;
                        case 5: deathmsg = "invoked the Magenta Curse"; break;
                        case 6: deathmsg = "missed the shooter"; break;
                        case 7: deathmsg = "teleported to the start"; break;
                        case 8: deathmsg = "fell behind the other marbles"; break;
                        case 9: deathmsg = "ricocheted off the wall"; break;
                        case 10: deathmsg = "contracted the Magenta Virus"; break;
                        case 11: deathmsg = "was not Approved by Orange"; break;
                        case 12: deathmsg = "lacked the will to go on"; break;
                        case 13: deathmsg = "realized in +inf"; break;
                        case 14: deathmsg = "got stuck"; break;
                        case 15: deathmsg = "flew off the screen"; break;
                        case 16: deathmsg = "was voted out"; break;
                        case 17: deathmsg = "got beat by the boss"; break;
                        case 18: deathmsg = "ran out of time"; break;
                        case 19: deathmsg = "went backwards"; break;
                    }
                    builder.AddField("**" + marbles[eliminated] + "** is eliminated!", marbles[eliminated] + " " + deathmsg + " and is now out of the competition!");
                    marbles[eliminated] = "///out";
                    Global.alive--;
                    await msg.ModifyAsync(_msg => _msg.Embed = builder.Build());
                    Thread.Sleep(1500);
                }
                for (int a = 0; a < marbles.Length - 1; a++)
                {
                    if (marbles[a] != "///out" && !string.IsNullOrEmpty(marbles[a]) && !string.IsNullOrWhiteSpace(marbles[a]))
                    {
                        builder.AddField("**" + marbles[a] + "** wins!", marbles[a] + " is the winner!");
                        await msg.ModifyAsync(_msg => _msg.Embed = builder.Build());
                        await ReplyAsync("**" + marbles[a] + "** won the race!");
                        break;
                    }
                }
                Global.id = 0;
                Global.alive = 0;
                using (var marbleList = new StreamWriter(fileID.ToString() + "race.txt"))
                {
                    await marbleList.WriteAsync("");
                    marbleList.Close();
                }
                Global.raceActive = false;
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
