using Discord;
using Discord.Commands;
using MarbleBot.Core;
using MarbleBot.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static MarbleBot.Global;

namespace MarbleBot.Modules
{
    /// <summary> Commands related to currency. </summary>
    public class Economy : MarbleBotModule
    {
        [Command("balance")]
        [Alias("credits", "money", "bal")]
        [Summary("Returns how much money you or someone else has.")]
        public async Task BalanceCommand([Remainder] string searchTerm = "")
        {
            var user = new MarbleBotUser();
            var id = Context.User.Id;
            if (string.IsNullOrEmpty(searchTerm)) user = GetUser(Context);
            else
            {
                string json;
                using (var users = new StreamReader($"Data{Path.DirectorySeparatorChar}Users.json")) json = users.ReadToEnd();
                var rawUsers = JsonConvert.DeserializeObject<Dictionary<string, MarbleBotUser>>(json);
                var foundUser = rawUsers.Where(usr => searchTerm.ToLower().Contains(usr.Value.Name.ToLower())
                || usr.Value.Name.ToLower().Contains(searchTerm.ToLower())
                || searchTerm.ToLower().Contains(usr.Value.Discriminator)).LastOrDefault();

                if (foundUser.Value == null)
                {
                    await SendErrorAsync($"**{Context.User.Username}**, the requested user could not be found.");
                    return;
                }

                id = ulong.Parse(foundUser.Key);
                user = foundUser.Value;
            }
            var author = Context.Client.GetUser(id);
            var builder = new EmbedBuilder()
                .WithAuthor(author)
                .WithCurrentTimestamp()
                .WithColor(GetColor(Context))
                .AddField("Balance", $"{UoM}{user.Balance:n2}", true)
                .AddField("Net Worth", $"{UoM}{user.NetWorth:n2}", true);
            await ReplyAsync(embed: builder.Build());
        }

        [Command("buy")]
        [Alias("buyitem")]
        [Summary("Buys items.")]
        public async Task BuyCommand(string rawId, string rawNoOfItems = "1")
        {
            if (!int.TryParse(rawNoOfItems, out int noOfItems) && noOfItems > 0)
            {
                await SendErrorAsync($"**{Context.User.Username}**, invalid item ID and/or number of items! Use `mb/help buy` to see how the command works.");
                return;
            }

            var item = GetItem<Item>(rawId);
            if (item == null)
            {
                await SendErrorAsync($"**{Context.User.Username}**, invalid item ID and/or number of items! Use `mb/help buy` to see how the command works.");
                return;
            }

            if (item.Price == -1)
            {
                await SendErrorAsync($"**{Context.User.Username}**, this item cannot be sold!");
                return;
            }

            if (!item.OnSale)
            {
                await SendErrorAsync($"**{Context.User.Username}**, this item is not on sale!");
                return;
            }

            var obj = GetUsersObject();
            var user = GetUser(Context, obj);
            if (user.Balance >= item.Price * noOfItems)
            {
                if (user.Items != null)
                {
                    if (user.Items.ContainsKey(item.Id)) user.Items[item.Id] += noOfItems;
                    else user.Items.Add(item.Id, noOfItems);
                }
                else
                {
                    user.Items = new SortedDictionary<uint, int> {
                        { item.Id, noOfItems }
                    };
                }
                user.Balance -= item.Price * noOfItems;
                WriteUsers(obj, Context.User, user);
                await ReplyAsync($"**{user.Name}** has successfully purchased **{item.Name}** x**{noOfItems}** for {UoM}**{item.Price:n2}** each!\nTotal price: {UoM}**{item.Price * noOfItems:n2}**\nNew balance: {UoM}**{user.Balance:n2}**.");
            }
            else await SendErrorAsync($"You can't afford this!");
        }

