using Discord;
using Discord.Commands;
using MarbleBot.BaseClasses;
using MarbleBot.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarbleBot.Modules {
    /// <summary> Commands related to currency. </summary>
    public class Economy : MarbleBotModule {
        [Command("balance")]
        [Alias("credits", "money", "bal")]
        [Summary("Returns how much money you or someone else has.")]
        public async Task BalanceCommandAsync([Remainder] string searchTerm = "") {
            await Context.Channel.TriggerTypingAsync();
            var User = new MBUser();
            var id = Context.User.Id;
            if (searchTerm.IsEmpty()) User = GetUser(Context);
            else {
                string json;
                using (var users = new StreamReader("Users.json")) json = users.ReadToEnd();
                var rawUsers = JsonConvert.DeserializeObject<Dictionary<string, MBUser>>(json);
                var foundUser = rawUsers.Where(usr => searchTerm.ToLower().Contains(usr.Value.Name.ToLower())
                || usr.Value.Name.ToLower().Contains(searchTerm.ToLower())
                || searchTerm.ToLower().Contains(usr.Value.Discriminator)).FirstOrDefault();
                id = ulong.Parse(foundUser.Key);
                User = foundUser.Value;
            }
            var author = Context.Client.GetUser(id);
            var builder = new EmbedBuilder()
                .WithAuthor(author)
                .WithCurrentTimestamp()
                .WithColor(GetColor(Context))
                .AddField("Balance", $"{Global.UoM}{User.Balance:n}", true)
                .AddField("Net Worth", $"{Global.UoM}{User.NetWorth:n}", true);
            await ReplyAsync(embed: builder.Build());
        }

        [Command("buy")]
        [Alias("buyitem")]
        [Summary("Buys items.")]
        public async Task BuyCommandAsync(string rawID, string rawNo = "1") {
            await Context.Channel.TriggerTypingAsync();
            if (int.TryParse(rawNo, out int noOfItems) && noOfItems > 0) {
                var item = GetItem(rawID);
                if (item.Id == -1) await ReplyAsync(":warning: | Could not find the requested item!");
                else if (item.Id == -2) await ReplyAsync(":warning: | Invalid item ID and/or number of items! Use `mb/help buy` to see how the command works.");
                else if (item.Price == -1) await ReplyAsync("This item cannot be sold!");
                else if (item.OnSale) {
                    var obj = GetUsersObj();
                    var user = GetUser(Context, obj);
                    if (user.Balance >= item.Price * noOfItems) {
                        if (user.Items != null) {
                            if (user.Items.ContainsKey(item.Id)) user.Items[item.Id] += noOfItems;
                            else user.Items.Add(item.Id, noOfItems);
                        } else {
                            user.Items = new SortedDictionary<int, int> {
                                { item.Id, noOfItems }
                            };
                        }
                        user.Balance -= item.Price * noOfItems;
                        WriteUsers(obj, Context.User, user);
                        await ReplyAsync($"**{user.Name}** has successfully purchased **{item.Name}** x**{noOfItems}** for <:unitofmoney:372385317581488128>**{item.Price:n}** each!\nTotal price: <:unitofmoney:372385317581488128>**{item.Price * noOfItems:n}**\nNew balance: <:unitofmoney:372385317581488128>**{user.Balance:n}**.");
                    } else await ReplyAsync($":warning: | You can't afford this!");
                } else await ReplyAsync($":warning: | This item is not on sale!");
            } else await ReplyAsync($":warning: | Invalid item ID and/or number of items! Use `mb/help buy` to see how the command works.");
        }

        [Command("craft")]
        [Summary("Crafts an item out of other items.")]
        public async Task CraftCommandAsync(string searchTerm, string rawNo = "1")
        {
            await Context.Channel.TriggerTypingAsync();
            var obj = GetUsersObj();
            var user = GetUser(Context, obj);
            if (user.Items.ContainsKey(17) || user.Items.ContainsKey(62)) {
                if (!byte.TryParse(rawNo, out byte noOfItems)) searchTerm += rawNo;
                var requestedItem = GetItem(searchTerm);
                if (requestedItem.CraftingStationRequired == 2 && !user.Items.ContainsKey(62))
                    await ReplyAsync($":warning: | **{Context.User.Username}**, your current Crafting Station cannot craft this item!");
                else if (requestedItem.CraftingRecipe.Count > 0) {
                    var sufficientMaterials = true;
                    foreach (var item in requestedItem.CraftingRecipe) {
                        if (!user.Items.ContainsKey(int.Parse(item.Key)) || item.Value * noOfItems > user.Items[int.Parse(item.Key)]) {
                            sufficientMaterials = false;
                            break;
                        }
                    }
                    if (sufficientMaterials) {
                        var noCrafted = (int)requestedItem.CraftingProduced * noOfItems;
                        var embed = new EmbedBuilder()
                               .WithCurrentTimestamp()
                               .WithColor(GetColor(Context))
                               .WithDescription($"**{Context.User.Username}** has successfully crafted **{requestedItem.Name}** x**{noCrafted}**!")
                               .WithTitle("Crafting: " + requestedItem.Name);
                        var output = new StringBuilder();
                        var currentNetWorth = user.NetWorth;
                        foreach (var rawItem in requestedItem.CraftingRecipe) {
                            var item = GetItem(rawItem.Key);
                            var noLost = rawItem.Value * noOfItems;
                            output.AppendLine($"`[{item.Id.ToString("000")}]` {item.Name}: {noLost}");
                            user.Items[int.Parse(rawItem.Key)] -= noLost;
                            user.NetWorth -= item.Price * noOfItems;
                        }
                        if (!user.Items.ContainsKey(requestedItem.Id)) user.Items.Add(requestedItem.Id, noCrafted);
                        else user.Items[requestedItem.Id] += noCrafted;
                        user.NetWorth += requestedItem.Price * noOfItems;
                        embed.AddField("Lost items", output.ToString())
                            .AddField("Net Worth", $"Old: {Global.UoM}**{currentNetWorth:n}**\nNew: {Global.UoM}**{user.NetWorth:n}**");
                        WriteUsers(obj, Context.User, user);    
                        await ReplyAsync(embed: embed.Build());
                    } else await ReplyAsync($":warning: | **{Context.User.Username}**, you do not have enough items to craft this!");
                } else await ReplyAsync($":warning: | **{Context.User.Username}**, the item **{requestedItem.Name}** cannot be crafted!");
            } else await ReplyAsync($":warning: | **{Context.User.Username}**, you need a Crafting Station to craft items!");
        }

        [Command("craftable")]
        [Summary("Returns a list of all items that can be crafted with the user's inventory.")]
        public async Task CraftableCommandAsync() {
            await Context.Channel.TriggerTypingAsync();
            var user = GetUser(Context);
            var output = new StringBuilder();
            string json;
            using (var itemFile = new StreamReader("Resources\\Items.json")) json = itemFile.ReadToEnd();
            var obj = JObject.Parse(json);
            var items = obj.ToObject<Dictionary<string, Item>>();
            foreach (var itemPair in items) {
                if (itemPair.Value.CraftingProduced != 0) {
                    var craftable = true;
                    foreach (var ingredient in itemPair.Value.CraftingRecipe) {
                        var id = int.Parse(ingredient.Key);
                        if (!user.Items.ContainsKey(id) || user.Items[id] < ingredient.Value) {
                            craftable = false;
                            break;
                        }
                    }
                    if (craftable) output.AppendLine($"`[{itemPair.Key}]` {itemPair.Value.Name}");
                }
            }
            if (output.Length < 1) output.Append("There are no items you can craft!");
            await ReplyAsync(embed: new EmbedBuilder()
                .WithAuthor(Context.User)
                .WithColor(GetColor(Context))
                .WithCurrentTimestamp()
                .WithDescription($"**Craftable items**\n{output.ToString()}")
                .Build());
        }

        [Command("daily")]
        [Summary("Gives daily Units of Money (200 to the power of your streak minus one).")]
        public async Task DailyCommandAsync() {
            await Context.Channel.TriggerTypingAsync();
            var obj = GetUsersObj();
            var user = GetUser(Context, obj);
            if (DateTime.UtcNow.Subtract(user.LastDaily).TotalHours > 24) {
                if (DateTime.UtcNow.Subtract(user.LastDaily).TotalHours > 60) user.DailyStreak = 0;
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
                if (!user.Items.ContainsKey(10) && DateTime.UtcNow.DayOfYear < 51 && DateTime.UtcNow.DayOfYear > 42) {
                    orange = true;
                    user.Items.Add(10, 1);
                }
                WriteUsers(obj, Context.User, user);
                await ReplyAsync($"**{Context.User.Username}**, you have received <:unitofmoney:372385317581488128>**{gift:n}**!\n(Streak: **{user.DailyStreak}**)");
                if (craftingStation) await ReplyAsync("You have been given a **Crafting Station Mk.I**!");
                if (orange) await ReplyAsync("You have been given a **Qefpedun Charm**!");
            } else {
                var ADayAgo = DateTime.UtcNow.AddDays(-1);
                await ReplyAsync($"You need to wait for **{GetDateString(user.LastDaily.Subtract(ADayAgo))}** until you can get your daily gift again!");
            }
        }

        /*[Command("donate")]
        [Summary("Donate money to others")]
        public async Task _donate(string money, [Remainder] string searchUser = "") {
            if (money == "accept") {

            } else {
                var gift = ulong.Parse(money.Trim());
                
                
                var rawUsers = JsonConvert.DeserializeObject<Dictionary<string, MoneyUser>>(json);
                var search = from user in rawUsers where searchUser.ToLower().Contains(user.Value.Name.ToLower()) || user.Value.Name.ToLower().Contains(searchUser.ToLower()) || searchUser.ToLower().Contains(user.Value.Discriminator) select user;
                var foundUser = search.FirstOrDefault();

            }
        }*/

        [Command("inventory")]
        [Alias("inv", "items")]
        [Summary("Shows all the items a user has.")]
        public async Task InventoryCommandAsync([Remainder] string searchTerm = "")
        {
            var user = new MBUser();
            var id = Context.User.Id;
            if (searchTerm.IsEmpty()) user = GetUser(Context);
            else {
                string json;
                using (var users = new StreamReader("Users.json")) json = users.ReadToEnd();
                var rawUsers = JsonConvert.DeserializeObject<Dictionary<string, MBUser>>(json);
                var foundUser = rawUsers.Where(usr => searchTerm.ToLower().Contains(usr.Value.Name.ToLower())
                || usr.Value.Name.ToLower().Contains(searchTerm.ToLower())
                || searchTerm.ToLower().Contains(usr.Value.Discriminator)).FirstOrDefault();
                id = ulong.Parse(foundUser.Key);
                user = foundUser.Value;
            }
            var itemOutput = new StringBuilder();
            if (user.Items.Count > 0) {
                foreach (var item in user.Items) {
                    if (item.Value > 0)
                        itemOutput.AppendLine($"`[{item.Key.ToString("000")}]` {GetItem(item.Key.ToString()).Name}: {item.Value}");
                }
            } else itemOutput.Append("None");
            var author = Context.Client.GetUser(id);
            await ReplyAsync(embed: new EmbedBuilder()
                .WithAuthor(author)
                .WithColor(GetColor(Context))
                .WithCurrentTimestamp()
                .WithDescription(itemOutput.ToString())
                .Build());
        }

        [Command("item")]
        [Alias("iteminfo")]
        [Summary("Returns information about an item.")]
        public async Task ItemCommandAsync([Remainder] string searchTerm) {
            await Context.Channel.TriggerTypingAsync();
            var item = GetItem(searchTerm);
            if (item.Id == -1) await ReplyAsync(":warning: | Could not find the requested item!");
            else if (item.Id == -2) await ReplyAsync(":warning: | Invalid item ID and/or number of items! Use `mb/help buy` to see how the command works.");
            else {
                if (item.Stage > GetUser(Context).Stage) {
                    string cannotViewMsg;
                    switch (Global.Rand.Next(0, 4)) {
                        case 0: cannotViewMsg = "Your inexperience blinds you..."; break;
                        case 1: cannotViewMsg = "Your vision is blurry..."; break;
                        case 2: cannotViewMsg = "Screams echo in your head..."; break;
                        default: cannotViewMsg = "Your mind is wracked with pain..."; break;
                    }
                    await ReplyAsync(embed: new EmbedBuilder()
                        .WithColor(GetColor(Context))
                        .WithCurrentTimestamp()
                        .WithDescription($"*{cannotViewMsg}*\n\nYou are unable to view information about this item!")
                        .WithTitle(item.Name)
                        .Build());
                } else {
                    var price = item.Price == -1 ? "N/A" : $"{Global.UoM}{item.Price:n}";
                    var builder = new EmbedBuilder()
                        .WithColor(GetColor(Context))
                        .WithCurrentTimestamp()
                        .WithDescription(item.Description)
                        .WithTitle(item.Name)
                        .AddField("ID", $"{item.Id:000}", true)
                        .AddField("Price", price, true)
                        .AddField("For Sale", item.OnSale ? "Yes" : "No", true)
                        .AddField("Scavenge Location", Enum.GetName(typeof(ScavengeLocation), item.ScavengeLocation));

                    if (item.CraftingRecipe.Count > 0) {
                        var output = new StringBuilder();
                        foreach (var rawItem in item.CraftingRecipe)
                            output.AppendLine($"`[{rawItem.Key}]` {GetItem(rawItem.Key).Name}: {rawItem.Value}");
                        builder.AddField($"Crafting Recipe (produces **{item.CraftingProduced}**)", output.ToString());
                    }
                    await ReplyAsync(embed: builder.Build());
                }
            }
        }

        [Command("poupsoop")]
        [Alias("poupsoopcalc, poupcalc")]
        [Summary("Calculates the total price of Poup Soop.")]
        public async Task PoupSoopCalcCommandAsync([Remainder] string msg) {
            await Context.Channel.TriggerTypingAsync();
            var splitMsg = msg.Split('|');
            var totalCost = 0m;
            var builder = new EmbedBuilder()
                .WithColor(GetColor(Context))
                .WithCurrentTimestamp()
                .WithTitle("Poup Soop Price Calculator");
            decimal[] poupSoopPrices = { 364400, 552387946, 140732609585, 180269042735, 221548933670, 262310854791, 303496572188, 1802201667100, 374180952623987 };
            for (int i = 0; i < splitMsg.Length; i++) {
                var no = splitMsg[i].ToDecimal();
                var subtot = (no * poupSoopPrices[i]);
                totalCost += subtot;
                var type = "";
                switch (i) {
                    case 0: type = "Regular"; break;
                    case 1: type = "Limited"; break;
                    case 2: type = "Frozen"; break;
                    case 3: type = "Orange"; break;
                    case 4: type = "Electric"; break;
                    case 5: type = "Burning"; break;
                    case 6: type = "Rotten"; break;
                    case 7: type = "Ulteymut"; break;
                    case 8: type = "Variety Pack"; break;
                }
                builder.AddField($"{type} x{no}", $"Cost: {Global.UoM}{subtot:n}");
            }
            builder.AddField("Total Cost", $"{Global.UoM}{totalCost:n}");
            await ReplyAsync(embed: builder.Build());
        }

        [Command("profile")]
        [Alias("stats")]
        [Summary("Returns the profile of you or someone else.")]
        public async Task ProfileCommandAsync([Remainder] string searchTerm = "") {
            await Context.Channel.TriggerTypingAsync();
            var user = new MBUser();
            var id = Context.User.Id;
            if (searchTerm.IsEmpty()) user = GetUser(Context);
            else {
                string json;
                using (var users = new StreamReader("Users.json")) json = users.ReadToEnd();
                var rawUsers = JsonConvert.DeserializeObject<Dictionary<string, MBUser>>(json);
                var foundUser = rawUsers.Where(usr => searchTerm.ToLower().Contains(usr.Value.Name.ToLower())
                || usr.Value.Name.ToLower().Contains(searchTerm.ToLower())
                || searchTerm.ToLower().Contains(usr.Value.Discriminator)).FirstOrDefault();
                id = ulong.Parse(foundUser.Key);
                user = foundUser.Value;
            }
            var lastDaily = user.LastDaily.ToString("yyyy-MM-dd hh:mm:ss");
            if (user.LastDaily.Year == 2019 && user.LastDaily.DayOfYear == 1) lastDaily = "N/A";
            var lastRaceWin = user.LastRaceWin.ToString("yyyy-MM-dd hh:mm:ss");
            if (user.LastRaceWin.Year == 2019 && user.LastRaceWin.DayOfYear == 1) lastRaceWin = "N/A";
            var lastScavenge = user.LastScavenge.ToString("yyyy-MM-dd hh:mm:ss");
            if (user.LastScavenge.Year == 2019 && user.LastScavenge.DayOfYear == 1) lastScavenge = "N/A";
            var lastSiegeWin = user.LastSiegeWin.ToString("yyyy-MM-dd hh:mm:ss");
            if (user.LastSiegeWin.Year == 2019 && user.LastSiegeWin.DayOfYear == 1) lastSiegeWin = "N/A";
            var author = Context.Client.GetUser(id);
            var itemOutput = new StringBuilder();
            if (user.Items.Count > 0) {
                foreach (var item in user.Items) {
                    if (item.Value > 0)
                        itemOutput.AppendLine($"`[{item.Key.ToString("000")}]` {GetItem(item.Key.ToString()).Name}: {item.Value}");
                }
            } else itemOutput.Append("None");
            var builder = new EmbedBuilder()
                .WithAuthor(author)
                .WithCurrentTimestamp()
                .WithColor(GetColor(Context))
                .WithFooter("All times in UTC, all dates YYYY-MM-DD.")
                .AddField("Balance", $"<:unitofmoney:372385317581488128>{user.Balance:n}", true)
                .AddField("Net Worth", $"<:unitofmoney:372385317581488128>{user.NetWorth:n}", true)
                .AddField("Daily Streak", user.DailyStreak, true)
                .AddField("Siege Mentions", user.SiegePing, true)
                .AddField("Race Wins", user.RaceWins, true)
                .AddField("Siege Wins", user.SiegeWins, true)
                .AddField("Last Daily", lastDaily, true)
                .AddField("Last Race Win", lastRaceWin, true)
                .AddField("Last Scavenge", lastScavenge, true)
                .AddField("Last Siege Win", lastSiegeWin, true)
                .AddField("Items", itemOutput.ToString());
            await ReplyAsync(embed: builder.Build());
        }

        [Command("recipes")]
        [Summary("Shows all crafting recipes in a range of IDs.")]
        public async Task RecipesCommandAsync(string rawIndex = "1")
        {
            var embed = new EmbedBuilder()
                .WithColor(GetColor(Context))
                .WithCurrentTimestamp();
            string json;
            using (var itemFile = new StreamReader("Resources\\Items.json")) json = itemFile.ReadToEnd();
            var obj = JObject.Parse(json);
            var items = obj.ToObject<Dictionary<string, Item>>();
            if (int.TryParse(rawIndex, out int index)) {
                var minValue = index * 20 - 20;
                if (minValue > 63) await ReplyAsync("The last item has ID `063`!");
                else {
                    var maxValue = index * 20 - 1;
                    embed.WithTitle($"Recipes in IDs `{minValue.ToString("000")}`-`{maxValue.ToString("000")}`");
                    foreach (var itemPair in items) {
                        if (itemPair.Value.CraftingProduced != 0) {
                            var itemId = int.Parse(itemPair.Key);
                            if (itemId >= minValue && itemId <= maxValue) {
                                if (itemPair.Value.Stage > GetUser(Context).Stage) {
                                    string cannotViewMsg;
                                    switch (Global.Rand.Next(0, 4)) {
                                        case 0: cannotViewMsg = "Your inexperience blinds you..."; break;
                                        case 1: cannotViewMsg = "Your vision is blurry..."; break;
                                        case 2: cannotViewMsg = "Screams echo in your head..."; break;
                                        default: cannotViewMsg = "Your mind is wracked with pain..."; break;
                                    }
                                    embed.AddField($"`[{itemPair.Key}]` {GetItem(itemPair.Key).Name}", $"*{cannotViewMsg}*\n\nYou are unable to view information about this item!");
                                } else {
                                    var output = new StringBuilder();
                                    foreach (var ingredient in itemPair.Value.CraftingRecipe)
                                        output.AppendLine($"`[{ingredient.Key}]` {GetItem(ingredient.Key).Name}: {ingredient.Value}");
                                    embed.AddField($"`[{itemPair.Key}]` {itemPair.Value.Name} (produces **{itemPair.Value.CraftingProduced}**)", output.ToString());
                                }
                            }
                            if (itemId > maxValue) break;
                        }
                    }
                }
                await ReplyAsync(embed: embed.Build());
            } else await ReplyAsync("Invalid number! Use `mb/help recipes` for more info.");
        }

        [Command("richlist")]
        [Alias("richest", "top10", "leaderboard", "networthleaderboard")]
        [Summary("Shows the ten richest people globally by Net Worth.")]
        public async Task RichListCommandAsync(string rawNo = "1") {
            await Context.Channel.TriggerTypingAsync();
            if (!int.TryParse(rawNo, out int no)) await ReplyAsync("This is not a valid integer!");
            else {
                string json;
                using (var userFile = new StreamReader("Users.json")) json = userFile.ReadToEnd();
                var rawUsers = JsonConvert.DeserializeObject<Dictionary<string, MBUser>>(json);
                var users = new List<Tuple<string, MBUser>>();
                foreach (var user in rawUsers) users.Add(Tuple.Create(user.Key, user.Value));
                var richList = (from user in users orderby user.Item2.NetWorth descending select user.Item2).ToList();
                int i = 1, j = 1, yourPos = 0, minValue = (no - 1) * 10 + 1, maxValue = no * 10;
                var output = new StringBuilder();
                foreach (var user in richList) {
                    if (i < maxValue + 1 && i >= minValue) {
                        output.Append($"**{i}{i.Ordinal()}:** {user.Name}#{user.Discriminator} - <:unitofmoney:372385317581488128>**{user.NetWorth:n}**\n");
                        if (j < richList.Count) if (richList[j].NetWorth != user.NetWorth) i++;
                        if (user.Name == Context.User.Username && user.Discriminator == Context.User.Discriminator) yourPos = i - 1;
                    } else {
                        if (yourPos != 0) break;
                        else if (user.Name == Context.User.Username && user.Discriminator == Context.User.Discriminator && i >= minValue) {
                            yourPos = i - 1;
                            break;
                        }
                    }
                    if (i < maxValue + 1 && !(i >= minValue)) i++;
                    j++;
                }
                var builder = new EmbedBuilder()
                    .WithColor(GetColor(Context))
                    .WithCurrentTimestamp()
                    .WithTitle("Net Worth Leaderboard")
                    .WithDescription(output.ToString());
                if (yourPos != 0) builder.WithFooter($"Requested by {Context.User.Username}#{Context.User.Discriminator} ({yourPos}{yourPos.Ordinal()})", Context.User.GetAvatarUrl());
                else builder.WithFooter($"Requested by {Context.User.Username}#{Context.User.Discriminator}");
                await ReplyAsync(embed: builder.Build());
            }
        }

        [Command("sell")]
        [Alias("sellitem")]
        [Summary("Sells items.")]
        public async Task SellCommandAsync(string rawID, string rawNo = "1") {
            await Context.Channel.TriggerTypingAsync();
            if (int.TryParse(rawNo, out int noOfItems) && noOfItems > 0) {
                var item = GetItem(rawID);
                if (item.Id == -1) await ReplyAsync(":warning: | Could not find the requested item!");
                else if (item.Id == -2) await ReplyAsync(":warning: | Invalid item ID and/or number of items! Use `mb/help buy` to see how the command works.");
                else if (item.Price == -1) await ReplyAsync("This item cannot be sold!");
                else {
                    var obj = GetUsersObj();
                    var user = GetUser(Context, obj);
                    if (user.Items.ContainsKey(item.Id) && user.Items[item.Id] >= noOfItems) {
                        user.Balance += item.Price * noOfItems;
                        user.Items[item.Id] -= noOfItems;
                        WriteUsers(obj, Context.User, user);
                        await ReplyAsync($"**{user.Name}** has successfully sold **{item.Name}** x**{noOfItems}** for <:unitofmoney:372385317581488128>**{item.Price:n}** each!\nTotal price: <:unitofmoney:372385317581488128>**{item.Price * noOfItems:n}**\nNew balance: <:unitofmoney:372385317581488128>**{user.Balance:n}**.");
                    } else await ReplyAsync(":warning: | You don't have enough of this item!");
                }
            } else await ReplyAsync(":warning: | Invalid item ID and/or number of items! Use `mb/help sell` to see how the command works.");
        }

        [Command("shop")]
        [Alias("store")]
        [Summary("Shows all items available for sale, their IDs and their prices.")]
        public async Task ShopCommandAsync() {
            await Context.Channel.TriggerTypingAsync();
            var output = new StringBuilder();
            string json;
            using (var userFile = new StreamReader("Resources\\Items.json")) json = userFile.ReadToEnd();
            var obj = JObject.Parse(json);
            var items = obj.ToObject<Dictionary<string, Item>>();
            foreach (var item in items) {
                if (item.Value.OnSale)
                    output.AppendLine($"`[{item.Key:000}]` **{item.Value.Name}** - <:unitofmoney:372385317581488128>**{item.Value.Price:n}**");
            }
            var builder = new EmbedBuilder()
                .WithColor(GetColor(Context))
                .WithCurrentTimestamp()
                .WithDescription(output.ToString())
                .WithTitle("All items for sale");
            await ReplyAsync(embed: builder.Build());
        }

        [Command("use")]
        [Alias("useitem")]
        [Summary("Uses an item.")]
        public async Task UseCommandAsync([Remainder] string searchTerm)
        {
            var item = GetItem(searchTerm);
            var obj = GetUsersObj();
            var user = GetUser(Context, obj);

            void UpdateUser(Item itm, int noOfItems) {
                user.Items[itm.Id] -= noOfItems;
                user.NetWorth -= item.Price * noOfItems;
                WriteUsers(obj, Context.User, user);
            } 

            if (user.Items.ContainsKey(item.Id) && user.Items[item.Id] > 0) {
                switch (item.Id) {
                    case 1: {
                        ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                        if (Global.SiegeInfo.ContainsKey(fileId)) {
                            var output = new StringBuilder();
                            var userMarble = Global.SiegeInfo[fileId].Marbles.Find(m => m.Id == Context.User.Id);
                            foreach (var marble in Global.SiegeInfo[fileId].Marbles) {
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
                            UpdateUser(item, 1);
                        } else await ReplyAsync($"**{Context.User.Username}**, that item can't be used here!");
                        break;
                    }
                    case 10: {
                        ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                        if (Global.SiegeInfo.ContainsKey(fileId)) {
                            var userMarble = Global.SiegeInfo[fileId].Marbles.Find(m => m.Id == Context.User.Id);
                            if (userMarble.QefpedunCharmUsed) await ReplyAsync($"**{Context.User.Username}**, you can only use the **{item.Name}** once per battle!");
                            else {
                                if (Global.Rand.Next(0, 100) < userMarble.ItemAccuracy) {
                                    var dmg = (int)Math.Round(100 + Global.SiegeInfo[fileId].Boss.MaxHP * 0.06 * (Global.Rand.NextDouble() * 0.04 + 0.98));
                                    Global.SiegeInfo[fileId].DealDamage(dmg);
                                    await ReplyAsync(embed: new EmbedBuilder()
                                        .AddField("Boss HP", $"**{Global.SiegeInfo[fileId].Boss.HP}**/{Global.SiegeInfo[fileId].Boss.MaxHP}")
                                        .WithColor(GetColor(Context))
                                        .WithCurrentTimestamp()
                                        .WithDescription($"**{userMarble.Name}** used their **{item.Name}**, dealing **{dmg}** damage to the boss!")
                                        .WithTitle(item.Name)
                                        .Build());
                                    userMarble.DamageDealt += dmg;
                                    userMarble.ItemAccuracy -= 25;
                                    userMarble.QefpedunCharmUsed = true;
                                } else await ReplyAsync(embed: new EmbedBuilder()
                                    .WithColor(GetColor(Context))
                                    .WithCurrentTimestamp()
                                    .WithDescription($"**{userMarble.Name}** used their **{item.Name}**! It missed!")
                                    .WithTitle(item.Name)
                                    .Build());
                            }
                        } else await ReplyAsync($"**{Context.User.Username}**, that item can't be used here!");
                        break;
                    }
                    case 14: {
                        ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                        if (Global.SiegeInfo.ContainsKey(fileId)) {
                            var userMarble = Global.SiegeInfo[fileId].Marbles.Find(m => m.Id == Context.User.Id);
                            if (Global.Rand.Next(0, 100) < userMarble.ItemAccuracy) {
                                var dmg = (int)Math.Round(75 + 12.5 * (int)Global.SiegeInfo[fileId].Boss.Difficulty);
                                Global.SiegeInfo[fileId].DealDamage(dmg);
                                await ReplyAsync(embed: new EmbedBuilder()
                                    .AddField("Boss HP", $"**{Global.SiegeInfo[fileId].Boss.HP}**/{Global.SiegeInfo[fileId].Boss.MaxHP}")
                                    .WithColor(GetColor(Context))
                                    .WithCurrentTimestamp()
                                    .WithDescription($"**{userMarble.Name}** used a **{item.Name}**, dealing **{dmg}** damage to the boss!")
                                    .WithTitle(item.Name)
                                    .Build());
                                UpdateUser(item, 1);
                                userMarble.DamageDealt += dmg;
                                userMarble.ItemAccuracy -= 25;
                            } else await ReplyAsync(embed: new EmbedBuilder()
                                .WithColor(GetColor(Context))
                                .WithCurrentTimestamp()
                                .WithDescription($"**{userMarble.Name}** used their **{item.Name}**! It missed!")
                                .WithTitle(item.Name)
                                .Build());
                        } else await ReplyAsync($"**{Context.User.Username}**, that item can't be used here!");
                        break;
                    }
                    case 17:
                        await ReplyAsync("Er... why aren't you using `mb/craft`?");
                        break;
                    case 18:
                        if (Global.ScavengeInfo.ContainsKey(Context.User.Id)) {
                            user.Items[18]--;
                            if (user.Items.ContainsKey(19)) user.Items[19]++;
                            else user.Items.Add(19, 1);
                            await ReplyAsync($"**{Context.User.Username}** dragged a **{item.Name}** across the water, turning it into a **Water Bucket**!");
                        } else await ReplyAsync($"**{Context.User.Username}**, that item can't be used here!");
                        break;
                    case 19:
                        user.Items[19]--;
                        if (user.Items.ContainsKey(19)) user.Items[18]++;
                        else user.Items.Add(18, 1);
                        await ReplyAsync($"**{Context.User.Username}** poured all the water out from a **{item.Name}**, turning it into a **Steel Bucket**!");
                        break;
                    case 23: {
                        ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                        if (Global.SiegeInfo.ContainsKey(fileId)) {
                            var output = new StringBuilder();
                            var userMarble = Global.SiegeInfo[fileId].Marbles.Find(m => m.Id == Context.User.Id);
                            foreach (var marble in Global.SiegeInfo[fileId].Marbles)
                                marble.StatusEffect = MSE.Poison;
                            await ReplyAsync(embed: new EmbedBuilder()
                                .WithColor(GetColor(Context))
                                .WithCurrentTimestamp()
                                .WithDescription($"**{userMarble.Name}** used **{item.Name}**! Everyone was poisoned!")
                                .WithTitle(item.Name)
                                .Build());
                            UpdateUser(item, 1);
                        } else await ReplyAsync($"**{Context.User.Username}**, that item can't be used here!");
                        break;
                    }
                    case 24: goto case 23;
                    case 25: goto case 23;
                    case 26: goto case 23;
                    case 27: goto case 23;
                    case 28: goto case 23;
                    case 29: goto case 23;
                    case 30: goto case 23;
                    case 31: goto case 23;
                    case 32: goto case 23;
                    case 33: goto case 23;
                    case 34: goto case 23;
                    case 35: goto case 23;
                    case 38: {
                        ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                        if (Global.SiegeInfo.ContainsKey(fileId)) {
                            var userMarble = Global.SiegeInfo[fileId].Marbles.Find(m => m.Id == Context.User.Id);
                            foreach (var marble in Global.SiegeInfo[fileId].Marbles)
                                marble.StatusEffect = MSE.Doom;
                            await ReplyAsync(embed: new EmbedBuilder()
                                .WithColor(GetColor(Context))
                                .WithCurrentTimestamp()
                                .WithDescription($"**{userMarble.Name}** used **{item.Name}**! Everyone is doomed!")
                                .WithTitle(item.Name)
                                .Build());
                            UpdateUser(item, 1);
                        } else await ReplyAsync($"**{Context.User.Username}**, that item can't be used here!");
                        break;
                    }
                    case 39: {
                        ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                        if (Global.SiegeInfo.ContainsKey(fileId)) {
                            var userMarble = Global.SiegeInfo[fileId].Marbles.Find(m => m.Id == Context.User.Id);
                            userMarble.StatusEffect = MSE.None;
                            await ReplyAsync(embed: new EmbedBuilder()
                                .WithColor(GetColor(Context))
                                .WithCurrentTimestamp()
                                .WithDescription($"**{userMarble.Name}** used **{item.Name}** and is now cured!")
                                .WithTitle(item.Name)
                                .Build());
                            UpdateUser(item, 1);
                        } else await ReplyAsync($"**{Context.User.Username}**, that item can't be used here!");
                        break;
                    }
                    case 50: {
                        ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                        if (Global.SiegeInfo.ContainsKey(fileId)) {
                            if (user.Items.ContainsKey(048) && user.Items[048] > 0) {
                                var userMarble = Global.SiegeInfo[fileId].Marbles.Find(m => m.Id == Context.User.Id);
                                if (Global.Rand.Next(0, 100) < userMarble.ItemAccuracy / 3) {
                                    var dmg = (int)Math.Round(75 + 12.5 * (int)Global.SiegeInfo[fileId].Boss.Difficulty);
                                    Global.SiegeInfo[fileId].DealDamage(dmg);
                                    await ReplyAsync(embed: new EmbedBuilder()
                                        .AddField("Boss HP", $"**{Global.SiegeInfo[fileId].Boss.HP}**/{Global.SiegeInfo[fileId].Boss.MaxHP}")
                                        .WithColor(GetColor(Context))
                                        .WithCurrentTimestamp()
                                        .WithDescription($"**{userMarble.Name}** used their **{item.Name}**, dealing **{dmg}** damage to the boss!")
                                        .WithTitle(item.Name)
                                        .Build());
                                    UpdateUser(GetItem("048"), 1);
                                    userMarble.DamageDealt += dmg;
                                    userMarble.ItemAccuracy -= 25;
                                } else await ReplyAsync(embed: new EmbedBuilder()
                                    .WithColor(GetColor(Context))
                                    .WithCurrentTimestamp()
                                    .WithDescription($"**{userMarble.Name}** used their **{item.Name}**! It missed!")
                                    .WithTitle(item.Name)
                                    .Build());
                            } else await ReplyAsync($"**{Context.User.Username}**, you do not have enough ammo for this item!");
                        } else await ReplyAsync($"**{Context.User.Username}**, that item can't be used here!");
                        break;
                    }
                    case 51: goto case 50;
                    case 52: goto case 50;
                    case 53: {
                        ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                        var dmgs = new int[3];
                        var embed = new EmbedBuilder()
                            .WithColor(GetColor(Context))
                            .WithCurrentTimestamp()
                            .WithTitle(item.Name);
                        if (Global.SiegeInfo.ContainsKey(fileId)) {
                            if (user.Items.ContainsKey(048) && user.Items[048] > 2) {
                                var userMarble = Global.SiegeInfo[fileId].Marbles.Find(m => m.Id == Context.User.Id);
                                for (int i = 0; i < 3; i++) {
                                    if (Global.Rand.Next(0, 100) < userMarble.ItemAccuracy / 3) {
                                        var dmg = 110 + 9 * (int)Global.SiegeInfo[fileId].Boss.Difficulty;
                                        Global.SiegeInfo[fileId].DealDamage(dmg);
                                        dmgs[i] = dmg;
                                        embed.AddField($"Trebuchet {i + 1}", $"**{dmg}** damage to **{Global.SiegeInfo[fileId].Boss.Name}**");
                                    }
                                    else {
                                        dmgs[i] = 0;
                                        embed.AddField($"Trebuchet {i + 1}", "Missed!");
                                    }
                                }
                                embed.AddField("Boss HP", $"**{Global.SiegeInfo[fileId].Boss.HP}**/{Global.SiegeInfo[fileId].Boss.MaxHP}")
                                    .WithDescription($"**{userMarble.Name}** used a **{item.Name}**, dealing a total of {dmgs.Sum()} damage to the boss!");
                                await ReplyAsync(embed: embed.Build());
                                userMarble.DamageDealt += dmgs.Sum();
                                UpdateUser(GetItem("048"), 3);
                            } else await ReplyAsync($"**{Context.User.Username}**, you do not have enough ammo for this item!");
                        } else await ReplyAsync($"**{Context.User.Username}**, that item can't be used here!");
                        break;
                    }
                    default:
                        await ReplyAsync($"**{Context.User.Username}**, that item can't be used here!");
                        break;
                }
            } else await ReplyAsync($"**{Context.User.Username}**, you don't have this item!");
        } 
    }
}