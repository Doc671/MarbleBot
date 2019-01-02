using System;
using System.IO;
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
        [Summary("Check your balance")]
        public async Task _balance() {
            var json = "";
            using (var users = new StreamReader("Users.json")) json = users.ReadToEnd();
            var obj = JObject.Parse(json);
            var builder = new EmbedBuilder()
                .WithAuthor(Context.User)
                .WithCurrentTimestamp()
                .WithColor(Global.GetColor(Context));
            var User = new MoneyUser();
            if (obj.ContainsKey(Context.User.Id.ToString())) {
                User = obj[(Context.User.Id.ToString())].ToObject<MoneyUser>();
            } else {
                User = new MoneyUser() {
                    Name = Context.User.Username,
                    Discriminator = Context.User.DiscriminatorValue,
                    Money = 0,
                    DailyStreak = 0,
                    LastDaily = DateTime.Parse("2019-01-01 00:00:00")
                };
            }
            builder.AddField("Money", "<:unitofmoney:372385317581488128>" + User.Money)
                .AddField("Daily Streak", User.DailyStreak)
                .AddField("Last Daily", User.LastDaily.ToString());
            await ReplyAsync("", false, builder.Build());
        }

        [Command("daily")]
        [Summary("Get daily money")]
        public async Task _daily() {
            var json = "";
            using (var users = new StreamReader("Users.json")) json = users.ReadToEnd();
            var obj = JObject.Parse(json);
            var User = new MoneyUser();
            if (obj.ContainsKey(Context.User.Id.ToString())) {
                User = obj[(Context.User.Id.ToString())].ToObject<MoneyUser>();
            } else {
                User = new MoneyUser() {
                    Name = Context.User.Username,
                    Discriminator = Context.User.DiscriminatorValue,
                    Money = 0,
                    DailyStreak = 0,
                    LastDaily = DateTime.Parse("2019-01-01 00:00:00")
                };
            }
            if (DateTime.UtcNow.Subtract(User.LastDaily).TotalHours > 24) {
                var gift = Convert.ToUInt64(Math.Round(Math.Pow(200, (1 + (Convert.ToDouble(User.DailyStreak) / 100)))));
                User.Money += gift;
                if (User.LastDaily.Subtract(DateTime.UtcNow).TotalHours < 48) User.DailyStreak++;
                else User.DailyStreak = 1;
                User.LastDaily = DateTime.UtcNow;
                obj.Remove(Context.User.Id.ToString());
                obj.Add(new JProperty(Context.User.Id.ToString(), JObject.FromObject(User)));
                using (var users = new StreamWriter("Users.json")) {
                    using (var users2 = new JsonTextWriter(users)) {
                        var Serialiser = new JsonSerializer();
                        Serialiser.Serialize(users2, obj);
                    }
                }
                await ReplyAsync(string.Format("**{0}**, you have received <:unitofmoney:372385317581488128>**{1}**!\n(Streak: **{2}**)", Context.User.Username, gift, User.DailyStreak));
            } else {
                var ADayAgo = DateTime.UtcNow.AddDays(-1);
                await ReplyAsync(string.Format("You need to wait for **{0}** until you can get your daily gift again!", Global.GetDateString(User.LastDaily.Subtract(ADayAgo))));
            }
        }

        [Command("poupsoop")]
        [Summary("Calculates total value of Poup Soop")]
        public async Task _poupsoop([Remainder] string msg) {
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
                builder.AddField(type + " x" + no, "Cost: <:unitofmoney:372385317581488128>" + subtot);
            }
            builder.AddField("Total Cost", "<:unitofmoney:372385317581488128>" + totalCost);
            await ReplyAsync("", false, builder.Build());
        }
    }
}