        [Command("craft")]
        [Summary("Crafts an item out of other items.")]
        public async Task CraftCommand(string searchTerm, string rawNoOfItems = "1")
        {
            var obj = GetUsersObject();
            var user = GetUser(Context, obj);
            if (!user.Items.ContainsKey(17) && !user.Items.ContainsKey(62))
            {
                await SendErrorAsync($"**{Context.User.Username}**, you need a Crafting Station to craft items!");
                return;
            }

            if (!int.TryParse(rawNoOfItems, out int noOfItems))
                searchTerm += rawNoOfItems;

            var requestedItem = GetItem<Item>(searchTerm);
            if (requestedItem.CraftingStationRequired == 2 && !user.Items.ContainsKey(62))
            {
                await SendErrorAsync($"**{Context.User.Username}**, your current Crafting Station cannot craft this item!");
                return;
            }

            if (requestedItem.CraftingRecipe.Count < 1)
            {
                await SendErrorAsync($"**{Context.User.Username}**, the item **{requestedItem.Name}** cannot be crafted!");
                return;
            }

            var sufficientMaterials = true;
            foreach (var item in requestedItem.CraftingRecipe)
            {
                if (!user.Items.ContainsKey(uint.Parse(item.Key)) || item.Value * noOfItems > user.Items[uint.Parse(item.Key)])
                {
                    sufficientMaterials = false;
                    break;
                }
            }

            if (!sufficientMaterials)
            {
                await SendErrorAsync($"**{Context.User.Username}**, you do not have enough items to craft this!");
                return;
            }

            var noCrafted = (int)requestedItem.CraftingProduced * noOfItems;
            var embed = new EmbedBuilder()
                    .WithCurrentTimestamp()
                    .WithColor(GetColor(Context))
                    .WithDescription($"**{Context.User.Username}** has successfully crafted **{requestedItem.Name}** x**{noCrafted}**!")
                    .WithTitle("Crafting: " + requestedItem.Name);
            var output = new StringBuilder();
            var currentNetWorth = user.NetWorth;
            foreach (var rawItem in requestedItem.CraftingRecipe)
            {
                var item = GetItem<Item>(rawItem.Key);
                var noLost = rawItem.Value * noOfItems;
                output.AppendLine($"`[{item.Id:000}]` {item.Name}: {noLost}");
                user.Items[uint.Parse(rawItem.Key)] -= noLost;
                user.NetWorth -= item.Price * noOfItems;
            }
            if (!user.Items.ContainsKey(requestedItem.Id)) user.Items.Add(requestedItem.Id, noCrafted);
            else user.Items[requestedItem.Id] += noCrafted;
            user.NetWorth += requestedItem.Price * noCrafted;
            embed.AddField("Lost items", output.ToString())
                .AddField("Net Worth", $"Old: {UoM}**{currentNetWorth:n2}**\nNew: {UoM}**{user.NetWorth:n2}**");
            WriteUsers(obj, Context.User, user);
            await ReplyAsync(embed: embed.Build());
        }

        [Command("craftable")]
        [Summary("Returns a list of all items that can be crafted with the user's inventory.")]
        public async Task CraftableCommand()
        {
            var user = GetUser(Context);
            var output = new StringBuilder();
            var items = GetItemsObject().ToObject<Dictionary<string, Item>>();
            foreach (var itemPair in items)
            {
                if (itemPair.Value.CraftingProduced != 0)
                {
                    var craftable = true;
                    var noCraftable = 0;
                    foreach (var ingredient in itemPair.Value.CraftingRecipe)
                    {
                        var id = uint.Parse(ingredient.Key);
                        if (!user.Items.ContainsKey(id) || user.Items[id] < ingredient.Value)
                        {
                            craftable = false;
                            break;
                        }
                        noCraftable = noCraftable == 0 ? user.Items[id] / ingredient.Value : Math.Min(user.Items[id] / ingredient.Value, noCraftable);
                    }
                    if (craftable) output.AppendLine($"`[{itemPair.Key}]` {itemPair.Value.Name}: {noCraftable}");
                }
            }
            if (output.Length < 1) output.Append("There are no items you can craft!");
            await ReplyAsync(embed: new EmbedBuilder()
                .WithAuthor(Context.User)
                .WithColor(GetColor(Context))
                .WithCurrentTimestamp()
                .WithDescription(output.ToString())
                .WithTitle("Craftable items")
                .Build());
        }

