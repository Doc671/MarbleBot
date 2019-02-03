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
        internal static string UoM = "<:unitofmoney:372385317581488128>";
        internal static ulong BotId = 286228526234075136;
        internal static Dictionary<string, string> Autoresponses = new Dictionary<string, string>();
        internal static DateTime ARLastUse = new DateTime();
        internal static ulong[] BotChannels = { 229280519697727488, 269922990936948737, 318053391777660929, 394090786578366474, 409655798730326016 };
        internal static ulong[] UsableChannels = { 229280519697727488, 269922990936948737, 318053391777660929, 394090786578366474, 409655798730326016, 252481530130202624, 224478087046234112, 293837572130603008 };

        // Server IDs
        internal const ulong CM = 223616088263491595; // Community Marble
        internal const ulong THS = 224277738608001024; // The Hat Stoar
        internal const ulong THSC = 318053169999511554; // The Hat Stoar Crew
        internal const ulong VFC = 394086559676235776; // Vinh Fan Club
        internal const ulong MT = 408694288604463114; // Melmon Test

        // Games
        internal static Dictionary<ulong, byte> RaceAlive = new Dictionary<ulong, byte>();
        internal static Dictionary<ulong, Siege> SiegeInfo = new Dictionary<ulong, Siege>();
        internal static List<Task> Sieges = new List<Task>();
        internal static Boss PreeTheTree = new Boss("Pree the Tree", 300, Difficulty.Simple, "https://cdn.discordapp.com/attachments/296376584238137355/541383182197719040/BossPreeTheTree.png", new Attack[] {
            new Attack("Falling Leaves", 3, 35),
            new Attack("Spinning Leaves", 3, 40),
            new Attack("Acorn Bomb", 5, 45),
            new Attack("Floating Twigs", 2, 55)
        });
        internal static Boss HATTMANN = new Boss("HATT MANN", 600, Difficulty.Easy, "https://cdn.discordapp.com/attachments/296376584238137355/541383185481596940/BossHATTMANN.png", new Attack[] {
            new Attack("Hat Trap", 4, 45),
            new Attack("Inverted Hat", 3, 50),
            new Attack("HATT GUNN", 6, 35),
            new Attack("Hat Spawner", 2, 90)
        });
        internal static Boss Orange = new Boss("Orange", 1200, Difficulty.Decent, "https://cdn.discordapp.com/attachments/296376584238137355/541383189114126339/BossOrange.png", new Attack[] {
            new Attack("Poup Soop Barrel", 4, 40),
            new Attack("Poup Krumb", 8, 50),
            new Attack("ORANGE HEDDS", 5, 40),
            new Attack("How To Be An Idiot Vol. 3", 3, 45)
        });
        internal static Boss Green = new Boss("Green", 1500, Difficulty.Risky, "https://cdn.discordapp.com/attachments/296376584238137355/541383199943819289/BossGreen.png", new Attack[] {
            new Attack("Wobbly Toxicut", 7, 45),
            new Attack("Falling Hellslash", 8, 40),
            new Attack("Attractive Domesday", 15, 20),
            new Attack("Spinning Pyroclash", 5, 70),
            new Attack("Accurate Flarer", 3, 95)
        });
        internal static Boss Destroyer = new Boss("Destroyer", 3720, Difficulty.Extreme, "https://cdn.discordapp.com/attachments/296376584238137355/541383205048287262/BossDestroyer.png", new Attack[] {
            new Attack("Antimatter Missile", 12, 55),
            new Attack("Annihilator-A", 10, 50),
            new Attack("Flamethrower", 9, 60),
            new Attack("Black Hole", 13, 60),
            new Attack("Repulsor Blast", 6, 75)
        });

        // Gets colour for embed depending on server
        internal static Color GetColor(SocketCommandContext Context) {
            Color coloure;
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
        internal static MBUser GetUser(SocketCommandContext Context) {
            var json = "";
            using (var users = new StreamReader("Users.json")) json = users.ReadToEnd();
            var obj = JObject.Parse(json);
            MBUser User;
            if (obj.ContainsKey(Context.User.Id.ToString())) {
                User = obj[(Context.User.Id.ToString())].ToObject<MBUser>();
                if (string.IsNullOrEmpty(obj[(Context.User.Id.ToString())]?.ToString())) User.Items = new Dictionary<int, int>();
            } else {
                User = new MBUser() {
                    Name = Context.User.Username,
                    Discriminator = Context.User.Discriminator,
                    Balance = 0,
                    NetWorth = 0,
                    DailyStreak = 0,
                    RaceWins = 0,
                    LastDaily = DateTime.Parse("2019-01-01 00:00:00"),
                    LastRaceWin = DateTime.Parse("2019-01-01 00:00:00"),
                    Items = new Dictionary<int, int>()
                };
            }
            return User;
        }

        internal static MBUser GetUser(SocketCommandContext Context, JObject obj) {
            MBUser User;
            if (obj.ContainsKey(Context.User.Id.ToString())) {
                User = obj[(Context.User.Id.ToString())].ToObject<MBUser>();
                if (string.IsNullOrEmpty(obj[(Context.User.Id.ToString())]?.ToString())) User.Items = new Dictionary<int, int>();
            } else {
                User = new MBUser() {
                    Name = Context.User.Username,
                    Discriminator = Context.User.Discriminator,
                    Balance = 0,
                    NetWorth = 0,
                    DailyStreak = 0,
                    RaceWins = 0,
                    LastDaily = DateTime.Parse("2019-01-01 00:00:00"),
                    LastRaceWin = DateTime.Parse("2019-01-01 00:00:00"),
                    Items = new Dictionary<int, int>()
                };
            }
            return User;
        }

        internal static MBUser GetUser(SocketCommandContext Context, JObject obj, ulong id) {
            MBUser User;
            if (obj.ContainsKey(id.ToString())) {
                User = obj[(id.ToString())].ToObject<MBUser>();
                if (string.IsNullOrEmpty(obj[(Context.User.Id.ToString())]?.ToString())) User.Items = new Dictionary<int, int>();
            } else {
                if (Context.IsPrivate) {
                    User = new MBUser() {
                        Name = Context.User.Username,
                        Discriminator = Context.User.Discriminator,
                        Balance = 0,
                        NetWorth = 0,
                        DailyStreak = 0,
                        RaceWins = 0,
                        LastDaily = DateTime.Parse("2019-01-01 00:00:00"),
                        LastRaceWin = DateTime.Parse("2019-01-01 00:00:00"),
                        Items = new Dictionary<int, int>()
                    };
                } else {
                    User = new MBUser() {
                        Name = Context.Guild.GetUser(id).Username,
                        Discriminator = Context.Guild.GetUser(id).Discriminator,
                        Balance = 0,
                        NetWorth = 0,
                        DailyStreak = 0,
                        RaceWins = 0,
                        LastDaily = DateTime.Parse("2019-01-01 00:00:00"),
                        LastRaceWin = DateTime.Parse("2019-01-01 00:00:00"),
                        Items = new Dictionary<int, int>()
                    };
                }
            }
            return User;
        }

        // Returns an item using its ID
        internal static Item GetItem(string searchTerm) {
            var item = new Item();
            if (int.TryParse(searchTerm, out int itemID)) {
                var itemFound = false;
                using (var items = new StreamReader("ShopItems.csv")) {
                    while (!items.EndOfStream) {
                        var properties = items.ReadLine().Split(',');
                        if (properties[0].ToInt() == itemID) {
                            itemFound = true;
                            item.Id = properties[0].ToInt();
                            item.Name = properties[1];
                            item.Price = properties[2].ToDecimal();
                            item.Description = properties[3];
                            item.OnSale = bool.Parse(properties[4]);
                        }
                    }
                }
                if (itemFound) return item;
                else {
                    item.Id = -1;
                    return item;
                }
            } else {
                item.Id = -2;
                return item;
            }
        }
    }
}
