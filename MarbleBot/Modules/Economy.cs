using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using MarbleBot.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MarbleBot.Modules {
    public class Economy: ModuleBase<SocketCommandContext> {
        /// <summary>
        /// Commands related to currency
        /// </summary>

        [Command("balance")]
        [Summary("Check your balance or the balance of someone else")]
        public async Task _balance([Remainder] string searchTerm = "") {
            await Context.Channel.TriggerTypingAsync();
            var User = new MoneyUser();
            var id = Context.User.Id;
            if (searchTerm.IsEmpty()) User = Global.GetUser(Context);
            else {
                var json = "";
                using (var users = new StreamReader("Users.json")) json = users.ReadToEnd();
                var rawUsers = JsonConvert.DeserializeObject<Dictionary<string, MoneyUser>>(json);
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
            var builder = new EmbedBuilder()
                .WithAuthor(author)
                .WithCurrentTimestamp()
                .WithColor(Global.GetColor(Context))
                .WithFooter("All times in UTC, all dates DD/MM/YYYY.")
                .AddInlineField("Balance", string.Format("<:unitofmoney:372385317581488128>{0:n}", User.Balance))
                .AddInlineField("Net Worth", string.Format("<:unitofmoney:372385317581488128>{0:n}", User.NetWorth))
                .AddInlineField("Daily Streak", User.DailyStreak)
                .AddInlineField("Last Daily", lastDaily)
                .AddInlineField("Last Race Win", lastRaceWin);
            await ReplyAsync("", false, builder.Build());
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
                var gift = Convert.ToDecimal(Math.Round(Math.Pow(200, (1 + (Convert.ToDouble(User.DailyStreak) / 100))), 2));
                User.Balance += gift;
                User.NetWorth += gift;
                if (User.LastDaily.Subtract(DateTime.UtcNow).TotalHours < 48) User.DailyStreak++;
                else User.DailyStreak = 1;
                User.LastDaily = DateTime.UtcNow;
                obj.Remove(Context.User.Id.ToString());
                obj.Add(new JProperty(Context.User.Id.ToString(), JObject.FromObject(User)));
                using (var users = new StreamWriter("Users.json")) {
                    using (var users2 = new JsonTextWriter(users)) {
                        var Serialiser = new JsonSerializer() { Formatting = Formatting.Indented };
                        Serialiser.Serialize(users2, obj);
                    }
                }
                await ReplyAsync(string.Format("**{0}**, you have received <:unitofmoney:372385317581488128>**{1}**!\n(Streak: **{2}**)", Context.User.Username, gift, User.DailyStreak));
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

        [Command("richlist")]
        [Summary("Shows the top 10 richest people")]
        public async Task _richlist() {
            await Context.Channel.TriggerTypingAsync();
            var json = "";
            using (var userFile = new StreamReader("Users.json")) json = userFile.ReadToEnd();
            var rawUsers = JsonConvert.DeserializeObject<Dictionary<string, MoneyUser>>(json);
            var users = new List<Tuple<string, MoneyUser>>();
            foreach (var user in rawUsers) users.Add(Tuple.Create(user.Key, user.Value));
            var richList = (from user in users orderby user.Item2.NetWorth descending select user.Item2).ToList();
            int i = 1, j = 1;
            var output = new StringBuilder();
            foreach (var user in richList) {
                if (i < 11) {
                    output.Append(string.Format("**{0}{1}:** {2}#{3} - <:unitofmoney:372385317581488128>**{4}**\n", i, i.Ordinal(), user.Name, user.Discriminator, string.Format("{0:n}", user.NetWorth)));
                    if (j < richList.Count) if (richList[j].NetWorth != user.NetWorth) i++;
                    j++;
                } else break;
            }
            var builder = new EmbedBuilder()
                .WithColor(Global.GetColor(Context))
                .WithCurrentTimestamp()
                .WithTitle("Top 10 Richest Users")
                .WithDescription(output.ToString());
            await ReplyAsync("", false, builder.Build());
        }
    }
}