        [Command("daily")]
        [Summary("Gives daily Units of Money (200 to the power of (your streak / 100 - 1)).")]
        public async Task DailyCommand()
        {
            var obj = GetUsersObject();
            var user = GetUser(Context, obj);
            if (DateTime.UtcNow.Subtract(user.LastDaily).TotalHours > 24)
            {
                if (DateTime.UtcNow.Subtract(user.LastDaily).TotalHours > DailyTimeout) user.DailyStreak = 0;
                decimal gift;
                var power = user.DailyStreak > 100 ? 100 : user.DailyStreak;
                gift = Convert.ToDecimal(Math.Round(Math.Pow(200, 1 + (Convert.ToDouble(power) / 100)), 2));
                user.Balance += gift;
                user.NetWorth += gift;
                user.DailyStreak++;
                user.LastDaily = DateTime.UtcNow;
                var craftingStation = user.DailyStreak > 2 && !user.Items.ContainsKey(17);
                if (craftingStation) user.Items.Add(17, 1);
                var orange = false;
                if (!user.Items.ContainsKey(10) && DateTime.UtcNow.DayOfYear < 51 && DateTime.UtcNow.DayOfYear > 42)
                {
                    orange = true;
                    user.Items.Add(10, 1);
                }
                WriteUsers(obj, Context.User, user);
                await ReplyAsync($"**{Context.User.Username}**, you have received {UoM}**{gift:n2}**!\n(Streak: **{user.DailyStreak}**)");
                if (craftingStation) await ReplyAsync("You have been given a **Crafting Station Mk.I**!");
                if (orange) await ReplyAsync("You have been given a **Qefpedun Charm**!");
            }
            else
            {
                var ADayAgo = DateTime.UtcNow.AddDays(-1);
                await ReplyAsync($"You need to wait for **{GetDateString(user.LastDaily.Subtract(ADayAgo))}** until you can get your daily gift again!");
            }
        }

        [Command("dismantle")]
        [Alias("decraft", "disassemble", "dismantle")]
        [Summary("Turns a crafted item back into its ingredients.")]
        public async Task DecraftCommand(string searchTerm, string rawNoOfItems = "1")
        {
            var obj = GetUsersObject();
            var user = GetUser(Context, obj);
            if (!user.Items.ContainsKey(17) && !user.Items.ContainsKey(62))
            {
                await SendErrorAsync($"**{Context.User.Username}**, you need a Crafting Station to decraft items!");
                return;
            }

            if (!int.TryParse(rawNoOfItems, out int noOfItems))
                searchTerm += rawNoOfItems;

            var requestedItem = GetItem<Item>(searchTerm);
            if (requestedItem == null)
            {
                await SendErrorAsync($"**{Context.User.Username}**, invalid item ID!");
            }

            if (requestedItem.CraftingProduced == 0)
            {
                await SendErrorAsync($"**{Context.User.Username}**, you cannot dismantle this item!");
                return;
            }

            if (requestedItem.CraftingStationRequired == 2 && !user.Items.ContainsKey(62))
            {
                await SendErrorAsync($"**{Context.User.Username}**, your Crafting Station is not advanced enough to dismantle this item!");
                return;
            }

            if (requestedItem.CraftingRecipe.Count < 1)
            {
                await SendErrorAsync($"**{Context.User.Username}**, the item **{requestedItem.Name}** cannot be decrafted!");
                return;
            }

            if (user.Items.ContainsKey(requestedItem.Id) && user.Items[requestedItem.Id] >= noOfItems)
            {
                var noCrafted = (int)requestedItem.CraftingProduced * noOfItems;
                var embed = new EmbedBuilder()
                        .WithCurrentTimestamp()
                        .WithColor(GetColor(Context))
                        .WithDescription($"**{Context.User.Username}** has successfully decrafted **{requestedItem.Name}** x**{noCrafted}**!")
                        .WithTitle("Crafting: " + requestedItem.Name);
                var output = new StringBuilder();
                var currentNetWorth = user.NetWorth;
                foreach (var rawItem in requestedItem.CraftingRecipe)
                {
                    var item = GetItem<Item>(rawItem.Key);
                    var noGained = rawItem.Value * noOfItems;
                    output.AppendLine($"`[{item.Id:000}]` {item.Name}: {noGained}");
                    if (user.Items.ContainsKey(item.Id)) user.Items[uint.Parse(rawItem.Key)] += noGained;
                    else user.Items.Add(item.Id, noGained);
                    user.NetWorth += item.Price * noOfItems;
                }
                user.Items[requestedItem.Id] -= noCrafted;
                user.NetWorth -= requestedItem.Price * noCrafted;
                embed.AddField("Gained items", output.ToString())
                    .AddField("Net Worth", $"Old: {UoM}**{currentNetWorth:n2}**\nNew: {UoM}**{user.NetWorth:n2}**");
                WriteUsers(obj, Context.User, user);
                await ReplyAsync(embed: embed.Build());
            }
            else await SendErrorAsync($"**{Context.User.Username}**, you do not have enough of this item!");
        }

