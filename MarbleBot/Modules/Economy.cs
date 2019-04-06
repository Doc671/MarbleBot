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
                .AddField("Balance", string.Format("<:unitofmoney:372385317581488128>{0:n}", User.Balance), true)
                .AddField("Net Worth", string.Format("<:unitofmoney:372385317581488128>{0:n}", User.NetWorth), true);
            await ReplyAsync(embed: builder.Build());
        }

        [Command("buy")]
        [Alias("buyitem")]
        [Summary("Buys items.")]
        public async Task BuyCommandAsync(string rawID, string rawNo) {
            await Context.Channel.TriggerTypingAsync();
            if (int.TryParse(rawNo, out int noOfItems) && noOfItems > 0) {
                var item = GetItem(rawID);
                if (item.Id == -1) await ReplyAsync(":warning: | Could not find the requested item!");
                else if (item.Id == -2) await ReplyAsync(":warning: | Invalid item ID and/or number of items! Use `mb/help buy` to see how the command works.");
                else if (item.Price == -1) await ReplyAsync("This item cannot be sold!");
                else if (item.OnSale) {
                    string json;
                    using (var users = new StreamReader("Users.json")) json = users.ReadToEnd();
                    var obj = JObject.Parse(json);
                    var user = GetUser(Context, obj);
                    if (user.Balance >= item.Price * noOfItems) {
                        if (user.Items != null) {
                            if (user.Items.ContainsKey(item.Id)) user.Items[item.Id] += noOfItems;
                            else user.Items.Add(item.Id, noOfItems);
                        } else {
                            user.Items = new Dictionary<int, int> {
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
        public async Task CraftCommandAsync(string searchTerm)
        {
            await Context.Channel.TriggerTypingAsync();
            string json;
            using (var users = new StreamReader("Users.json")) json = users.ReadToEnd();
            var obj = JObject.Parse(json);
            var user = GetUser(Context, obj);
            if (user.Items.ContainsKey(17)) {
                var requestedItem = GetItem(searchTerm);
                string json2;
                using (var recipes = new StreamReader("Resources\\Recipes.json")) json2 = await recipes.ReadToEndAsync();
                var rawRecipes = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, int>>>(json2);
                var formattedId = requestedItem.Id.ToString("000");
                if (rawRecipes.ContainsKey(formattedId)) {
                    var correctAmount = true;
                    foreach (var item in rawRecipes[formattedId]) {
                        if (user.Items[int.Parse(item.Key)] < item.Value) correctAmount = false;
                    }
                    if (correctAmount) {
                        var embed = new EmbedBuilder()
                            .WithCurrentTimestamp()
                            .WithColor(GetColor(Context))
                            .WithDescription($"**{Context.User.Username}** has successfully crafted a {requestedItem.Name}!")
                            .WithTitle("Crafting: " + requestedItem.Name);
                        var output = new StringBuilder();
                        var currentNetWorth = user.NetWorth;
                        foreach (var rawItem in rawRecipes[formattedId]) {
                            var item = GetItem(rawItem.Key);
                            output.AppendLine($"{item.Name}: {rawItem.Value}");
                            user.Items[int.Parse(rawItem.Key)] -= rawItem.Value;
                            user.NetWorth -= item.Price;
                        }
                        user.Items[requestedItem.Id] += 1;
                        user.NetWorth += requestedItem.Price;
                        embed.AddField("Lost items", output.ToString())
                            .AddField("Net Worth", $"Old: {Global.UoM}{currentNetWorth}\nNew: {Global.UoM}{user.NetWorth}");
                        WriteUsers(obj, Context.User, user);
                        await ReplyAsync(embed: embed.Build());
                    } else await ReplyAsync($":warning: | **{Context.User.Username}**, you do not have enough items to craft this!");
                } else await ReplyAsync($":warning: | **{Context.User.Username}**, the item {requestedItem.Name} cannot be crafted!");
            } else await ReplyAsync($":warning: | **{Context.User.Username}**, you need a Crafting Station to craft items!");
        }

        [Command("daily")]
        [Summary("Gives daily Units of Money (200 to the power of your streak minus one).")]
        public async Task DailyCommandAsync() {
            await Context.Channel.TriggerTypingAsync();
            string json;
            using (var users = new StreamReader("Users.json")) json = users.ReadToEnd();
            var obj = JObject.Parse(json);
            var user = GetUser(Context, obj);
            if (DateTime.UtcNow.Subtract(user.LastDaily).TotalHours > 24) {
                if (DateTime.UtcNow.Subtract(user.LastDaily).TotalHours > 48) user.DailyStreak = 0;
                decimal gift;
                var power = user.DailyStreak > 100 ? 100 : user.DailyStreak;
                gift = Convert.ToDecimal(Math.Round(Math.Pow(200, 1 + (Convert.ToDouble(power) / 100)), 2));
                var orange = false;
                if (!user.Items.ContainsKey(10) && DateTime.UtcNow.DayOfYear < 51 && DateTime.UtcNow.DayOfYear > 42) {
                    orange = true;
                    user.Items.Add(10, 1);
                }
                user.Balance += gift;
                user.NetWorth += gift;
                user.DailyStreak++;
                user.LastDaily = DateTime.UtcNow;
                WriteUsers(obj, Context.User, user);
                await ReplyAsync(string.Format("**{0}**, you have received <:unitofmoney:372385317581488128>**{1:n}**!\n(Streak: **{2}**)", Context.User.Username, gift, user.DailyStreak));
                if (orange) await ReplyAsync("You have been given a **Qefpedun Charm**!");
            } else {
                var ADayAgo = DateTime.UtcNow.AddDays(-1);
                await ReplyAsync(string.Format("You need to wait for **{0}** until you can get your daily gift again!", GetDateString(user.LastDaily.Subtract(ADayAgo))));
            }
        }

        /*[Command("donate")]
        [Summary("Donate money to others")]
        public async Task _donate(string money, [Remainder] string searchUser = "") {
            if (money == "accept") {

            } else {
                var gift = ulong.Parse(money.Trim());
                string json;
                using (var users = new StreamReader("Users.json")) json = users.ReadToEnd();
                var rawUsers = JsonConvert.DeserializeObject<Dictionary<string, MoneyUser>>(json);
                var search = from user in rawUsers where searchUser.ToLower().Contains(user.Value.Name.ToLower()) || user.Value.Name.ToLower().Contains(searchUser.ToLower()) || searchUser.ToLower().Contains(user.Value.Discriminator) select user;
                var foundUser = search.FirstOrDefault();

            }
        }*/

        [Command("inventory")]
        [Alias("inv", "items")]
        [Summary("Shows all the items a user has.")]
        public async Task InventoryCommandAsync(string searchTerm = "")
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
                        itemOutput.AppendLine($"{GetItem(item.Key.ToString()).Name}: {item.Value}");
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
                var price = item.Price == -1 ? "N/A" : $"{Global.UoM}{item.Price:n}";
                var builder = new EmbedBuilder()
                    .WithColor(GetColor(Context))
                    .WithCurrentTimestamp()
                    .WithDescription(item.Description)
                    .WithTitle(item.Name)
                    .AddField("ID", $"{item.Id:000}", true)
                    .AddField("Price", price, true)
                    .AddField("For Sale", item.OnSale ? "Yes" : "No", true);

                string json;
                using (var recipes = new StreamReader("Resources\\Recipes.json")) json = await recipes.ReadToEndAsync();
                var rawRecipes = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, int>>>(json);
                var formattedId = item.Id.ToString("000");

                if (rawRecipes.ContainsKey(formattedId)) {
                    var output = new StringBuilder();
                    foreach (var rawItem in rawRecipes[formattedId]) {
                        output.AppendLine($"{GetItem(rawItem.Key).Name}: {rawItem.Value}");
                    }
                    builder.AddField("Crafting Recipe", output.ToString());
                }
                await ReplyAsync(embed: builder.Build());
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
                builder.AddField(type + " x" + no, string.Format("Cost: <:unitofmoney:372385317581488128>{0:n}", subtot));
            }
            builder.AddField("Total Cost", string.Format("<:unitofmoney:372385317581488128>{0:n}", totalCost));
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
                    itemOutput.AppendLine($"{GetItem(item.Key.ToString()).Name}: {item.Value}");
                }
            } else itemOutput.Append("None");
            var builder = new EmbedBuilder()
                .WithAuthor(author)
                .WithCurrentTimestamp()
                .WithColor(GetColor(Context))
                .WithFooter("All times in UTC, all dates YYYY-MM-DD.")
                .AddField("Balance", string.Format("<:unitofmoney:372385317581488128>{0:n}", user.Balance), true)
                .AddField("Net Worth", string.Format("<:unitofmoney:372385317581488128>{0:n}", user.NetWorth), true)
                .AddField("Daily Streak", user.DailyStreak, true)
                .AddField("Siege Mentions", user.SiegePing, true)
                .AddField("Race Wins", user.RaceWins, true)
                .AddField("Siege Wins", user.SiegeWins, true)
                .AddField("Last Daily", lastDaily, true)
                .AddField("Last Race Win", lastRaceWin, true)
                .AddField("Last Siege Win", lastSiegeWin, true)
                .AddField("Items", itemOutput.ToString());
            await ReplyAsync(embed: builder.Build());
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
                if (yourPos != 0) builder.WithFooter(string.Format("Requested by {2}#{3} ({0}{1})", yourPos, yourPos.Ordinal(), Context.User.Username, Context.User.Discriminator), Context.User.GetAvatarUrl());
                else builder.WithFooter(string.Format("Requested by {0}#{1}", Context.User.Username, Context.User.Discriminator));
                await ReplyAsync(embed: builder.Build());
            }
        }

        [Command("sell")]
        [Alias("sellitem")]
        [Summary("Sells items.")]
        public async Task SellCommandAsync(string rawID, string rawNo) {
            await Context.Channel.TriggerTypingAsync();
            if (int.TryParse(rawNo, out int noOfItems) && noOfItems > 0) {
                var item = GetItem(rawID);
                if (item.Id == -1) await ReplyAsync(":warning: | Could not find the requested item!");
                else if (item.Id == -2) await ReplyAsync(":warning: | Invalid item ID and/or number of items! Use `mb/help buy` to see how the command works.");
                else if (item.Price == -1) await ReplyAsync("This item cannot be sold!");
                else {
                    string json;
                    using (var users = new StreamReader("Users.json")) json = users.ReadToEnd();
                    var obj = JObject.Parse(json);
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
        [Alias("items", "store")]
        [Summary("Shows all items available for sale, their IDs and their prices.")]
        public async Task ShopCommandAsync() {
            await Context.Channel.TriggerTypingAsync();
            var output = new StringBuilder();
            using (var itemFile = new StreamReader("Resources\\ShopItems.csv")){
                while (!itemFile.EndOfStream) {
                    var properties = (await itemFile.ReadLineAsync()).Split(',');
                    if (bool.Parse(properties[4]) == true) output.AppendLine(string.Format("`[{0:000}]` **{1}** - <:unitofmoney:372385317581488128>**{2:n}**", properties[0].ToInt(), properties[1], properties[2].ToDecimal()));
                }
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
        [Summary("Not implemented yet.")]
        public async Task UseCommandAsync(string searchTerm)
        {
            await ReplyAsync("");
        }
    }
}