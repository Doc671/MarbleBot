using Discord;
using Discord.Commands;
using MarbleBot.Common;
using MarbleBot.Common.Games;
using MarbleBot.Extensions;
using MarbleBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarbleBot.Modules
{
    [Summary("Commands relating to currency and items.")]
    public class Economy : MarbleBotModule
    {
        private readonly DailyTimeoutService _dailyTimeoutService;

        public Economy(DailyTimeoutService dailyTimeoutService)
        {
            _dailyTimeoutService = dailyTimeoutService;
        }

        [Command("balance")]
        [Alias("credits", "money", "bal")]
        [Summary("Returns how much money you or someone else has.")]
        public async Task BalanceCommand([Remainder] MarbleBotUser? user = null)
        {
            user ??= MarbleBotUser.Find(Context);
            await ReplyAsync(embed: new EmbedBuilder()
                .WithAuthor(Context.Client.GetUser(user.Id))
                .WithColor(GetColor(Context))
                .AddField("Balance", $"{UnitOfMoney}{user.Balance:n2}", true)
                .AddField("Net Worth", $"{UnitOfMoney}{user.NetWorth:n2}", true)
                .Build());
        }

        [Command("buy")]
        [Alias("buyitem")]
        [Summary("Buys items.")]
        public async Task BuyCommand(Item item, int noOfItems = 1)
        {
            if (noOfItems < 1)
            {
                await SendErrorAsync($"**{Context.User.Username}**, invalid number of items! Use `mb/help buy` to see how the command works.");
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

            MarbleBotUser user = MarbleBotUser.Find(Context);
            if (user.Balance >= item.Price * noOfItems)
            {
                if (user.Items.ContainsKey(item.Id))
                {
                    user.Items[item.Id] += noOfItems;
                }
                else
                {
                    user.Items.Add(item.Id, noOfItems);
                }

                user.Balance -= item.Price * noOfItems;
                MarbleBotUser.UpdateUser(user);
                await ReplyAsync($"**{user.Name}** has successfully purchased **{item.Name}** x**{noOfItems}** for {UnitOfMoney}**{item.Price:n2}** each!\nTotal price: {UnitOfMoney}**{item.Price * noOfItems:n2}**\nNew balance: {UnitOfMoney}**{user.Balance:n2}**.");
            }
            else
            {
                await SendErrorAsync("You can't afford this!");
            }
        }

        [Command("craft")]
        [Summary("Crafts an item out of other items.")]
        public async Task CraftCommand(Item requestedItem, int noOfItems = 1)
        {
            MarbleBotUser user = MarbleBotUser.Find(Context);
            if (!user.Items.ContainsKey(17) && !user.Items.ContainsKey(62))
            {
                await SendErrorAsync($"**{Context.User.Username}**, you need a Crafting Station to craft items!");
                return;
            }

            if (requestedItem.CraftingStationRequired == 2 && !user.Items.ContainsKey(62))
            {
                await SendErrorAsync($"**{Context.User.Username}**, your current Crafting Station cannot craft this item!");
                return;
            }

            if (requestedItem.CraftingRecipe == null)
            {
                await SendErrorAsync($"**{Context.User.Username}**, the item **{requestedItem.Name}** cannot be crafted!");
                return;
            }

            bool sufficientMaterials = requestedItem.CraftingRecipe
                .All(itemPair => user.Items.ContainsKey(itemPair.Key)
                                 && itemPair.Value * noOfItems <= user.Items[itemPair.Key]);

            if (!sufficientMaterials)
            {
                await SendErrorAsync($"**{Context.User.Username}**, you do not have enough items to craft this!");
                return;
            }

            int noCrafted = requestedItem.CraftingProduced * noOfItems;
            var output = new StringBuilder();
            decimal currentNetWorth = user.NetWorth;
            foreach ((int itemId, int noRequired) in requestedItem.CraftingRecipe)
            {
                var item = Item.Find<Item>(itemId);
                int noLost = noRequired * noOfItems;
                output.AppendLine($"`[{item.Id:000}]` {item.Name}: {noLost}");
                user.Items[itemId] -= noLost;
                user.NetWorth -= item.Price * noOfItems;
            }

            if (!user.Items.ContainsKey(requestedItem.Id))
            {
                user.Items.Add(requestedItem.Id, noCrafted);
            }
            else
            {
                user.Items[requestedItem.Id] += noCrafted;
            }

            user.NetWorth += requestedItem.Price * noCrafted;
            MarbleBotUser.UpdateUser(user);
            await ReplyAsync(embed: new EmbedBuilder()
                .WithColor(GetColor(Context))
                .WithDescription($"**{Context.User.Username}** has successfully crafted **{requestedItem.Name}** x**{noCrafted}**!")
                .WithTitle("Crafting: " + requestedItem.Name)
                .AddField("Lost items", output.ToString())
                .AddField("Net Worth",
                    $"Old: {UnitOfMoney}**{currentNetWorth:n2}**\nNew: {UnitOfMoney}**{user.NetWorth:n2}**")
                .Build());
        }

        [Command("craftable")]
        [Summary("Returns a list of all items that can be crafted with the user's inventory.")]
        public async Task CraftableCommand()
        {
            int currentPart = 0;
            var embed = new EmbedBuilder();
            MarbleBotUser user = MarbleBotUser.Find(Context);
            var output = new StringBuilder();
            IDictionary<int, Item> itemsDict = Item.GetItems();

            foreach ((int itemId, Item item) in itemsDict)
            {
                if (item.CraftingRecipe != null)
                {
                    bool craftable = true;
                    int noCraftable = 0;
                    foreach ((int ingredientId, int ingredientNoOwned) in item.CraftingRecipe)
                    {
                        if (!user.Items.ContainsKey(ingredientId) || user.Items[ingredientId] < ingredientNoOwned)
                        {
                            craftable = false;
                            break;
                        }

                        noCraftable = noCraftable == 0
                            ? user.Items[ingredientId] / ingredientNoOwned
                            : Math.Min(user.Items[ingredientId] / ingredientNoOwned, noCraftable);
                    }

                    if (craftable)
                    {
                        string itemInfo = $"`[{itemId:000}]` {item.Name}: {noCraftable}";
                        if (output.Length + itemInfo.Length > EmbedFieldBuilder.MaxFieldValueLength)
                        {
                            currentPart++;
                            embed.AddField($"Part {currentPart}", output.ToString());
                            output = new StringBuilder(itemInfo);
                        }
                        else
                        {
                            output.AppendLine(itemInfo);
                        }
                    }
                }
            }

            if (output.Length == 0)
            {
                output.Append("There are no items you can craft!");
            }

            if (embed.Fields.Count == 0)
            {
                embed.WithDescription(output.ToString());
            }
            else
            {
                currentPart++;
                embed.AddField($"Part {currentPart}", output.ToString());
            }

            await ReplyAsync(embed: embed
                .WithAuthor(Context.User)
                .WithColor(GetColor(Context))
                .WithTitle("Craftable items")
                .Build());
        }

        [Command("daily")]
        [Summary("Gives daily Units of Money (200 to the power of (your streak / 100 - 1)).")]
        public async Task DailyCommand()
        {
            MarbleBotUser user = MarbleBotUser.Find(Context);
            if ((DateTime.UtcNow - user.LastDaily).TotalHours < 24)
            {
                DateTime aDayAgo = DateTime.UtcNow.AddDays(-1);
                await ReplyAsync($"You need to wait for {GetTimeSpanSentence(user.LastDaily - aDayAgo)} until you can get your daily gift again!");
                return;
            }

            if ((DateTime.UtcNow - user.LastDaily).TotalHours > _dailyTimeoutService.DailyTimeout)
            {
                user.DailyStreak = 0;
            }

            int power = user.DailyStreak > 100 ? 100 : user.DailyStreak;
            decimal gift = (decimal)MathF.Round(MathF.Pow(200f, 1f + power / 100f), 2);
            bool giveCraftingStation = user.DailyStreak > 2 && !user.Items.ContainsKey(17);

            user.Balance += gift;
            user.NetWorth += gift;
            user.DailyStreak++;
            user.LastDaily = DateTime.UtcNow;

            if (giveCraftingStation)
            {
                user.Items.Add(17, 1);
            }

            bool giveQefpedunCharm = false;
            if (!user.Items.ContainsKey(10) && DateTime.UtcNow.DayOfYear < 51 && DateTime.UtcNow.DayOfYear > 42)
            {
                giveQefpedunCharm = true;
                user.Items.Add(10, 1);
            }

            MarbleBotUser.UpdateUser(user);
            await ReplyAsync($"**{Context.User.Username}**, you have received {UnitOfMoney}**{gift:n2}**!\n(Streak: **{user.DailyStreak}**)");

            if (giveCraftingStation)
            {
                await ReplyAsync("You have been given a **Crafting Station Mk.I**!");
            }

            if (giveQefpedunCharm)
            {
                await ReplyAsync("You have been given a **Qefpedun Charm**!");
            }
        }

        [Command("dismantle")]
        [Alias("decraft", "disassemble", "dismantle")]
        [Summary("Turns a crafted item back into its ingredients.")]
        public async Task DecraftCommand(Item requestedItem, int noOfItems = 1)
        {
            MarbleBotUser user = MarbleBotUser.Find(Context);
            if (!user.Items.ContainsKey(17) && !user.Items.ContainsKey(62))
            {
                await SendErrorAsync($"**{Context.User.Username}**, you need a Crafting Station to decraft items!");
                return;
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

            if (requestedItem.CraftingRecipe == null)
            {
                await SendErrorAsync($"**{Context.User.Username}**, the item **{requestedItem.Name}** cannot be decrafted!");
                return;
            }

            if (!user.Items.ContainsKey(requestedItem.Id) || user.Items[requestedItem.Id] < noOfItems)
            {
                await SendErrorAsync($"**{Context.User.Username}**, you do not have enough of this item!");
                return;
            }

            decimal currentNetWorth = user.NetWorth;
            int noCrafted = requestedItem.CraftingProduced * noOfItems;
            var output = new StringBuilder();
            foreach ((int itemId, int noRequired) in requestedItem.CraftingRecipe)
            {
                var item = Item.Find<Item>(itemId);
                int noGained = noRequired * noOfItems;
                output.AppendLine($"`[{item.Id:000}]` {item.Name}: {noGained}");
                if (user.Items.ContainsKey(item.Id))
                {
                    user.Items[itemId] += noGained;
                }
                else
                {
                    user.Items.Add(item.Id, noGained);
                }

                user.NetWorth += item.Price * noOfItems;
            }

            user.Items[requestedItem.Id] -= noCrafted;
            user.NetWorth -= requestedItem.Price * noCrafted;
            MarbleBotUser.UpdateUser(user);
            await ReplyAsync(embed: new EmbedBuilder()
                .WithColor(GetColor(Context))
                .WithDescription($"**{Context.User.Username}** has successfully decrafted **{requestedItem.Name}** x**{noCrafted}**!")
                .WithTitle($"Crafting: {requestedItem.Name}")
                .AddField("Gained items", output.ToString())
                .AddField("Net Worth",
                    $"Old: {UnitOfMoney}**{currentNetWorth:n2}**\nNew: {UnitOfMoney}**{user.NetWorth:n2}**")
                .Build());
        }

        [Command("inventory")]
        [Alias("inv", "items")]
        [Summary("Shows all the items a user has.")]
        public async Task InventoryCommand(MarbleBotUser marbleBotUser, int page = 1)
        {
            if (page < 1)
            {
                await SendErrorAsync($"**{Context.User.Username}**, the inventory page must be at least one!");
                return;
            }

            await ShowUserInventory(Context.Client.GetUser(marbleBotUser.Id), marbleBotUser, page);
        }

        [Command("inventory")]
        [Alias("inv", "items")]
        [Summary("Shows all the items a user has.")]
        [Priority(1)]
        public async Task InventoryCommand(int page = 1)
        {
            if (page < 1)
            {
                await SendErrorAsync($"**{Context.User.Username}**, the inventory page must be at least one!");
                return;
            }

            await ShowUserInventory(Context.User, MarbleBotUser.Find(Context), page);
        }

        private async Task ShowUserInventory(IUser discordUser, MarbleBotUser marbleBotUser, int page = 1)
        {
            if (marbleBotUser.Items.Count == 0)
            {
                await SendErrorAsync($"**{Context.User.Username}**, you don't have any items!");
                return;
            }

            var itemOutput = new StringBuilder();
            var items = marbleBotUser.Items.Skip((page - 1) * 20).Take(20).ToArray();
            bool itemsPresent = items.Any();
            if (itemsPresent)
            {
                foreach ((int itemId, int noOwned) in items)
                {
                    if (noOwned > 0)
                    {
                        itemOutput.AppendLine($"`[{itemId:000}]` {Item.Find<Item>(itemId.ToString("000")).Name}: {noOwned}");
                    }
                }
            }
            else
            {
                itemOutput.Append($"**{Context.User.Username}**, there are no items on page **{page}**!");
            }

            await ReplyAsync(embed: new EmbedBuilder()
                .WithAuthor(discordUser)
                .WithColor(GetColor(Context))
                .WithDescription(itemOutput.ToString())
                .WithTitle(itemsPresent
                    ? $"Page **{page}** of **{(marbleBotUser.Items.Count - 1) / 20 + 1}**"
                    : "Invalid page")
                .Build());
        }

        [Command("item")]
        [Alias("iteminfo")]
        [Summary("Returns information about an item.")]
        public async Task ItemCommand([Remainder] Item item)
        {
            MarbleBotUser user = MarbleBotUser.Find(Context);
            if (item.Stage > user.Stage)
            {
                await ReplyAsync(embed: new EmbedBuilder()
                    .WithColor(GetColor(Context))
                    .WithDescription($"{StageTooHighString()}\n\nYou are unable to view information about this item!")
                    .WithTitle(item.Name)
                    .Build());
                return;
            }

            string price = item.Price == -1 ? "N/A" : $"{UnitOfMoney}{item.Price:n2}";
            var builder = new EmbedBuilder()
                .WithColor(GetColor(Context))
                .WithDescription(item.Description)
                .WithTitle(item.Name)
                .AddField("ID", $"{item.Id:000}", true)
                .AddField("Price", price, true)
                .AddField("For Sale", item.OnSale ? "Yes" : "No", true)
                .AddField("Scavenge Location", item.ScavengeLocation, true);

            if (user.Stage > 1)
            {
                builder.AddField("Stage", item.Stage, true);
            }

            switch (item)
            {
                case Weapon weapon:
                {
                    builder.AddField("Weapon Info", new StringBuilder()
                        .AppendLine($"Class: **{weapon.WeaponClass}**")
                        .AppendLine($"Accuracy: **{weapon.Accuracy}**%")
                        .AppendLine($"Damage: **{weapon.Damage}**")
                        .AppendLine($"Uses: **{weapon.Hits}**"), true);

                    if (weapon.Ammo.Length != 0)
                    {
                        var output = new StringBuilder();
                        foreach (int ammoId in weapon.Ammo)
                        {
                            output.AppendLine($"`[{ammoId:000}]` {Item.Find<Ammo>(ammoId.ToString("000")).Name}");
                        }

                        builder.AddField("Ammo", output.ToString(), true);
                    }

                    break;
                }
                case Ammo ammo:
                    builder.AddField("Ammo Damage", ammo.Damage, true);
                    break;
                case Spikes spikes:
                    builder.AddField("Spikes Damage Boost", $"{spikes.OutgoingDamageMultiplier}%", true);
                    break;
                case Shield shield:
                    builder.AddField("Shield Damage Absorption:", $"{shield.IncomingDamageMultiplier}%");
                    break;
            }

            if (item.CraftingRecipe != null)
            {
                var output = new StringBuilder();
                foreach ((int ingredientId, int noRequired) in item.CraftingRecipe)
                {
                    output.AppendLine($"`[{ingredientId:000}]` {Item.Find<Item>(ingredientId).Name}: {noRequired}");
                }

                builder.AddField($"Crafting Recipe (produces **{item.CraftingProduced}**)", output.ToString());
            }

            await ReplyAsync(embed: builder.Build());
        }

        [Command("itemlist")]
        [Summary("Gives a link to the item list.")]
        public async Task ItemListCommand()
        {
            await ReplyAsync("https://docs.google.com/spreadsheets/d/1tKT8nFH4Aa1VkH_UeieoOkN_iBAfueZLqLOdHsVTJ1I/edit#gid=0");
        }

        [Command("poupsoop")]
        [Alias("poupsoopcalc", "poupcalc")]
        [Summary("Calculates the total price of Poup Soop.")]
        public async Task PoupSoopCalcCommand([Remainder] string msg)
        {
            string[] splitMsg = msg.Split('|');
            decimal totalCost = 0m;
            var builder = new EmbedBuilder()
                .WithColor(GetColor(Context))
                .WithTitle("Poup Soop Price Calculator");
            decimal[] poupSoopPrices =
            {
                364400, 552387946, 140732609585, 180269042735, 221548933670, 262310854791, 303496572188, 1802201667100,
                374180952623987
            };

            for (int i = 0; i < splitMsg.Length; i++)
            {
                decimal no = decimal.Parse(splitMsg[i]);
                decimal subtotal = no * poupSoopPrices[i];
                totalCost += subtotal;
                string type = i switch
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
                builder.AddField($"{type} x{no}", $"Cost: {UnitOfMoney}{subtotal:n2}");
            }

            builder.AddField("Total Cost", $"{UnitOfMoney}{totalCost:n2}");
            await ReplyAsync(embed: builder.Build());
        }

        [Command("profile")]
        [Alias("stats")]
        [Summary("Returns the profile of you or someone else.")]
        public async Task ProfileCommand([Remainder] MarbleBotUser? user = null)
        {
            user ??= MarbleBotUser.Find(Context);

            string lastDaily = user.LastDaily.ToString("yyyy-MM-dd HH:mm:ss");
            if (user.LastDaily.Year == 1)
            {
                lastDaily = "N/A";
            }

            string lastRaceWin = user.LastRaceWin.ToString("yyyy-MM-dd HH:mm:ss");
            if (user.LastRaceWin.Year == 1)
            {
                lastRaceWin = "N/A";
            }

            string lastScavenge = user.LastScavenge.ToString("yyyy-MM-dd HH:mm:ss");
            if (user.LastScavenge.Year == 1)
            {
                lastScavenge = "N/A";
            }

            string lastSiegeWin = user.LastSiegeWin.ToString("yyyy-MM-dd HH:mm:ss");
            if (user.LastSiegeWin.Year == 1)
            {
                lastSiegeWin = "N/A";
            }

            string lastWarWin = user.LastWarWin.ToString("yyyy-MM-dd HH:mm:ss");
            if (user.LastWarWin.Year == 1)
            {
                lastWarWin = "N/A";
            }

            var builder = new EmbedBuilder()
                .WithAuthor(Context.Client.GetUser(user.Id))
                .WithColor(GetColor(Context))
                .WithFooter("All times in UTC, all dates YYYY-MM-DD.")
                .AddField("Balance", $"{UnitOfMoney}{user.Balance:n2}", true)
                .AddField("Net Worth", $"{UnitOfMoney}{user.NetWorth:n2}", true)
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
                builder.AddField("Stage", user.Stage, true)
                    .AddField("Shield", user.GetShield()?.Name ?? "None", true)
                    .AddField("Spikes", user.GetShield()?.Name ?? "None", true);
            }

            var weaponOutput = new StringBuilder();
            foreach (Item item in user.Items.Select(item => Item.Find<Item>(item.Key)))
            {
                if (item is Weapon weapon)
                {
                    weaponOutput.AppendLine(weapon.ToString());
                }
            }

            if (weaponOutput.Length != 0)
            {
                builder.AddField("Weapons", weaponOutput.ToString());
            }

            await ReplyAsync(embed: builder.Build());
        }

        [Command("recipes")]
        [Summary("Shows all crafting recipes in a range of IDs.")]
        public async Task RecipesCommand(string rawPage = "1")
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithColor(GetColor(Context));
            IDictionary<int, Item> items = Item.GetItems();
            if (!int.TryParse(rawPage, out int page) && page > 0)
            {
                await ReplyAsync("Invalid number! Use `mb/help recipes` for more info.");
                return;
            }

            int minValue = page * 20 - 20;
            KeyValuePair<int, Item> lastItem = items.Last();
            if (minValue > lastItem.Key)
            {
                await ReplyAsync($"The last item has ID `{lastItem}`!");
                return;
            }

            int maxValue = page * 20 - 1;
            embed.WithTitle($"Recipes in IDs `{minValue:000}`-`{maxValue:000}` :notepad_spiral:");
            foreach ((int itemId, Item item) in items)
            {
                if (item.CraftingRecipe != null)
                {
                    if (itemId >= minValue && itemId <= maxValue)
                    {
                        if (item.Stage > MarbleBotUser.Find(Context).Stage)
                        {
                            embed.AddField($"`[{itemId:000}]` {Item.Find<Item>(itemId).Name}",
                                $"{StageTooHighString()}\n\nYou are unable to view information about this item!");
                        }
                        else
                        {
                            var output = new StringBuilder();
                            foreach ((int ingredientId, int noRequired) in item.CraftingRecipe)
                            {
                                output.AppendLine($"`[{ingredientId:000}]` {Item.Find<Item>(ingredientId).Name}: {noRequired}");
                            }

                            embed.AddField($"`[{itemId:000}]` {item.Name} (produces **{item.CraftingProduced}**)",
                                output.ToString());
                        }
                    }

                    if (itemId > maxValue)
                    {
                        break;
                    }
                }
            }

            await ReplyAsync(embed: embed.Build());
        }

        [Command("richlist")]
        [Alias("richest", "leaderboard", "networthleaderboard")]
        [Summary("Shows the ten richest people globally by Net Worth.")]
        public async Task RichListCommand(int page = 1)
        {
            if (page < 1)
            {
                await SendErrorAsync($"**{Context.User.Username}**, the leaderboard page must be at least one!");
                return;
            }

            (int place, MarbleBotUser user)[] users = (from user in MarbleBotUser.GetUsers()
                orderby user.Value.NetWorth descending
                select (place: 0, user: user.Value)).ToArray();

            await DisplayRichList(users, page);
        }

        [Command("richlist local")]
        [Alias("richlist local", "leaderboard local", "networthleaderboard local")]
        [RequireContext(ContextType.Guild)]
        public async Task RichListLocalCommand(int page = 1)
        {
            if (page < 1)
            {
                await SendErrorAsync($"**{Context.User.Username}**, the leaderboard page must be at least one!");
                return;
            }

            (int place, MarbleBotUser user)[] users = (from marbleBotUserPair in MarbleBotUser.GetUsers()
                orderby marbleBotUserPair.Value.NetWorth descending
                where Context.Guild.Users.Any(guildUser => guildUser.Id == marbleBotUserPair.Value.Id)
                select (place: 0, user: marbleBotUserPair.Value)).ToArray();

            await DisplayRichList(users, page);
        }

        private async Task DisplayRichList(IList<(int place, MarbleBotUser user)> users, int page)
        {
            if (page > users.Count / 10 + 1)
            {
                await ReplyAsync($"**{Context.User.Username}**, there is nobody in page **{page}**!");
                return;
            }

            int displayedPlace = 0;
            decimal lastValue = 0m;
            for (int i = 0; i < users.Count; i++)
            {
                (_, MarbleBotUser user) = users[i];
                if (user.NetWorth != lastValue)
                {
                    displayedPlace++;
                }

                lastValue = user.NetWorth;
                users[i] = (displayedPlace, user);
            }

            int maxIndex = page * 10 - 1, minIndex = (page - 1) * 10, currentUserIndex = 0;
            var output = new StringBuilder();
            for (int i = 0; i < users.Count; i++)
            {
                (int place, MarbleBotUser user) = users[i];
                if (i < maxIndex + 1 && i >= minIndex)
                {
                    output.AppendLine($"{place}{place.Ordinal()}: {user.Name}#{user.Discriminator} - {UnitOfMoney}**{user.NetWorth:n2}**");
                }

                if (user.Id == Context.User.Id)
                {
                    currentUserIndex = place;
                }
            }

            var builder = new EmbedBuilder()
                .WithColor(GetColor(Context))
                .WithTitle($"Net Worth Leaderboard: Page {page}")
                .WithDescription(output.ToString());

            if (currentUserIndex != 0)
            {
                builder.WithFooter($"Requested by {Context.User.Username}#{Context.User.Discriminator} ({currentUserIndex}{currentUserIndex.Ordinal()})",
                    Context.User.GetAvatarUrl());
            }
            else
            {
                builder.WithFooter($"Requested by {Context.User.Username}#{Context.User.Discriminator}");
            }

            await ReplyAsync(embed: builder.Build());
        }

        [Command("sell")]
        [Alias("sellitem")]
        [Summary("Sells items.")]
        public async Task SellCommand(Item item, int noOfItems)
        {
            if (noOfItems < 1)
            {
                await SendErrorAsync($"**{Context.User.Username}**, invalid number of items! Use `mb/help sell` to see how the command works.");
                return;
            }

            if (item.Price == -1)
            {
                await SendErrorAsync($"**{Context.User.Username}**, this item cannot be sold!");
                return;
            }

            MarbleBotUser user = MarbleBotUser.Find(Context);
            if (!user.Items.ContainsKey(item.Id) || user.Items[item.Id] < noOfItems)
            {
                await SendErrorAsync("You don't have enough of this item!");
                return;
            }

            user.Balance += item.Price * noOfItems;
            user.Items[item.Id] -= noOfItems;
            MarbleBotUser.UpdateUser(user);
            await ReplyAsync($"**{user.Name}** has successfully sold **{item.Name}** x**{noOfItems}** for {UnitOfMoney}**{item.Price:n2}** each!\nTotal price: {UnitOfMoney}**{item.Price * noOfItems:n2}**\nNew balance: {UnitOfMoney}**{user.Balance:n2}**.");
        }

        [Command("shop")]
        [Alias("store")]
        [Summary("Shows all items available for sale, their IDs and their prices.")]
        public async Task ShopCommand()
        {
            var output = new StringBuilder();
            IDictionary<int, Item> items = Item.GetItems();
            foreach ((int itemId, Item item) in items)
            {
                if (item.OnSale)
                {
                    output.AppendLine($"`[{itemId:000}]` **{item.Name}** - {UnitOfMoney}**{item.Price:n2}**");
                }
            }

            var builder = new EmbedBuilder()
                .AddField("Items for sale", output.ToString())
                .WithColor(GetColor(Context))
                .WithDescription("Use `mb/buy <item ID> <# of items>` to buy an item on sale.")
                .WithTitle("MarbleBot Shop :shopping_bags:");
            await ReplyAsync(embed: builder.Build());
        }
    }
}