        [Command("inventory")]
        [Alias("inv", "items")]
        [Summary("Shows all the items a user has.")]
        public async Task InventoryCommand([Remainder] string rawSearchTerm = "")
        {
            var searchTermParts = rawSearchTerm.Length == 0 ? Array.Empty<string>(): rawSearchTerm.Split(' ');
            int page = 1;
            var name = new StringBuilder();

            for (int i = 0; i < searchTermParts.Length; i++)
            {
                string part = searchTermParts[i];
                if (!int.TryParse(part, out page))
                    name.Append($"{part}{(i == searchTermParts.Length - 2 ? "" : " ")}");
            }

            page = page == 0 ? 1 : page;

            if (page < 1)
            {
                await ReplyAsync($"**{Context.User.Username}**, the inventory page must be at least one!");
                return;
            }

            ulong id = Context.User.Id;
            var searchTerm = name.ToString();
            var user = new MarbleBotUser();
            if (string.IsNullOrEmpty(searchTerm))
            {
                user = GetUser(Context);
                if (user.Items == null)
                {
                    await SendErrorAsync($"**{Context.User.Username}**, you don't have any items!");
                    return;
                }
            }
            else
            {
                string json;
                using (var users = new StreamReader($"Data{Path.DirectorySeparatorChar}Users.json")) json = users.ReadToEnd();
                var rawUsers = JsonConvert.DeserializeObject<Dictionary<string, MarbleBotUser>>(json);
                var foundUser = rawUsers.Where(usr => searchTerm.ToLower().Contains(usr.Value.Name.ToLower())
                || usr.Value.Name.ToLower().Contains(searchTerm.ToLower())
                || searchTerm.ToLower().Contains(usr.Value.Discriminator)).LastOrDefault();

                if (foundUser.Value == null)
                {
                    await SendErrorAsync($"**{Context.User.Username}**, the requested user could not be found.");
                    return;
                }

                else if (foundUser.Value.Items == null)
                {
                    await SendErrorAsync($"**{Context.User.Username}**, the user **{foundUser.Value.Name}** does not have any items!");
                    return;
                }
                id = ulong.Parse(foundUser.Key);
                user = foundUser.Value;
            }
            var itemOutput = new StringBuilder();
            var items = user.Items.Skip((page - 1) * 20).Take(20);
            var itemsPresent = items.Count() > 0;
            if (itemsPresent)
            {
                foreach (var itemPair in items)
                {
                    if (itemPair.Value > 0)
                    {
                        itemOutput.AppendLine($"`[{itemPair.Key:000}]` {GetItem<Item>(itemPair.Key.ToString("000")).Name}: {itemPair.Value}");
                    }
                }
            }
            else itemOutput.Append($"**{Context.User.Username}**, there are no items on page **{page}**!");
            await base.ReplyAsync(embed: new EmbedBuilder()
                .WithAuthor(Context.Client.GetUser(id))
                .WithColor(GetColor(Context))
                .WithCurrentTimestamp()
                .WithDescription(itemOutput.ToString())
                .WithTitle(itemsPresent ? $"Page **{page}** of **{user.Items.Count / 20 + 1}**" : "Invalid page")
                .Build());
        }

        [Command("item")]
        [Alias("iteminfo")]
        [Summary("Returns information about an item.")]
        public async Task ItemCommand([Remainder] string searchTerm)
        {
            var item = GetItem<Weapon>(searchTerm);
            if (item == null)
            {
                await SendErrorAsync($"Invalid item ID and/or number of items! Use `mb/help buy` to see how the command works.");
                return;
            }

            var user = GetUser(Context);
            if (item.Stage > user.Stage)
            {
                await ReplyAsync(embed: new EmbedBuilder()
                    .WithColor(GetColor(Context))
                    .WithCurrentTimestamp()
                    .WithDescription($"{StageTooHighString()}\n\nYou are unable to view information about this item!")
                    .WithTitle(item.Name)
                    .Build());
                return;
            }

            var price = item.Price == -1 ? "N/A" : $"{UoM}{item.Price:n2}";
            var builder = new EmbedBuilder()
                .WithColor(GetColor(Context))
                .WithCurrentTimestamp()
                .WithDescription(item.Description)
                .WithTitle(item.Name)
                .AddField("ID", $"{item.Id:000}", true)
                .AddField("Price", price, true)
                .AddField("For Sale", item.OnSale ? "Yes" : "No", true)
                .AddField("Scavenge Location", Enum.GetName(typeof(ScavengeLocation), item.ScavengeLocation), true);

            if (user.Stage > 1) builder.AddField("Stage", item.Stage, true);

            if (item.WarClass > 0)
            {
                builder.AddField("Weapon Info", new StringBuilder()
                    .AppendLine($"Class: **{Enum.GetName(typeof(WeaponClass), item.WarClass)}**")
                    .AppendLine($"Accuracy: **{item.Accuracy}**%")
                    .AppendLine($"Damage: **{item.Damage}**")
                    .AppendLine($"Uses: **{item.Uses}**"), true);
                if (item.Ammo != null)
                {
                    var output = new StringBuilder();
                    foreach (var itemId in item.Ammo)
                        output.AppendLine($"`[{itemId:000}]` {GetItem<Item>(itemId.ToString("000")).Name}");
                    builder.AddField("Ammo", output.ToString(), true);
                }
            }
            else if (item.Damage > 0)
                builder.AddField("Ammo Damage", item.Damage, true);

            if (item.CraftingRecipe.Count > 0)
            {
                var output = new StringBuilder();
                foreach (var rawItem in item.CraftingRecipe)
                    output.AppendLine($"`[{rawItem.Key}]` {GetItem<Item>(rawItem.Key).Name}: {rawItem.Value}");
                builder.AddField($"Crafting Recipe (produces **{item.CraftingProduced}**)", output.ToString());
            }

            await ReplyAsync(embed: builder.Build());
        }

