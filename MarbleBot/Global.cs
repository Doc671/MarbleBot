using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Discord;
using Discord.Commands;
using MarbleBot.Modules;
using Newtonsoft.Json.Linq;

namespace MarbleBot
{
    class Global
    {
        /// <summary>
        /// Contains global variables
        /// </summary>

        internal static Random Rand = new Random();
        internal static DateTime StartTime = new DateTime();
        internal static string YTKey = "";
        internal static ulong BotId = 286228526234075136;
        internal static Dictionary<string, string> Autoresponses = new Dictionary<string, string>();
        internal static DateTime ARLastUse = new DateTime();

        // Server IDs
        internal const ulong CM = 223616088263491595; // Community Marble
        internal const ulong THS = 224277738608001024; // The Hat Stoar
        internal const ulong THSC = 318053169999511554; // The Hat Stoar Crew
        internal const ulong VFC = 394086559676235776; // Vinh Fan Club
        internal const ulong MT = 408694288604463114; // Melmon Test

        // Games
        internal static bool RaceActive = false;
        internal static Dictionary<ulong, byte> Alive = new Dictionary<ulong, byte>();

        // Gets colour for embed depending on server
        internal static Color GetColor(SocketCommandContext Context) {
            Color coloure = Color.DarkerGrey;
            var id = 0ul;
            if (!Context.IsPrivate) id = Context.Guild.Id;
            switch (id) {
                case CM: coloure = Color.Teal; break;
                case THS: coloure = Color.Orange; break;
                case MT: coloure = Color.DarkGrey; break;
                case VFC: coloure = Color.Blue; break;
                case THSC: goto case THS;
                default: coloure = Color.DarkerGrey; break;
            }
            return coloure;
        }

        // Gets a date string
        internal static string GetDateString(TimeSpan dateTime) {
            var output = new StringBuilder();
            if (dateTime.Days > 1) output.Append(dateTime.Days + " days, ");
            else if (dateTime.Days > 0) output.Append(dateTime.Days + " day, ");
            if (dateTime.Hours > 1) output.Append(dateTime.Hours + " hours, ");
            else if (dateTime.Hours > 0) output.Append(dateTime.Hours + " hour, ");
            if (dateTime.Minutes > 1) output.Append(dateTime.Minutes + " minutes ");
            else if (dateTime.Minutes > 0) output.Append(dateTime.Minutes + " minute ");
            if (dateTime.Seconds > 1) {
                if (dateTime.Minutes > 0) output.Append("and " + dateTime.Seconds + " seconds");
                else output.Append(dateTime.Seconds + " seconds");
            } else if (dateTime.Seconds > 0) {
                if (dateTime.Minutes > 0) output.Append("and " + dateTime.Seconds + " second");
                else output.Append(dateTime.Seconds + " second");
            }
            return output.ToString();
        }

        // Returns a MoneyUser with the ID of the user
        internal static MoneyUser GetUser(SocketCommandContext Context) {
            var json = "";
            using (var users = new StreamReader("Users.json")) json = users.ReadToEnd();
            var obj = JObject.Parse(json);
            var User = new MoneyUser();
            if (obj.ContainsKey(Context.User.Id.ToString())) {
                User = obj[(Context.User.Id.ToString())].ToObject<MoneyUser>();
            } else {
                User = new MoneyUser() {
                    Name = Context.User.Username,
                    Discriminator = Context.User.Discriminator,
                    Money = 0,
                    DailyStreak = 0,
                    LastDaily = DateTime.Parse("2019-01-01 00:00:00"),
                    LastRaceWin = DateTime.Parse("2019-01-01 00:00:00")
                };
            }
            return User;
        }

        internal static MoneyUser GetUser(SocketCommandContext Context, JObject obj) {
            var User = new MoneyUser();
            if (obj.ContainsKey(Context.User.Id.ToString())) {
                User = obj[(Context.User.Id.ToString())].ToObject<MoneyUser>();
            } else {
                User = new MoneyUser() {
                    Name = Context.User.Username,
                    Discriminator = Context.User.Discriminator,
                    Money = 0,
                    DailyStreak = 0,
                    LastDaily = DateTime.Parse("2019-01-01 00:00:00")
                };
            }
            return User;
        }

        internal static MoneyUser GetUser(SocketCommandContext Context, JObject obj, ulong id) {
            var User = new MoneyUser();
            if (obj.ContainsKey(id.ToString())) {
                User = obj[(id.ToString())].ToObject<MoneyUser>();
            } else {
                if (Context.IsPrivate) {
                    User = new MoneyUser() {
                        Name = Context.User.Username,
                        Discriminator = Context.User.Discriminator,
                        Money = 0,
                        DailyStreak = 0,
                        LastDaily = DateTime.Parse("2019-01-01 00:00:00"),
                        LastRaceWin = DateTime.Parse("2019-01-01 00:00:00")
                    };
                } else {
                    User = new MoneyUser() {
                        Name = Context.Guild.GetUser(id).Username,
                        Discriminator = Context.Guild.GetUser(id).Discriminator,
                        Money = 0,
                        DailyStreak = 0,
                        LastDaily = DateTime.Parse("2019-01-01 00:00:00"),
                        LastRaceWin = DateTime.Parse("2019-01-01 00:00:00")
                    };
                }
            }
            return User;
        }
    }
}
