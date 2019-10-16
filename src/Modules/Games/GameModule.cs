using Discord;
using Discord.Commands;
using MarbleBot.Common;
using MarbleBot.Extensions;
using MarbleBot.Modules.Games.Services;
using MarbleBot.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarbleBot.Modules.Games
{
    /// <summary> A module for game commands. </summary>
    public class GameModule : MarbleBotModule
    {
        public BotCredentials BotCredentials { get; set; }
        public GamesService GamesService { get; set; }

        /// <summary> Gets the string representation of the game. </summary>
        /// <param name="gameType"> The type of game. </param>
        /// <param name="capitalised"> Whether or not the name being returned is capitalised. </param>
        /// <returns> The string representation of the game. </returns>
        private string GameName(GameType gameType, bool capitalised = true)
        => capitalised ? Enum.GetName(typeof(GameType), gameType) : Enum.GetName(typeof(GameType), gameType).ToLower();

        /// <summary> Sends a message showing whether a user can earn from a game. </summary>
        /// <param name="context"> The context of the command. </param>
        /// <param name="gameType"> The type of game. </param>
        protected static async Task Checkearn(SocketCommandContext context, GameType gameType)
        {
            var user = GetUser(context);
            var lastWin = gameType switch
            {
                GameType.Race => user.LastRaceWin,
                GameType.Siege => user.LastSiegeWin,
                GameType.War => user.LastWarWin,
                _ => user.LastScavenge,
            };
            var nextEarn = DateTime.UtcNow.Subtract(lastWin);
            var output = nextEarn.TotalHours < 6 ?
                $"You can earn money from racing in **{GetDateString(lastWin.Subtract(DateTime.UtcNow.AddHours(-6)))}**!"
                : "You can earn money from racing now!";
            await context.Channel.SendMessageAsync(embed: new EmbedBuilder()
                .WithAuthor(context.User)
                .WithColor(GetColor(context))
                .WithCurrentTimestamp()
                .WithDescription(output)
                .Build());
        }

        /// <summary> Clears the contestant list. </summary>
        /// <param name="context"> The context of the command. </param>
        /// <param name="gameType"> The type of game. </param>
        protected async Task Clear(SocketCommandContext context, GameType gameType)
        {
            ulong fileId = context.IsPrivate ? context.User.Id : context.Guild.Id;
            if (BotCredentials.AdminIds.Any(id => id == context.User.Id) || context.IsPrivate)
            {
                using var marbleList = new StreamWriter($"Data{Path.DirectorySeparatorChar}{fileId}{GameName(gameType, false)}.csv", false);
                await marbleList.WriteAsync("");
                await context.Channel.SendMessageAsync("Contestant list successfully cleared!");
            }
            else await context.Channel.SendMessageAsync($"**{context.User.Username}**, you cannot do this!");
        }

        /// <summary> Shows leaderboards. </summary>
        /// <param name="orderedData"> The data to be made into a leaderboard. </param>
        /// <param name="no"> The part of the leaderboard that will be displayed. </param>
        /// <returns> A leaderboard string ready to be printed. </returns>
        protected static string Leaderboard(IEnumerable<(string elementName, int value)> orderedData, int no)
        {
            var dataList = new List<(int place, string elementName, int value)>();
            int displayedPlace = 0, lastValue = 0;
            foreach (var (elementName, value) in orderedData)
            {
                if (value != lastValue)
                    displayedPlace++;
                dataList.Add((displayedPlace, elementName, value));
                lastValue = value;
            }
            if (no > dataList.Last().place / 10)
                return $"There are no entries in page **{no}**!";
            // This displays in groups of ten (i.e. if no is 1, first 10 displayed;
            // no = 2, next 10, etc.
            int minValue = (no - 1) * 10 + 1, maxValue = no * 10;
            var output = new StringBuilder();
            foreach (var (place, elementName, value) in dataList)
            {
                if (place > maxValue) break;
                if (place < maxValue + 1 && place >= minValue)
                    output.AppendLine($"{place}{place.Ordinal()}: {elementName} {value}");
            }
            if (output.Length > 2048) return string.Concat(output.ToString().Take(2048));
            return output.ToString();
        }

        /// <summary> Removes a contestant from the contestant list of a game. </summary>
        /// <param name="context"> The context of the command. </param>
        /// <param name="gameType"> The type of game. </param>
        /// <param name="marbleToRemove"> The name of the marble to remove. </param>
        protected async Task RemoveContestant(SocketCommandContext context, GameType gameType, string marbleToRemove)
        {
            ulong fileId = context.IsPrivate ? context.User.Id : context.Guild.Id;
            string marbleListDirectory = $"Data{Path.DirectorySeparatorChar}{fileId}{GameName(gameType, false)}.csv";
            if (!File.Exists(marbleListDirectory))
            {
                await context.Channel.SendMessageAsync($"**{context.User.Username}**, no data exists for this {(context.IsPrivate ? "DM" : "guild")}! There are no marbles signed up to remove!");
                return;
            }
            // 0 - Not found, 1 - Found but not yours, 2 - Found & yours, 3 - Found & overridden
            int state = BotCredentials.AdminIds.Any(id => id == context.User.Id) ? 3 : 0;
            var wholeFile = new StringBuilder();
            string line;
            using (var marbleList = new StreamReader(marbleListDirectory))
            {
                while (!marbleList.EndOfStream)
                {
                    line = await marbleList.ReadLineAsync();
                    if (string.Compare(line.Split(',')[0], marbleToRemove, true) == 0)
                    {
                        if (ulong.Parse(line.Split(',')[1]) == context.User.Id)
                            state = 2;
                        else
                        {
                            wholeFile.AppendLine(line);
                            if (!(state == 2)) state = 1;
                        }
                    }
                    else wholeFile.AppendLine(line);
                }
            }
            
            switch (state)
            {
                case 0: await context.Channel.SendMessageAsync($"**{context.User.Username}**, could not find the requested marble!"); break;
                case 1: await context.Channel.SendMessageAsync($"**{context.User.Username}**, this is not your marble!"); break;
                case 2:
                case 3:
                    string bold = marbleToRemove.Contains('*') || marbleToRemove.Contains('\\') ? "" : "**";
                    using (var marbleList = new StreamWriter(marbleListDirectory, false))
                    {
                        await marbleList.WriteAsync(wholeFile.ToString());
                        await context.Channel.SendMessageAsync($"**{context.User.Username}**, removed contestant {bold}{marbleToRemove}{bold}!");
                    }
                    break;
            }
        }

        /// <summary> Returns a message showing the contestants currently signed up to the game. </summary>
        /// <param name="context"> The context of the command. </param>
        /// <param name="gameType"> The type of game. </param>
        protected async Task ShowContestants(SocketCommandContext context, GameType gameType)
        {
            ulong fileId = context.IsPrivate ? context.User.Id : context.Guild.Id;
            string marbleListDirectory = $"Data{Path.DirectorySeparatorChar}{fileId}{GameName(gameType, false)}.csv";
            if (!File.Exists(marbleListDirectory))
            {
                await context.Channel.SendMessageAsync($"**{context.User.Username}**, no data exists for this {(context.IsPrivate ? "DM" : "guild")}! No-one is signed up!");
                return;
            }

            var marbles = new StringBuilder();
            int count = 0;
            using (var marbleList = new StreamReader(marbleListDirectory))
            {
                var allMarbles = (await marbleList.ReadToEndAsync()).Split('\n');
                for (count = 0; count < allMarbles.Length; count++)
                {
                    string marble = allMarbles[count];
                    if (marble.Length > 16)
                    {
                        var marbleInfo = marble.Split(',');
                        string bold = marbleInfo[0].Contains('*') || marbleInfo[0].Contains('\\') ? "" : "**";
                        var user = context.Client.GetUser(ulong.Parse(marbleInfo[1]));

                        if (user == null)
                        {
                            marbles.AppendLine($"{bold}{marbleInfo[0]}{bold}");
                            continue;
                        }

                        marbles.AppendLine($"{bold}{marbleInfo[0]}{bold} {(context.IsPrivate ? "" : $"[{user.Username}#{user.Discriminator}]")}");
                    }
                }
            }

            var output = marbles.ToString();
            if (string.IsNullOrEmpty(output))
                await context.Channel.SendMessageAsync("No-one is signed up!");
            else
                await context.Channel.SendMessageAsync(embed: new EmbedBuilder()
                    .WithColor(GetColor(context))
                    .WithCurrentTimestamp()
                    .WithFooter($"Contestant count: {count - 1}")
                    .WithTitle($"Marble {GameName(gameType)}: Contestants")
                    .AddField("Contestants", output)
                    .Build());
        }

        /// <summary> Removes a contestant from the contestant list of a game. </summary>
        /// <param name="context"> The context of the command. </param>
        /// <param name="gameType"> The type of game. </param>
        /// <param name="marbleName"> The name of the contestant signing up. </param>
        /// <param name="marbleLimit"> The maximum number of marbles that can be signed up. </param>
        /// <param name="startCommand"> The command to execute if the marble limit has been met. </param>
        /// <param name="itemId"> (War only) The ID of the weapon the marble is joining with. </param>
        protected async Task Signup(SocketCommandContext context, GameType gameType, string marbleName, int marbleLimit,
            Func<Task> startCommand, string itemId = "")
        {
            ulong fileId = context.IsPrivate ? context.User.Id : context.Guild.Id;
            string marbleListDirectory = $"Data{Path.DirectorySeparatorChar}{fileId}{GameName(gameType, false)}.csv";
            if (!File.Exists(marbleListDirectory)) File.Create(marbleListDirectory).Close();

            var weapon = new Weapon();

            if (gameType == GameType.Siege || gameType == GameType.War)
            {
                if (gameType == GameType.Siege)
                {
                    if (GamesService.SiegeInfo.ContainsKey(fileId) && GamesService.SiegeInfo[fileId].Active)
                    {
                        await context.Channel.SendMessageAsync($"**{context.User.Username}**, a battle is currently ongoing!");
                        return;
                    }
                }
                else if (gameType == GameType.War)
                {
                    if (GamesService.WarInfo.ContainsKey(fileId))
                    {
                        await context.Channel.SendMessageAsync($"**{context.User.Username}**, a battle is currently ongoing!");
                        return;
                    }

                    weapon = GetItem<Weapon>(itemId);
                    if (weapon == null)
                    {
                        await context.Channel.SendMessageAsync($"**{context.User.Username}**, this is not a valid item!");
                    }

                    if (weapon.WarClass == WeaponClass.None || weapon.WarClass == WeaponClass.Artillery)
                    {
                        await context.Channel.SendMessageAsync($"**{context.User.Username}**, this item cannot be used as a weapon!");
                        return;
                    }

                    var user = GetUser(context);
                    if (!user.Items.ContainsKey(weapon.Id) || user.Items[weapon.Id] < 1)
                    {
                        await context.Channel.SendMessageAsync($"**{context.User.Username}**, you don't have this item!");
                        return;
                    }
                }
                using var marbleList = new StreamReader($"Data{Path.DirectorySeparatorChar}{fileId}{GameName(gameType, false)}.csv");
                if ((await marbleList.ReadToEndAsync()).Contains(context.User.Id.ToString()))
                {
                    await context.Channel.SendMessageAsync($"**{context.User.Username}**, you've already joined!");
                    return;
                }
            }

            if (string.IsNullOrEmpty(marbleName) || marbleName.StartsWith("<@")) marbleName = context.User.Username;
            else if (marbleName.Length > 100)
            {
                await context.Channel.SendMessageAsync($"**{context.User.Username}**, your entry exceeds the 100 character limit.");
                return;
            }
            else marbleName = marbleName.Replace('\n', ' ').Replace(',', ';');

            string bold = marbleName.Contains('*') || marbleName.Contains('\\') ? "" : "**";
            var builder = new EmbedBuilder()
                .WithColor(GetColor(context))
                .WithCurrentTimestamp()
                .AddField($"Marble {GameName(gameType)}: Signed up!",
                    $"**{context.User.Username}** has successfully signed up as {bold}{marbleName}{bold}{(gameType == GameType.War ? $" with the weapon **{weapon.Name}**" : "")}!");
            using (var racers = new StreamWriter($"Data{Path.DirectorySeparatorChar}{GameName(gameType)}MostUsed.txt", true))
                await racers.WriteLineAsync(marbleName);

            using (var marbleList = new StreamWriter(marbleListDirectory, true))
            {
                if (gameType == GameType.War) await marbleList.WriteLineAsync($"{marbleName},{context.User.Id},{itemId}");
                else await marbleList.WriteLineAsync($"{marbleName},{context.User.Id}");
            }

            int marbleNo;
            using (var marbleList = new StreamReader(marbleListDirectory, true))
                marbleNo = (await marbleList.ReadToEndAsync()).Split('\n').Length;
            await context.Channel.SendMessageAsync(embed: builder.Build());

            if (marbleNo > marbleLimit)
            {
                await context.Channel.SendMessageAsync($"The limit of {marbleLimit} contestants has been reached!");
                await startCommand();
            }
        }

        [Command("use", RunMode = RunMode.Async)]
        [Alias("useitem")]
        [Summary("Uses an item.")]
        public async Task UseCommand([Remainder] string searchTerm)
        {
            var item = GetItem<Weapon>(searchTerm);

            if (item == null)
            {
                await SendErrorAsync("Could not find the requested item!");
                return;
            }

            var obj = GetUsersObject();
            var user = GetUser(Context, obj);

            void UpdateUser(Item itm, int noOfItems)
            {
                if (user.Items.ContainsKey(itm.Id)) user.Items[itm.Id] += noOfItems;
                else user.Items.Add(itm.Id, noOfItems);
                user.NetWorth += item.Price * noOfItems;
                WriteUsers(obj, Context.User, user);
            }

            if (user.Items.ContainsKey(item.Id) && user.Items[item.Id] > 0)
            {
                if (item.WarClass != WeaponClass.None)
                {
                    ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                    if (!GamesService.SiegeInfo.ContainsKey(fileId))
                    {
                        await SendErrorAsync($"**{Context.User.Username}**, that item can't be used here!");
                        return;
                    }

                    await GamesService.SiegeInfo[fileId].WeaponAttack(Context, item);
                    return;
                }

                switch (item.Id)
                {
                    case 1:
                        {
                            ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                            if (GamesService.SiegeInfo.ContainsKey(fileId))
                            {
                                var output = new StringBuilder();
                                var userMarble = GamesService.SiegeInfo[fileId].Marbles.Find(m => m.Id == Context.User.Id);
                                foreach (var marble in GamesService.SiegeInfo[fileId].Marbles)
                                {
                                    marble.HP = marble.MaxHP;
                                    output.AppendLine($"**{marble.Name}** (HP: **{marble.HP}**/{marble.MaxHP}, DMG: **{marble.DamageDealt}**) [{Context.Client.GetUser(marble.Id).Username}#{Context.Client.GetUser(marble.Id).Discriminator}]");
                                }
                                await ReplyAsync(embed: new EmbedBuilder()
                                    .AddField("Marbles", output.ToString())
                                    .WithColor(GetColor(Context))
                                    .WithCurrentTimestamp()
                                    .WithDescription($"**{userMarble.Name}** used **{item.Name}**! Everyone was healed!")
                                    .WithTitle(item.Name)
                                    .Build());
                                UpdateUser(item, -1);
                            }
                            else await SendErrorAsync($"**{Context.User.Username}**, that item can't be used here!");
                            break;
                        }
                    case 10:
                        {
                            ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                            if (GamesService.SiegeInfo.ContainsKey(fileId))
                                await GamesService.SiegeInfo[fileId].ItemAttack(Context, obj, item.Id,
                                    (int)Math.Round(90 + GamesService.SiegeInfo[fileId].Boss.MaxHP * 0.05 * (Global.Rand.NextDouble() * 0.12 + 0.94)));
                            else await SendErrorAsync($"**{Context.User.Username}**, that item can't be used here!");
                            break;
                        }
                    case 14:
                        {
                            ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                            if (GamesService.SiegeInfo.ContainsKey(fileId))
                                await GamesService.SiegeInfo[fileId].ItemAttack(Context, obj, 14,
                                    70 + 10 * (int)GamesService.SiegeInfo[fileId].Boss.Difficulty, true);
                            else await SendErrorAsync($"**{Context.User.Username}**, that item can't be used here!");
                            break;
                        }
                    case 17:
                        await ReplyAsync("Er... why aren't you using `mb/craft`?");
                        break;
                    case 18:
                        if (GamesService.ScavengeInfo.ContainsKey(Context.User.Id))
                        {
                            UpdateUser(item, -1);
                            if (GamesService.ScavengeInfo[Context.User.Id].Location == ScavengeLocation.CanaryBeach)
                            {
                                UpdateUser(GetItem<Item>("019"), 1);
                                await ReplyAsync($"**{Context.User.Username}** dragged a **{item.Name}** across the water, turning it into a **Water Bucket**!");
                            }
                            else if (GamesService.ScavengeInfo[Context.User.Id].Location == ScavengeLocation.VioletVolcanoes)
                            {
                                UpdateUser(GetItem<Item>("020"), 1);
                                await ReplyAsync($"**{Context.User.Username}** dragged a **{item.Name}** across the lava, turning it into a **Lava Bucket**!");
                            }
                        }
                        else await SendErrorAsync($"**{Context.User.Username}**, that item can't be used here!");
                        break;
                    case 19:
                    case 20:
                        user.Items[item.Id]--;
                        if (user.Items.ContainsKey(19)) user.Items[18]++;
                        else user.Items.Add(18, 1);
                        await ReplyAsync($"**{Context.User.Username}** poured all the water out from a **{item.Name}**, turning it into a **Steel Bucket**!");
                        break;
                    case 22:
                        {
                            ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                            if (GamesService.SiegeInfo.ContainsKey(fileId) && string.Compare(GamesService.SiegeInfo[fileId].Boss.Name, "Help Me the Tree", true) == 0)
                            {
                                var randDish = 22 + Global.Rand.Next(0, 13);
                                UpdateUser(item, -1);
                                UpdateUser(GetItem<Item>(randDish.ToString("000")), 1);
                                await ReplyAsync($"**{Context.User.Username}** used their **{item.Name}**! It somehow picked up a disease and is now a **{GetItem<Item>(randDish.ToString("000")).Name}**!");
                            }
                            else await SendErrorAsync($"**{Context.User.Username}**, that item can't be used here!");
                            break;
                        }
                    case 23:
                    case 24:
                    case 25:
                    case 26:
                    case 27:
                    case 28:
                    case 29:
                    case 30:
                    case 31:
                    case 32:
                    case 33:
                    case 34:
                    case 35:
                        {
                            ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                            if (GamesService.SiegeInfo.ContainsKey(fileId))
                            {
                                var output = new StringBuilder();
                                var userMarble = GamesService.SiegeInfo[fileId].Marbles.Find(m => m.Id == Context.User.Id);
                                foreach (var marble in GamesService.SiegeInfo[fileId].Marbles)
                                    marble.StatusEffect = StatusEffect.Poison;
                                await ReplyAsync(embed: new EmbedBuilder()
                                    .WithColor(GetColor(Context))
                                    .WithCurrentTimestamp()
                                    .WithDescription($"**{userMarble.Name}** used **{item.Name}**! Everyone was poisoned!")
                                    .WithTitle(item.Name)
                                    .Build());
                                UpdateUser(item, -1);
                            }
                            else await SendErrorAsync($"**{Context.User.Username}**, that item can't be used here!");
                            break;
                        }
                    case 38:
                        {
                            ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                            if (GamesService.SiegeInfo.ContainsKey(fileId))
                            {
                                var userMarble = GamesService.SiegeInfo[fileId].Marbles.Find(m => m.Id == Context.User.Id);
                                foreach (var marble in GamesService.SiegeInfo[fileId].Marbles)
                                    marble.StatusEffect = StatusEffect.Doom;
                                await ReplyAsync(embed: new EmbedBuilder()
                                    .WithColor(GetColor(Context))
                                    .WithCurrentTimestamp()
                                    .WithDescription($"**{userMarble.Name}** used **{item.Name}**! Everyone is doomed!")
                                    .WithTitle(item.Name)
                                    .Build());
                                UpdateUser(item, -1);
                            }
                            else await SendErrorAsync($"**{Context.User.Username}**, that item can't be used here!");
                            break;
                        }
                    case 39:
                        {
                            ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                            if (GamesService.SiegeInfo.ContainsKey(fileId))
                            {
                                var userMarble = GamesService.SiegeInfo[fileId].Marbles.Find(m => m.Id == Context.User.Id);
                                userMarble.StatusEffect = StatusEffect.None;
                                await ReplyAsync(embed: new EmbedBuilder()
                                    .WithColor(GetColor(Context))
                                    .WithCurrentTimestamp()
                                    .WithDescription($"**{userMarble.Name}** used **{item.Name}** and is now cured!")
                                    .WithTitle(item.Name)
                                    .Build());
                                UpdateUser(item, -1);
                            }
                            else await SendErrorAsync($"**{Context.User.Username}**, that item can't be used here!");
                            break;
                        }
                    case 57:
                        {
                            ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                            if (GamesService.SiegeInfo.ContainsKey(fileId))
                            {
                                var userMarble = GamesService.SiegeInfo[fileId].Marbles.Find(m => m.Id == Context.User.Id);
                                userMarble.Evade = 50;
                                userMarble.BootsUsed = true;
                                await ReplyAsync(embed: new EmbedBuilder()
                                    .WithColor(GetColor(Context))
                                    .WithDescription($"**{userMarble.Name}** used **{item.Name}**, increasing their dodge chance to 50% for the next attack!")
                                    .WithTitle($"{item.Name}!")
                                    .Build());
                            }
                            break;
                        }
                    case 62: goto case 17;
                    case 91:
                        {
                            ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                            if (!GamesService.SiegeInfo.ContainsKey(fileId))
                            {
                                GamesService.SiegeInfo.GetOrAdd(fileId, new Siege(GamesService, Context, new List<SiegeMarble>())
                                {
                                    Active = false,
                                    Boss = Siege.GetBoss("Destroyer")
                                });
                                await ReplyAsync("*You hear the whirring of machinery...*");
                            }
                            else
                                await SendErrorAsync($"**{Context.User.Username}**, that item can't be used here!");
                            break;
                        }
                    default:
                        await SendErrorAsync($"**{Context.User.Username}**, that item can't be used here!");
                        break;
                }
            }
            else await ReplyAsync($"**{Context.User.Username}**, you don't have this item!");
        }
    }
}