        [Command("itemlist")]
        [Summary("Gives a link to the item list.")]
        public async Task ItemListCommand()
            => await ReplyAsync("https://docs.google.com/spreadsheets/d/1tKT8nFH4Aa1VkH_UeieoOkN_iBAfueZLqLOdHsVTJ1I/edit#gid=0");

        [Command("poupsoop")]
        [Alias("poupsoopcalc", "poupcalc")]
        [Summary("Calculates the total price of Poup Soop.")]
        public async Task PoupSoopCalcCommand([Remainder] string msg)
        {
            var splitMsg = msg.Split('|');
            var totalCost = 0m;
            var builder = new EmbedBuilder()
                .WithColor(GetColor(Context))
                .WithCurrentTimestamp()
                .WithTitle("Poup Soop Price Calculator");
            decimal[] poupSoopPrices = { 364400, 552387946, 140732609585, 180269042735, 221548933670, 262310854791, 303496572188, 1802201667100, 374180952623987 };
            for (int i = 0; i < splitMsg.Length; i++)
            {
                var no = Convert.ToDecimal(splitMsg[i]);
                var subtot = (no * poupSoopPrices[i]);
                totalCost += subtot;
                var type = i switch
                {
                    0 => "Regular",
                    1 => "Limited",
                    2 => "Frozen",
                    3 => "Orange",
                    4 => "Electric",
                    5 => "Burning",
                    6 => "Rotten",
                    7 => "Ulteymut",
                    _ => "Variety Pack"
                };
                builder.AddField($"{type} x{no}", $"Cost: {UoM}{subtot:n2}");
            }
            builder.AddField("Total Cost", $"{UoM}{totalCost:n2}");
            await ReplyAsync(embed: builder.Build());
        }

