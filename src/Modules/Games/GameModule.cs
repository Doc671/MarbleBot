using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MarbleBot.Common;
using MarbleBot.Common.Games;
using MarbleBot.Common.Games.Scavenge;
using MarbleBot.Common.Games.Siege;
using MarbleBot.Extensions;
using MarbleBot.Modules.Games.Services;
using MarbleBot.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace MarbleBot.Modules.Games
{
    public class GameModule : MarbleBotModule
    {
        protected readonly BotCredentials _botCredentials;
        protected readonly GamesService _gamesService;
        protected readonly RandomService _randomService;

        protected GameModule(BotCredentials botCredentials, GamesService gamesService, RandomService randomService)
        {
            _botCredentials = botCredentials;
            _gamesService = gamesService;
            _randomService = randomService;
        }

        protected static string Bold(string stringToBold)
        {
            // Puts two asterisks on each side of the given string
            // Discord will display this as bold text
            // If there are already asterisks present, put a backslash in front of them so Discord will ignore
            var output = new StringBuilder();
            output.Append("**");
            foreach (char c in stringToBold)
            {
                output.Append(c switch
                {
                    '\\' => "",
                    '*' => "\\*",
                    _ => c
                });
            }

            output.Append("**");
            return output.ToString();
        }

        private static string GetGameName(GameType gameType, bool capitalised = true)
        {
            string name = gameType.ToString();
            return capitalised ? name : name.ToLower();
        }

        protected async Task Checkearn(GameType gameType)
        {
            var user = MarbleBotUser.Find(Context);
            DateTime lastWin = gameType switch
            {
                GameType.Race => user.LastRaceWin,
                GameType.Siege => user.LastSiegeWin,
                GameType.War => user.LastWarWin,
                _ => user.LastScavenge
            };
            TimeSpan nextEarn = DateTime.UtcNow - lastWin;

            string game = gameType switch
            {
                GameType.Race => "race",
                GameType.Scavenge => "scavenge",
                GameType.Siege => "siege",
                GameType.War => "war",
                _ => "none"
            };

            var output = nextEarn.TotalHours < 6
                ? $"You can earn money from {game} in {GetTimeSpanSentence(lastWin - DateTime.UtcNow.AddHours(-6))}!"
                : $"You can earn money from {game} now!";

            await ReplyAsync(embed: new EmbedBuilder()
                .WithAuthor(Context.User)
                .WithColor(GetColor(Context))
                .WithDescription(output)
                .Build());
        }

        protected async Task Clear(GameType gameType)
        {
            ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
            if (_botCredentials.AdminIds.Any(id => id == Context.User.Id) || Context.IsPrivate)
            {
                await using var marbleList =
                    new StreamWriter($"Data{Path.DirectorySeparatorChar}{fileId}.{GetGameName(gameType, false)}",
                        false);
                await marbleList.WriteAsync("");
                await ReplyAsync("Contestant list successfully cleared!");
            }
            else
            {
                await ReplyAsync($"**{Context.User.Username}**, you cannot do this!");
            }
        }

        protected static string Leaderboard(IEnumerable<(string elementName, int value)> orderedData, int pageNo)
        {
            var dataList = new List<(int place, string elementName, int value)>();
            int displayedPlace = 0, lastValue = 0;
            foreach ((string elementName, int value) in orderedData)
            {
                if (value != lastValue)
                {
                    displayedPlace++;
                }

                dataList.Add((displayedPlace, elementName, value));
                lastValue = value;
            }

            const int pageSize = 10;
            if (pageNo > dataList.Last().place / pageSize)
            {
                return $"There are no entries in page **{pageNo}**!";
            }

            // Displays in groups of ten (e.g. if pageNo is 1, first 10 displayed)
            int minValue = (pageNo - 1) * pageSize + 1;
            int maxValue = pageNo * pageSize;
            var output = new StringBuilder();
            foreach ((int place, string elementName, int value) in dataList)
            {
                if (place > maxValue)
                {
                    break;
                }

                if (place < maxValue + 1 && place >= minValue)
                {
                    output.AppendLine($"{place}{place.Ordinal()}: {elementName} {value}");
                }
            }

            return output.ToString();
        }

        protected async Task RemoveContestant(GameType gameType, string marbleToRemove)
        {
            ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
            string marbleListDirectory = $"Data{Path.DirectorySeparatorChar}{fileId}.{GetGameName(gameType, false)}";
            if (!File.Exists(marbleListDirectory))
            {
                await SendErrorAsync($"**{Context.User.Username}**, no-one is signed up!");
                return;
            }

            const int notFound = 0;
            const int foundNotOwner = 1;
            const int foundRemoved = 2;

            int state = _botCredentials.AdminIds.Any(id => id == Context.User.Id) ? 3 : notFound;
            var formatter = new BinaryFormatter();
            if (gameType == GameType.War)
            {
                List<(ulong id, string name, int itemId)> marbles;
                using (var marbleListFile = new StreamReader(marbleListDirectory))
                {
                    if (marbleListFile.BaseStream.Length == 0)
                    {
                        await SendErrorAsync($"**{Context.User.Username}**, no-one is signed up!");
                        return;
                    }

                    marbles =
                        (List<(ulong id, string name, int itemId)>)formatter.Deserialize(marbleListFile.BaseStream);
                }

                (ulong id, string name, int itemId)? marble = marbles.Find(info =>
                    string.Compare(marbleToRemove, info.name, StringComparison.OrdinalIgnoreCase) == 0)!;

                if (marble != null)
                {
                    if (marble.Value.id == Context.User.Id)
                    {
                        state = foundRemoved;
                        marbles.Remove(marble.Value);
                    }
                    else
                    {
                        state = foundNotOwner;
                    }
                }

                await using (var marbleListFile = new StreamWriter(marbleListDirectory))
                {
                    if (marbleListFile.BaseStream.Length == 0)
                    {
                        await SendErrorAsync($"**{Context.User.Username}**, no-one is signed up!");
                        return;
                    }

                    formatter.Serialize(marbleListFile.BaseStream, marbles);
                }
            }
            else
            {
                List<(ulong id, string name)> marbles;
                using (var marbleListFile = new StreamReader(marbleListDirectory))
                {
                    marbles = (List<(ulong id, string name)>)formatter.Deserialize(marbleListFile.BaseStream);
                }

                (ulong id, string name)? marble = marbles.Find(info =>
                    string.Compare(marbleToRemove, info.name, StringComparison.OrdinalIgnoreCase) == 0);

                if (marble != null)
                {
                    if (marble.Value.id == Context.User.Id)
                    {
                        state = foundRemoved;
                        marbles.Remove(marble.Value);
                    }
                    else
                    {
                        state = foundNotOwner;
                    }
                }

                await using (var marbleListFile = new StreamWriter(marbleListDirectory))
                {
                    formatter.Serialize(marbleListFile.BaseStream, marbles);
                }
            }

            switch (state)
            {
                case notFound:
                    await ReplyAsync($"**{Context.User.Username}**, could not find the requested marble!");
                    break;
                case foundNotOwner:
                    await ReplyAsync($"**{Context.User.Username}**, this is not your marble!");
                    break;
                case foundRemoved:
                    await ReplyAsync($"**{Context.User.Username}**, removed contestant {Bold(marbleToRemove)}!");
                    break;
            }
        }

        protected async Task ShowContestants(GameType gameType)
        {
            ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
            string marbleListDirectory = $"Data{Path.DirectorySeparatorChar}{fileId}.{GetGameName(gameType, false)}";
            if (!File.Exists(marbleListDirectory))
            {
                await SendErrorAsync($"**{Context.User.Username}**, no-one is signed up!");
                return;
            }

            var marbleOutput = new StringBuilder();
            int count;
            using (var marbleListFile = new StreamReader(marbleListDirectory))
            {
                if (marbleListFile.BaseStream.Length == 0)
                {
                    await SendErrorAsync($"**{Context.User.Username}**, no-one is signed up!");
                    return;
                }

                var formatter = new BinaryFormatter();
                SocketUser user;
                if (gameType == GameType.War)
                {
                    var marbles = (List<(ulong id, string name, int itemId)>)formatter.Deserialize(marbleListFile.BaseStream);
                    count = marbles.Count;
                    if (count == 0)
                    {
                        await SendErrorAsync($"**{Context.User.Username}**, no-one is signed up!");
                        return;
                    }

                    foreach ((ulong id, string name, int itemId) in marbles)
                    {
                        user = Context.Client.GetUser(id);
                        marbleOutput.AppendLine(user == null
                            ? $"{Bold(name)} (Weapon: **{Item.Find<Item>(itemId.ToString()).Name}**) [user not found]"
                            : $"{Bold(name)} (Weapon: **{Item.Find<Item>(itemId.ToString()).Name}**) [{user.Username}#{user.Discriminator}]");
                    }
                }
                else
                {
                    var marbles = (List<(ulong id, string name)>)formatter.Deserialize(marbleListFile.BaseStream);
                    count = marbles.Count;
                    if (count == 0)
                    {
                        await SendErrorAsync($"**{Context.User.Username}**, no-one is signed up!");
                        return;
                    }

                    foreach ((ulong id, string name) in marbles)
                    {
                        user = Context.Client.GetUser(id);
                        marbleOutput.AppendLine(user == null
                            ? $"{Bold(name)} [user not found]"
                            : $"{Bold(name)} [{user.Username}#{user.Discriminator}]");
                    }
                }
            }

            await ReplyAsync(embed: new EmbedBuilder()
                .WithColor(GetColor(Context))
                .WithFooter($"Contestant count: {count}")
                .WithTitle($"Marble {GetGameName(gameType)}: Contestants")
                .AddField("Contestants", marbleOutput.ToString())
                .Build());
        }

        protected async Task Signup(GameType gameType, string marbleName, int marbleLimit,
            Func<Task> startCommand, Weapon? weapon = null)
        {
            ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
            string marbleListFilePath = $"Data{Path.DirectorySeparatorChar}{fileId}.{GetGameName(gameType, false)}";
            if (!File.Exists(marbleListFilePath))
            {
                File.Create(marbleListFilePath).Close();
            }

            var binaryFormatter = new BinaryFormatter();
            if (gameType == GameType.Siege)
            {
                if (_gamesService.Sieges.ContainsKey(fileId) && _gamesService.Sieges[fileId].Active)
                {
                    await ReplyAsync($"**{Context.User.Username}**, a battle is currently ongoing!");
                    return;
                }

                using var marbleList = new StreamReader(marbleListFilePath);
                if (marbleList.BaseStream.Length != 0 &&
                    ((List<(ulong id, string name)>)binaryFormatter.Deserialize(marbleList.BaseStream)).Any(info =>
                        info.id == Context.User.Id))
                {
                    await ReplyAsync($"**{Context.User.Username}**, you've already joined!");
                    return;
                }
            }
            else if (gameType == GameType.War)
            {
                if (_gamesService.Wars.ContainsKey(fileId))
                {
                    await ReplyAsync($"**{Context.User.Username}**, a battle is currently ongoing!");
                    return;
                }

                if (weapon!.WeaponClass == WeaponClass.None || weapon.WeaponClass == WeaponClass.Artillery)
                {
                    await ReplyAsync($"**{Context.User.Username}**, this item cannot be used as a weapon!");
                    return;
                }

                var user = MarbleBotUser.Find(Context);
                if (!user.Items.ContainsKey(weapon.Id) || user.Items[weapon.Id] < 1)
                {
                    await ReplyAsync($"**{Context.User.Username}**, you don't have this item!");
                    return;
                }

                using var marbleList = new StreamReader(marbleListFilePath);
                if (marbleList.BaseStream.Length != 0 &&
                    ((List<(ulong id, string name, int itemId)>)binaryFormatter.Deserialize(marbleList.BaseStream))
                    .Any(info => info.id == Context.User.Id))
                {
                    await ReplyAsync($"**{Context.User.Username}**, you've already joined!");
                    return;
                }
            }

            if (string.IsNullOrEmpty(marbleName) || marbleName.StartsWith("<@"))
            {
                marbleName = Context.User.Username;
            }
            else if (marbleName.Length > 100)
            {
                await ReplyAsync($"**{Context.User.Username}**, your entry exceeds the 100 character limit.");
                return;
            }

            var builder = new EmbedBuilder()
                .WithColor(GetColor(Context))
                .AddField($"Marble {GetGameName(gameType)}: Signed up!",
                    $"**{Context.User.Username}** has successfully signed up as {Bold(marbleName)}{(gameType == GameType.War ? $" with the weapon **{weapon!.Name}**" : "")}!");

            await using (var mostUsedFile =
                new StreamWriter($"Data{Path.DirectorySeparatorChar}{GetGameName(gameType)}MostUsed.txt", true))
            {
                await mostUsedFile.WriteLineAsync(marbleName);
            }

            int marbleNo;
            if (gameType == GameType.War)
            {
                var marbles = new List<(ulong id, string name, int itemId)>();
                using (var marbleFile = new StreamReader(marbleListFilePath))
                {
                    if (marbleFile.BaseStream.Length == 0)
                    {
                        marbleNo = 0;
                    }
                    else
                    {
                        marbles = (List<(ulong id, string name, int itemId)>)binaryFormatter.Deserialize(marbleFile.BaseStream);
                        marbleNo = marbles.Count;
                    }
                }

                marbles.Add((Context.User.Id, marbleName, weapon!.Id));

                await using (var marbleFile = new StreamWriter(marbleListFilePath))
                {
                    binaryFormatter.Serialize(marbleFile.BaseStream, marbles);
                }
            }
            else
            {
                var marbles = new List<(ulong id, string name)>();
                using (var marbleFile = new StreamReader(marbleListFilePath))
                {
                    if (marbleFile.BaseStream.Length == 0)
                    {
                        marbleNo = 0;
                    }
                    else
                    {
                        marbles = (List<(ulong id, string name)>)binaryFormatter.Deserialize(marbleFile.BaseStream);
                        marbleNo = marbles.Count + 1;
                    }
                }

                marbles.Add((Context.User.Id, marbleName));

                await using (var marbleFile = new StreamWriter(marbleListFilePath))
                {
                    binaryFormatter.Serialize(marbleFile.BaseStream, marbles);
                }
            }

            await ReplyAsync(embed: builder.Build());

            if (marbleNo >= marbleLimit)
            {
                await ReplyAsync($"The limit of {marbleLimit} contestants has been reached!");
                await startCommand();
            }
        }

        [Command("use", RunMode = RunMode.Async)]
        [Alias("useitem")]
        [Summary("Uses an item.")]
        public async Task UseCommand([Remainder] string searchTerm)
        {
            var item = Item.Find<Item>(searchTerm);
            var user = MarbleBotUser.Find(Context);
            if (!user.Items.ContainsKey(item.Id) || user.Items[item.Id] == 0)
            {
                await ReplyAsync($"**{Context.User.Username}**, you don't have this item!");
                return;
            }

            void UpdateUser(Item itm, int noOfItems)
            {
                if (user.Items.ContainsKey(itm.Id))
                {
                    user.Items[itm.Id] += noOfItems;
                }
                else
                {
                    user.Items.Add(itm.Id, noOfItems);
                }

                user.NetWorth += item.Price * noOfItems;
                MarbleBotUser.UpdateUser(user);
            }

            if (item is Weapon weapon)
            {
                ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                if (!_gamesService.Sieges.ContainsKey(fileId))
                {
                    await SendErrorAsync($"**{Context.User.Username}**, that item can't be used here!");
                    return;
                }

                await _gamesService.Sieges[fileId].WeaponAttack(weapon);
                return;
            }

            switch (item.Id)
            {
                case 1:
                    {
                        ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                        if (_gamesService.Sieges.ContainsKey(fileId))
                        {
                            var output = new StringBuilder();
                            var userMarble = _gamesService.Sieges[fileId].Marbles.Find(m => m.Id == Context.User.Id)!;
                            foreach (var marble in _gamesService.Sieges[fileId].Marbles)
                            {
                                marble.Health = marble.MaxHealth;
                                output.AppendLine($"**{marble.Name}** (Health: **{marble.Health}**/{marble.MaxHealth}, DMG: **{marble.DamageDealt}**) [{Context.Client.GetUser(marble.Id).Username}#{Context.Client.GetUser(marble.Id).Discriminator}]");
                            }

                            await ReplyAsync(embed: new EmbedBuilder()
                                .AddField("Marbles", output.ToString())
                                .WithColor(GetColor(Context))
                                .WithDescription($"**{userMarble.Name}** used **{item.Name}**! Everyone was healed!")
                                .WithTitle(item.Name)
                                .Build());
                            UpdateUser(item, -1);
                        }
                        else
                        {
                            await SendErrorAsync($"**{Context.User.Username}**, that item can't be used here!");
                        }

                        break;
                    }
                case 10:
                    {
                        ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                        if (_gamesService.Sieges.TryGetValue(fileId, out Siege? siege))
                        {
                            await siege.ItemAttack(item.Id,
                                (int)Math.Round(90 + siege.Boss!.MaxHealth * 0.05 * _randomService.Rand.NextDouble() * 0.12 + 0.94));
                        }
                        else
                        {
                            await SendErrorAsync($"**{Context.User.Username}**, that item can't be used here!");
                        }

                        break;
                    }
                case 14:
                    {
                        ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                        if (_gamesService.Sieges.TryGetValue(fileId, out Siege? siege))
                        {
                            await siege.ItemAttack(item.Id,
                                70 + 10 * (int)siege.Boss!.Difficulty,
                                true);
                        }
                        else
                        {
                            await SendErrorAsync($"**{Context.User.Username}**, that item can't be used here!");
                        }

                        break;
                    }
                case 62:
                case 17:
                    await ReplyAsync("Er... why aren't you using `mb/craft`?");
                    break;
                case 18:
                    if (_gamesService.Scavenges.ContainsKey(Context.User.Id))
                    {
                        UpdateUser(item, -1);
                        switch (_gamesService.Scavenges[Context.User.Id].Location)
                        {
                            case ScavengeLocation.CanaryBeach:
                                UpdateUser(Item.Find<Item>("019"), 1);
                                await ReplyAsync($"**{Context.User.Username}** dragged a **{item.Name}** across the water, turning it into a **Water Bucket**!");
                                break;
                            case ScavengeLocation.VioletVolcanoes:
                                UpdateUser(Item.Find<Item>("020"), 1);
                                await ReplyAsync($"**{Context.User.Username}** dragged a **{item.Name}** across the lava, turning it into a **Lava Bucket**!");
                                break;
                            default:
                                await SendErrorAsync($"**{Context.User.Username}**, that item can't be used here!");
                                break;
                        }
                    }
                    else
                    {
                        await SendErrorAsync($"**{Context.User.Username}**, that item can't be used here!");
                    }

                    break;
                case 19:
                case 20:
                    user.Items[item.Id]--;
                    if (user.Items.ContainsKey(19))
                    {
                        user.Items[18]++;
                    }
                    else
                    {
                        user.Items.Add(18, 1);
                    }

                    await ReplyAsync($"**{Context.User.Username}** poured all the water out from a **{item.Name}**, turning it into a **Steel Bucket**!");
                    break;
                case 22:
                    {
                        ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                        if (_gamesService.Sieges.TryGetValue(fileId, out Siege? siege) &&
                            siege.Boss!.Name == "Help Me the Tree")
                        {
                            int randDish = 22 + _randomService.Rand.Next(0, 13);
                            UpdateUser(item, -1);
                            UpdateUser(Item.Find<Item>(randDish.ToString("000")), 1);
                            await ReplyAsync($"**{Context.User.Username}** used their **{item.Name}**! It somehow picked up a disease and is now a **{Item.Find<Item>(randDish.ToString("000")).Name}**!");
                        }
                        else
                        {
                            await SendErrorAsync($"**{Context.User.Username}**, that item can't be used here!");
                        }

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
                        if (_gamesService.Sieges.TryGetValue(fileId, out Siege? siege))
                        {
                            var userMarble = siege.Marbles.Find(m => m.Id == Context.User.Id)!;
                            foreach (var marble in siege.Marbles)
                            {
                                marble.StatusEffect = StatusEffect.Poison;
                            }

                            await ReplyAsync(embed: new EmbedBuilder()
                                .WithColor(GetColor(Context))
                                .WithDescription($"**{userMarble.Name}** used **{item.Name}**! Everyone was poisoned!")
                                .WithTitle(item.Name)
                                .Build());
                            UpdateUser(item, -1);
                        }
                        else
                        {
                            await SendErrorAsync($"**{Context.User.Username}**, that item can't be used here!");
                        }

                        break;
                    }
                case 38:
                    {
                        ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                        if (_gamesService.Sieges.TryGetValue(fileId, out Siege? siege))
                        {
                            var userMarble = siege.Marbles.Find(m => m.Id == Context.User.Id)!;
                            foreach (var marble in siege.Marbles)
                            {
                                marble.StatusEffect = StatusEffect.Doom;
                            }

                            await ReplyAsync(embed: new EmbedBuilder()
                                .WithColor(GetColor(Context))
                                .WithDescription($"**{userMarble.Name}** used **{item.Name}**! Everyone is doomed!")
                                .WithTitle(item.Name)
                                .Build());
                            UpdateUser(item, -1);
                        }
                        else
                        {
                            await SendErrorAsync($"**{Context.User.Username}**, that item can't be used here!");
                        }

                        break;
                    }
                case 39:
                    {
                        ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                        if (_gamesService.Sieges.TryGetValue(fileId, out Siege? siege))
                        {
                            var userMarble = siege.Marbles.Find(m => m.Id == Context.User.Id)!;
                            userMarble.StatusEffect = StatusEffect.None;
                            await ReplyAsync(embed: new EmbedBuilder()
                                .WithColor(GetColor(Context))
                                .WithDescription($"**{userMarble.Name}** used **{item.Name}** and is now cured!")
                                .WithTitle(item.Name)
                                .Build());
                            UpdateUser(item, -1);
                        }
                        else
                        {
                            await SendErrorAsync($"**{Context.User.Username}**, that item can't be used here!");
                        }

                        break;
                    }
                case 57:
                    {
                        ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                        if (_gamesService.Sieges.TryGetValue(fileId, out Siege? siege))
                        {
                            var userMarble = siege.Marbles.Find(m => m.Id == Context.User.Id)!;
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
                case 91:
                    {
                        ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                        if (!_gamesService.Sieges.ContainsKey(fileId))
                        {
                            _gamesService.Sieges.GetOrAdd(fileId,
                                new Siege(Context, _gamesService, _randomService, Boss.GetBoss("Destroyer"),
                                    new List<SiegeMarble>()));
                            await ReplyAsync("*You hear the whirring of machinery...*");
                        }
                        else
                        {
                            await SendErrorAsync($"**{Context.User.Username}**, that item can't be used here!");
                        }

                        break;
                    }
                default:
                    await SendErrorAsync($"**{Context.User.Username}**, that item can't be used here!");
                    break;
            }
        }
    }
}
