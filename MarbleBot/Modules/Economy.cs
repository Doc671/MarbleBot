using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using MarbleBot.BaseClasses;
using MarbleBot.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MarbleBot.Modules {
    public class Economy : ModuleBase<SocketCommandContext> {
        /// <summary>
        /// Commands related to currency
        /// </summary>

        [Command("balance")]
        [Alias("credits", "money")]
        [Summary("Check your balance or the balance of someone else")]
        public async Task _balance([Remainder] string searchTerm = "") {
            await Context.Channel.TriggerTypingAsync();
            var User = new MBUser();
            var id = Context.User.Id;
            if (searchTerm.IsEmpty()) User = Global.GetUser(Context);
            else {
                var json = "";
                using (var users = new StreamReader("Users.json")) json = users.ReadToEnd();
                var rawUsers = JsonConvert.DeserializeObject<Dictionary<string, MBUser>>(json);
                var search = from user in rawUsers where searchTerm.ToLower().Contains(user.Value.Name.ToLower()) || user.Value.Name.ToLower().Contains(searchTerm.ToLower()) || searchTerm.ToLower().Contains(user.Value.Discriminator) select user;
                var foundUser = search.FirstOrDefault();
                id = ulong.Parse(foundUser.Key);
                User = foundUser.Value;
            }
            var author = Context.Client.GetUser(id);
            var builder = new EmbedBuilder()
                .WithAuthor(author)
                .WithCurrentTimestamp()
                .WithColor(Global.GetColor(Context))
                .AddField("Balance", string.Format("<:unitofmoney:372385317581488128>{0:n}", User.Balance), true)
                .AddField("Net Worth", string.Format("<:unitofmoney:372385317581488128>{0:n}", User.NetWorth), true);
            await ReplyAsync("", false, builder.Build());
        }

        [Command("buy")]
        [Summary("Buy items")]
        public async Task _buy(string rawID, string rawNo) {
            await Context.Channel.TriggerTypingAsync();
            if (int.TryParse(rawNo, out int noOfItems) && noOfItems > 0) {
                var item = Global.GetItem(rawID);
                if (item.Id == -1) await ReplyAsync(":warning: | Could not find the requested item!");
                else if (item.Id == -2) await ReplyAsync(":warning: | Invalid item ID and/or number of items! Use `mb/help buy` to see how the command works.");
                else if (item.Price == -1) await ReplyAsync("This item cannot be sold!");
                else if (item.OnSale) {
                    var json = "";
                    using (var users = new StreamReader("Users.json")) json = users.ReadToEnd();
                    var obj = JObject.Parse(json);
                    var user = Global.GetUser(Context, obj);
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
                        obj.Remove(Context.User.Id.ToString());
                        obj.Add(new JProperty(Context.User.Id.ToString(), JObject.FromObject(user)));
                        using (var users = new StreamWriter("Users.json")) {
                            using (var users2 = new JsonTextWriter(users)) {
                                var Serialiser = new JsonSerializer() { Formatting = Formatting.Indented };
                                Serialiser.Serialize(users2, obj);
                            }
                        }
                        await ReplyAsync(string.Format("**{0}** has successfully purchased **{1}** x**{2}** for <:unitofmoney:372385317581488128>**{3:n}** each!\nTotal price: <:unitofmoney:372385317581488128>**{4:n}**\nNew balance: <:unitofmoney:372385317581488128>**{5:n}**.", user.Name, item.Name, noOfItems, item.Price, item.Price * noOfItems, user.Balance));
                    }
                    else await ReplyAsync(":warning: | You can't afford this!");
                }
                else await ReplyAsync(":warning: | This item is not on sale!");
            } else await ReplyAsync(":warning: | Invalid item ID and/or number of items! Use `mb/help buy` to see how the command works.");
        }

        [Command("daily")]
        [Summary("Get daily money")]
        public async Task _daily() {
            await Context.Channel.TriggerTypingAsync();
            var json = "";
            using (var users = new StreamReader("Users.json")) json = users.ReadToEnd();
            var obj = JObject.Parse(json);
            var User = Global.GetUser(Context, obj);
            if (DateTime.UtcNow.Subtract(User.LastDaily).TotalHours > 24) {
                if (DateTime.UtcNow.Subtract(User.LastDaily).TotalHours > 48) User.DailyStreak = 0;
                decimal gift;
                gift = Convert.ToDecimal(Math.Round(Math.Pow(200, 1 + (Convert.ToDouble(User.DailyStreak) / 100)), 2));
                User.Balance += gift;
                User.NetWorth += gift;
                User.DailyStreak++;
                User.LastDaily = DateTime.UtcNow;
                var orange = false;
                if (!User.Items.ContainsKey(10) && DateTime.UtcNow.DayOfYear < 51 && DateTime.UtcNow.DayOfYear > 42) {
                    orange = true;
                    User.Items.Add(10, 1);
                }
                obj.Remove(Context.User.Id.ToString());
                obj.Add(new JProperty(Context.User.Id.ToString(), JObject.FromObject(User)));
                using (var users = new StreamWriter("Users.json")) {
                    using (var users2 = new JsonTextWriter(users)) {
                        var Serialiser = new JsonSerializer() { Formatting = Formatting.Indented };
                        Serialiser.Serialize(users2, obj);
                    }
                }
                await ReplyAsync(string.Format("**{0}**, you have received <:unitofmoney:372385317581488128>**{1:n}**!\n(Streak: **{2}**)", Context.User.Username, gift, User.DailyStreak));
                if (orange) await ReplyAsync("You have been given a **Qefpedun Charm**!");
            } else {
                var ADayAgo = DateTime.UtcNow.AddDays(-1);
                await ReplyAsync(string.Format("You need to wait for **{0}** until you can get your daily gift again!", Global.GetDateString(User.LastDaily.Subtract(ADayAgo))));
            }
        }

        /*[Command("donate")]
        [Summary("Donate money to others")]
        public async Task _donate(string money, [Remainder] string searchUser = "") {
            if (money == "accept") {

            } else {
                var gift = ulong.Parse(money.Trim());
                var json = "";
                using (var users = new StreamReader("Users.json")) json = users.ReadToEnd();
                var rawUsers = JsonConvert.DeserializeObject<Dictionary<string, MoneyUser>>(json);
                var search = from user in rawUsers where searchUser.ToLower().Contains(user.Value.Name.ToLower()) || user.Value.Name.ToLower().Contains(searchUser.ToLower()) || searchUser.ToLower().Contains(user.Value.Discriminator) select user;
                var foundUser = search.FirstOrDefault();

            }
        }*/

        [Command("item")]
        [Summary("View info about an item")]
        public async Task _item([Remainder] string searchTerm) {
            await Context.Channel.TriggerTypingAsync();
            var item = Global.GetItem(searchTerm);
            if (item.Id == -1) await ReplyAsync(":warning: | Could not find the requested item!");
            else if (item.Id == -2) await ReplyAsync(":warning: | Invalid item ID and/or number of items! Use `mb/help buy` to see how the command works.");
            else {
                var price = item.Price == -1 ? "N/A: Event" : string.Format("<:unitofmoney:372385317581488128>{0:n}", item.Price);
                var builder = new EmbedBuilder()
                    .WithColor(Global.GetColor(Context))
                    .WithCurrentTimestamp()
                    .WithDescription(item.Description)
                    .WithTitle(item.Name)
                    .AddField("ID", string.Format("{0:000}", item.Id), true)
                    .AddField("Price", price, true)
                    .AddField("For Sale", item.OnSale ? "Yes" : "No", true);
                await ReplyAsync("", false, builder.Build());
            }
        }

        [Command("poupsoop")]
        [Summary("Calculates total value of Poup Soop")]
        public async Task _poupsoop([Remainder] string msg) {
            await Context.Channel.TriggerTypingAsync();
            var splitMsg = msg.Split('|');
            var totalCost = 0m;
            var builder = new EmbedBuilder()
                .WithColor(Global.GetColor(Context))
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
            await ReplyAsync("", false, builder.Build());
        }

        [Command("profile")]
        [Alias("stats")]
        [Summary("Check your profile or the profile of someone else")]
        public async Task _profile([Remainder] string searchTerm = "") {
            await Context.Channel.TriggerTypingAsync();
            var User = new MBUser();
            var id = Context.User.Id;
            if (searchTerm.IsEmpty()) User = Global.GetUser(Context);
            else {
                var json = "";
                using (var users = new StreamReader("Users.json")) json = users.ReadToEnd();
                var rawUsers = JsonConvert.DeserializeObject<Dictionary<string, MBUser>>(json);
                var search = from user in rawUsers where searchTerm.ToLower().Contains(user.Value.Name.ToLower()) || user.Value.Name.ToLower().Contains(searchTerm.ToLower()) || searchTerm.ToLower().Contains(user.Value.Discriminator) select user;
                var foundUser = search.FirstOrDefault();
                id = ulong.Parse(foundUser.Key);
                User = foundUser.Value;
            }
            var lastDaily = User.LastDaily.ToString();
            if (User.LastDaily.ToString("dd/MM/yyyy") == "01/01/2019") lastDaily = "N/A";
            var lastRaceWin = User.LastRaceWin.ToString();
            if (User.LastRaceWin.ToString("dd/MM/yyyy") == "01/01/2019") lastRaceWin = "N/A";
            var author = Context.Client.GetUser(id);
            var itemOutput = new StringBuilder();
            if (User.Items.Count > 0) {
                foreach (var item in User.Items) {
                    itemOutput.AppendLine($"{Global.GetItem(item.Key.ToString()).Name}: {item.Value}");
                }
            } else itemOutput.Append("None");
            var builder = new EmbedBuilder()
                .WithAuthor(author)
                .WithCurrentTimestamp()
                .WithColor(Global.GetColor(Context))
                .WithFooter("All times in UTC, all dates DD/MM/YYYY.")
                .AddField("Balance", string.Format("<:unitofmoney:372385317581488128>{0:n}", User.Balance), true)
                .AddField("Net Worth", string.Format("<:unitofmoney:372385317581488128>{0:n}", User.NetWorth), true)
                .AddField("Daily Streak", User.DailyStreak, true)
                .AddField("Race Wins", User.RaceWins, true)
                .AddField("Siege Wins", User.SiegeWins, true)
                .AddField("Last Daily", lastDaily, true)
                .AddField("Last Race Win", lastRaceWin, true)
                .AddField("Last Siege Win", User.LastSiegeWin, true)
                .AddField("Items", itemOutput.ToString());
            await ReplyAsync("", false, builder.Build());
        }

        [Command("richlist")]
        [Alias("richest", "top10", "leaderboard")]
        [Summary("Shows the top 10 richest people")]
        public async Task _richlist(string rawNo = "1") {
            await Context.Channel.TriggerTypingAsync();
            if (!int.TryParse(rawNo, out int no)) await ReplyAsync("This is not a valid integer!");
            else {
                var json = "";
                using (var userFile = new StreamReader("Users.json")) json = userFile.ReadToEnd();
                var rawUsers = JsonConvert.DeserializeObject<Dictionary<string, MBUser>>(json);
                var users = new List<Tuple<string, MBUser>>();
                foreach (var user in rawUsers) users.Add(Tuple.Create(user.Key, user.Value));
                var richList = (from user in users orderby user.Item2.NetWorth descending select user.Item2).ToList();
                int i = 1, j = 1, yourPos = 0, minValue = (no - 1) * 10 + 1, maxValue = no * 10;
                var output = new StringBuilder();
                foreach (var user in richList) {
                    if (i < maxValue + 1 && i >= minValue) {
                        output.Append(string.Format("**{0}{1}:** {2}#{3} - <:unitofmoney:372385317581488128>**{4:n}**\n", i, i.Ordinal(), user.Name, user.Discriminator, user.NetWorth));
                        if (j < richList.Count) if (richList[j].NetWorth != user.NetWorth) i++;
                        if (user.Name == Context.User.Username && user.Discriminator == Context.User.Discriminator) yourPos = i - 1;
                    } else {
                        if (yourPos != 0) break;
                        else if (user.Name == Context.User.Username && user.Discriminator == Context.User.Discriminator) yourPos = i - 1; break;
                    }
                    if (!(i < maxValue + 1) && i >= minValue) i++;
                    j++;
                }
                var builder = new EmbedBuilder()
                    .WithColor(Global.GetColor(Context))
                    .WithCurrentTimestamp()
                    .WithTitle("Top 10 Richest Users")
                    .WithDescription(output.ToString());
                if (yourPos != 0) builder.WithFooter(string.Format("Requested by {2}#{3} ({0}{1})", yourPos, yourPos.Ordinal(), Context.User.Username, Context.User.Discriminator), Context.User.GetAvatarUrl());
                else builder.WithFooter(string.Format("Requested by {0}#{1}", Context.User.Username, Context.User.Discriminator));
                await ReplyAsync("", false, builder.Build());
            }
        }

        [Command("sell")]
        [Summary("Sell items")]
        public async Task _sell(string rawID, string rawNo) {
            await Context.Channel.TriggerTypingAsync();
            if (int.TryParse(rawNo, out int noOfItems) && noOfItems > 0) {
                var item = Global.GetItem(rawID);
                if (item.Id == -1) await ReplyAsync(":warning: | Could not find the requested item!");
                else if (item.Id == -2) await ReplyAsync(":warning: | Invalid item ID and/or number of items! Use `mb/help buy` to see how the command works.");
                else if (item.Price == -1) await ReplyAsync("This item cannot be sold!");
                else {
                    var json = "";
                    using (var users = new StreamReader("Users.json")) json = users.ReadToEnd();
                    var obj = JObject.Parse(json);
                    var user = Global.GetUser(Context, obj);
                    if (user.Items.ContainsKey(item.Id) && user.Items[item.Id] >= noOfItems) {
                        user.Items[item.Id] -= noOfItems;
                        user.Balance += item.Price * noOfItems;
                        obj.Remove(Context.User.Id.ToString());
                        obj.Add(new JProperty(Context.User.Id.ToString(), JObject.FromObject(user)));
                        using (var users = new StreamWriter("Users.json")) {
                            using (var users2 = new JsonTextWriter(users)) {
                                var Serialiser = new JsonSerializer() { Formatting = Formatting.Indented };
                                Serialiser.Serialize(users2, obj);
                            }
                        }
                        await ReplyAsync(string.Format("**{0}** has successfully sold **{1}** x**{2}** for <:unitofmoney:372385317581488128>**{3:n}** each!\nTotal price: <:unitofmoney:372385317581488128>**{4:n}**\nNew balance: <:unitofmoney:372385317581488128>**{5:n}**.", user.Name, item.Name, noOfItems, item.Price, item.Price * noOfItems, user.Balance));
                    } else await ReplyAsync(":warning: | You don't have enough of this item!");
                }
            } else await ReplyAsync(":warning: | Invalid item ID and/or number of items! Use `mb/help sell` to see how the command works.");
        }

        [Command("shop")]
        [Alias("items")]
        [Summary("Shows all available items")]
        public async Task _shop() {
            await Context.Channel.TriggerTypingAsync();
            var output = new StringBuilder();
            using (var itemFile = new StreamReader("ShopItems.csv")){
                while (!itemFile.EndOfStream) {
                    var properties = (await itemFile.ReadLineAsync()).Split(',');
                    if (bool.Parse(properties[4]) == true) output.AppendLine(string.Format("`[{0:000}]` **{1}** - <:unitofmoney:372385317581488128>**{2:n}**", properties[0].ToInt(), properties[1], properties[2].ToDecimal()));
                }
            }
            var builder = new EmbedBuilder()
                .WithColor(Global.GetColor(Context))
                .WithCurrentTimestamp()
                .WithDescription(output.ToString())
                .WithTitle("All items for sale");
            await ReplyAsync("", false, builder.Build());
        }
    }
}