        [Command("profile")]
        [Alias("stats")]
        [Summary("Returns the profile of you or someone else.")]
        public async Task ProfileCommand([Remainder] string searchTerm = "")
        {
            var user = new MarbleBotUser();
            var id = Context.User.Id;
            if (string.IsNullOrEmpty(searchTerm)) user = GetUser(Context);
            else
            {
                string json;
                using (var users = new StreamReader($"Data{Path.DirectorySeparatorChar}Users.json")) json = users.ReadToEnd();
                var rawUsers = JsonConvert.DeserializeObject<Dictionary<string, MarbleBotUser>>(json);
                var foundUser = rawUsers.Where(usr => searchTerm.ToLower().Contains(usr.Value.Name.ToLower())
                || usr.Value.Name.ToLower().Contains(searchTerm.ToLower())
                || searchTerm.ToLower().Contains(usr.Value.Discriminator)).LastOrDefault();

                if (foundUser.Value == null)
                {
                    await SendErrorAsync($"**{Context.User.Username}**, the requested user could not be found.");
                    return;
                }

                id = ulong.Parse(foundUser.Key);
                user = foundUser.Value;
            }
            
            var lastDaily = user.LastDaily.ToString("yyyy-MM-dd HH:mm:ss");
            if (user.LastDaily.Year == 1) lastDaily = "N/A";
            var lastRaceWin = user.LastRaceWin.ToString("yyyy-MM-dd HH:mm:ss");
            if (user.LastRaceWin.Year == 1) lastRaceWin = "N/A";
            var lastScavenge = user.LastScavenge.ToString("yyyy-MM-dd HH:mm:ss");
            if (user.LastScavenge.Year == 1) lastScavenge = "N/A";
            var lastSiegeWin = user.LastSiegeWin.ToString("yyyy-MM-dd HH:mm:ss");
            if (user.LastSiegeWin.Year == 1) lastSiegeWin = "N/A";
            var lastWarWin = user.LastWarWin.ToString("yyyy-MM-dd HH:mm:ss");
            if (user.LastWarWin.Year == 1) lastWarWin = "N/A";

            var author = Context.Client.GetUser(id);
            var builder = new EmbedBuilder()
                .WithAuthor(author)
                .WithCurrentTimestamp()
                .WithColor(GetColor(Context))
                .WithFooter("All times in UTC, all dates YYYY-MM-DD.")
                .AddField("Balance", $"{UoM}{user.Balance:n2}", true)
                .AddField("Net Worth", $"{UoM}{user.NetWorth:n2}", true)
                .AddField("Daily Streak", user.DailyStreak, true)
                .AddField("Siege Mentions", user.SiegePing, true)
                .AddField("War Mentions", user.WarPing, true)
                .AddField("Race Wins", user.RaceWins, true)
                .AddField("Siege Wins", user.SiegeWins, true)
                .AddField("War Wins", user.WarWins, true)
                .AddField("Last Daily", lastDaily, true)
                .AddField("Last Race Win", lastRaceWin, true)
                .AddField("Last Scavenge", lastScavenge, true)
                .AddField("Last Siege Win", lastSiegeWin, true)
                .AddField("Last War Win", lastWarWin, true);

            if (user.Stage == 2)
            {
                var shield = user.Items.ContainsKey(063) && user.Items[063] > 0 ? "Coating of Destruction" : "None";
                var spikes = "None";
                foreach (var itemPair in user.Items)
                {
                    var item = GetItem<Item>(itemPair.Key.ToString("000"));
                    if (item.Name.Contains("Spikes") && itemPair.Value > 0) spikes = item.Name;
                }
                builder.AddField("Stage", user.Stage, true)
                  .AddField("Shield", shield, true)
                  .AddField("Spikes", spikes, true);
            }

            var weaponOutput = new StringBuilder();
            foreach (var itemPair in user.Items)
            {
                var weapon = GetItem<Weapon>(itemPair.Key.ToString("000"));
                if (weapon.WarClass == 0) continue;
                weaponOutput.AppendLine(weapon.ToString());
            }
            if (weaponOutput.Length > 0) builder.AddField("Weapons", weaponOutput.ToString());
            await ReplyAsync(embed: builder.Build());
        }

        [Command("recipes")]
        [Summary("Shows all crafting recipes in a range of IDs.")]
        public async Task RecipesCommand(string rawPage = "1")
        {
            var embed = new EmbedBuilder()
                .WithColor(GetColor(Context))
                .WithCurrentTimestamp();
            var items = GetItemsObject().ToObject<Dictionary<string, Item>>();
            if (!int.TryParse(rawPage, out int page))
            {
                await ReplyAsync("Invalid number! Use `mb/help recipes` for more info.");
                return;
            }

            var minValue = page * 20 - 20;
            if (minValue > int.Parse(items.Last().Key))
            {
                await ReplyAsync("The last item has ID `102`!");
                return;
            }

            var maxValue = page * 20 - 1;
            embed.WithTitle($"Recipes in IDs `{minValue:000}`-`{maxValue:000}`");
            foreach (var itemPair in items)
            {
                if (itemPair.Value.CraftingProduced != 0)
                {
                    var itemId = int.Parse(itemPair.Key);
                    if (itemId >= minValue && itemId <= maxValue)
                    {
                        if (itemPair.Value.Stage > GetUser(Context).Stage)
                            embed.AddField($"`[{itemPair.Key}]` {GetItem<Item>(itemPair.Key).Name}",
                                $"{StageTooHighString()}\n\nYou are unable to view information about this item!");
                        else
                        {
                            var output = new StringBuilder();
                            foreach (var ingredient in itemPair.Value.CraftingRecipe)
                                output.AppendLine($"`[{ingredient.Key}]` {GetItem<Item>(ingredient.Key).Name}: {ingredient.Value}");
                            embed.AddField($"`[{itemPair.Key}]` {itemPair.Value.Name} (produces **{itemPair.Value.CraftingProduced}**)", output.ToString());
                        }
                    }
                    if (itemId > maxValue) break;
                }
            }
            await ReplyAsync(embed: embed.Build());
        }

        [Command("richlist")]
        [Alias("richest", "top10", "leaderboard", "networthleaderboard")]
        [Summary("Shows the ten richest people globally by Net Worth.")]
        public async Task RichListCommand(string rawPage = "1")
        {
            if (!int.TryParse(rawPage, out int page))
            {
                await SendErrorAsync($"**{Context.User.Username}**, this is not a valid integer!");
                return;
            }

            if (page < 1)
            {
                await SendErrorAsync($"**{Context.User.Username}**, the leaderboard page must be at least one!");
                return;
            }

            string json;
            using (var userFile = new StreamReader($"Data{Path.DirectorySeparatorChar}Users.json")) json = userFile.ReadToEnd();
            var rawUsers = JsonConvert.DeserializeObject<Dictionary<string, MarbleBotUser>>(json);
            var richList = from user in rawUsers orderby user.Value.NetWorth descending select user.Value;
            var dataList = new List<(int, MarbleBotUser)>();
            int displayedPlace = 0;
            decimal lastValue = 0m;

            foreach (var user in richList)
            {
                if (user.NetWorth != lastValue)
                    displayedPlace++;
                dataList.Add((displayedPlace, user));
                lastValue = user.NetWorth;
            }

            if (page > dataList.Last().Item1 / 10)
            {
                await ReplyAsync($"**{Context.User.Username}**, there is nobody in page **{page}**!");
                return;
            }

            int maxValue = page * 10, minValue = (page - 1) * 10 + 1, userPos = 0;
            var output = new StringBuilder();
            foreach (var userPair in dataList)
            {
                if (userPair.Item1 < maxValue + 1 && userPair.Item1 >= minValue)
                    output.AppendLine($"{userPair.Item1}{userPair.Item1.Ordinal()}: {userPair.Item2.Name}#{userPair.Item2.Discriminator} - {UoM}**{userPair.Item2.NetWorth:n2}**");
                if (string.Compare(userPair.Item2.Name, Context.User.Username, false) == 0 && string.Compare(userPair.Item2.Discriminator, Context.User.Discriminator) == 0)
                    userPos = userPair.Item1;
            }

            var builder = new EmbedBuilder()
                .WithColor(GetColor(Context))
                .WithCurrentTimestamp()
                .WithTitle("Net Worth Leaderboard")
                .WithDescription(output.ToString());
            if (userPos != 0) builder.WithFooter($"Requested by {Context.User.Username}#{Context.User.Discriminator} ({userPos}{userPos.Ordinal()})", Context.User.GetAvatarUrl());
            else builder.WithFooter($"Requested by {Context.User.Username}#{Context.User.Discriminator}");
            await ReplyAsync(embed: builder.Build());
        }

        [Command("sell")]
        [Alias("sellitem")]
        [Summary("Sells items.")]
        public async Task SellCommand(string rawId, string rawNoOfItems = "1")
        {
            if (!int.TryParse(rawNoOfItems, out int noOfItems) && noOfItems > 0)
            {
                await SendErrorAsync($"**{Context.User.Username}**, invalid item ID and/or number of items! Use `mb/help sell` to see how the command works.");
                return;
            }

            var item = GetItem<Item>(rawId);
            if (item == null)
            {
                await SendErrorAsync($"**{Context.User.Username}**, invalid item ID and/or number of items! Use `mb/help sell` to see how the command works.");
                return;
            }

            if (item.Price == -1)
            {
                await SendErrorAsync($"**{Context.User.Username}**, this item cannot be sold!");
                return;
            }

            var obj = GetUsersObject();
            var user = GetUser(Context, obj);
            if (user.Items.ContainsKey(item.Id) && user.Items[item.Id] >= noOfItems)
            {
                user.Balance += item.Price * noOfItems;
                user.Items[item.Id] -= noOfItems;
                WriteUsers(obj, Context.User, user);
                await ReplyAsync($"**{user.Name}** has successfully sold **{item.Name}** x**{noOfItems}** for {UoM}**{item.Price:n2}** each!\nTotal price: {UoM}**{item.Price * noOfItems:n2}**\nNew balance: {UoM}**{user.Balance:n2}**.");
            }
            else await SendErrorAsync($"You don't have enough of this item!");
        }

        [Command("shop")]
        [Alias("store")]
        [Summary("Shows all items available for sale, their IDs and their prices.")]
        public async Task ShopCommand()
        {
            var output = new StringBuilder();
            string json;
            using (var userFile = new StreamReader($"Resources{Path.DirectorySeparatorChar}Items.json")) json = userFile.ReadToEnd();
            var obj = JObject.Parse(json);
            var items = obj.ToObject<Dictionary<string, Item>>();
            foreach (var item in items)
            {
                if (item.Value.OnSale)
                    output.AppendLine($"`[{item.Key:000}]` **{item.Value.Name}** - {UoM}**{item.Value.Price:n2}**");
            }

            var builder = new EmbedBuilder()
                .WithColor(GetColor(Context))
                .WithCurrentTimestamp()
                .WithDescription(output.ToString())
                .WithTitle("All items for sale");
            await ReplyAsync(embed: builder.Build());
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
                    if (!SiegeInfo.ContainsKey(fileId))
                    {
                        await SendErrorAsync($"**{Context.User.Username}**, that item can't be used here!");
                        return;
                    }

                    await SiegeInfo[fileId].WeaponAttack(Context, item);
                    return;
                }

                switch (item.Id)
                {
                    case 1:
                        {
                            ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                            if (SiegeInfo.ContainsKey(fileId))
                            {
                                var output = new StringBuilder();
                                var userMarble = SiegeInfo[fileId].Marbles.Find(m => m.Id == Context.User.Id);
                                foreach (var marble in SiegeInfo[fileId].Marbles)
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
                            if (SiegeInfo.ContainsKey(fileId))
                                await SiegeInfo[fileId].ItemAttack(Context, obj, item.Id,
                                    (int)Math.Round(90 + SiegeInfo[fileId].Boss.MaxHP * 0.05 * (Rand.NextDouble() * 0.12 + 0.94)));
                            else await SendErrorAsync($"**{Context.User.Username}**, that item can't be used here!");
                            break;
                        }
                    case 14:
                        {
                            ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                            if (SiegeInfo.ContainsKey(fileId))
                                await SiegeInfo[fileId].ItemAttack(Context, obj, 14,
                                    70 + 10 * (int)SiegeInfo[fileId].Boss.Difficulty, true);
                            else await SendErrorAsync($"**{Context.User.Username}**, that item can't be used here!");
                            break;
                        }
                    case 17:
                        await ReplyAsync("Er... why aren't you using `mb/craft`?");
                        break;
                    case 18:
                        if (ScavengeInfo.ContainsKey(Context.User.Id))
                        {
                            UpdateUser(item, -1);
                            if (ScavengeInfo[Context.User.Id].Location == ScavengeLocation.CanaryBeach)
                            {
                                UpdateUser(GetItem<Item>("019"), 1);
                                await ReplyAsync($"**{Context.User.Username}** dragged a **{item.Name}** across the water, turning it into a **Water Bucket**!");
                            }
                            else if (ScavengeInfo[Context.User.Id].Location == ScavengeLocation.VioletVolcanoes)
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
                            if (SiegeInfo.ContainsKey(fileId) && string.Compare(SiegeInfo[fileId].Boss.Name, "Help Me the Tree", true) == 0)
                            {
                                var randDish = 22 + Rand.Next(0, 13);
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
                            if (SiegeInfo.ContainsKey(fileId))
                            {
                                var output = new StringBuilder();
                                var userMarble = SiegeInfo[fileId].Marbles.Find(m => m.Id == Context.User.Id);
                                foreach (var marble in SiegeInfo[fileId].Marbles)
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
                            if (SiegeInfo.ContainsKey(fileId))
                            {
                                var userMarble = SiegeInfo[fileId].Marbles.Find(m => m.Id == Context.User.Id);
                                foreach (var marble in SiegeInfo[fileId].Marbles)
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
                            if (SiegeInfo.ContainsKey(fileId))
                            {
                                var userMarble = SiegeInfo[fileId].Marbles.Find(m => m.Id == Context.User.Id);
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
                            if (SiegeInfo.ContainsKey(fileId))
                            {
                                var userMarble = SiegeInfo[fileId].Marbles.Find(m => m.Id == Context.User.Id);
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
                            if (!SiegeInfo.ContainsKey(fileId))
                            {
                                SiegeInfo.GetOrAdd(fileId, new Siege(Context, new List<SiegeMarble>())
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