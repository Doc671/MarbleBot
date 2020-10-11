using Discord;
using Discord.Commands;
using MarbleBot.Common.Games;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MarbleBot.Common
{
    public class MarbleBotUser
    {
        public MarbleBotUser()
        {
        }

        [JsonConstructor]
        public MarbleBotUser(ulong id, string name, string discriminator, decimal balance, decimal netWorth, int stage,
            int dailyStreak, bool siegePing, bool warPing, int raceWins, int siegeWins, int warWins,
            DateTime lastDaily, DateTime lastRaceWin, DateTime lastScavenge, DateTime lastSiegeWin,
            DateTime lastWarWin, SortedDictionary<int, int>? items)
        {
            Id = id;
            Name = name;
            Discriminator = discriminator;
            Balance = balance;
            NetWorth = netWorth;
            Stage = stage;
            DailyStreak = dailyStreak;
            SiegePing = siegePing;
            WarPing = warPing;
            RaceWins = raceWins;
            SiegeWins = siegeWins;
            WarWins = warWins;
            LastDaily = lastDaily;
            LastRaceWin = lastRaceWin;
            LastScavenge = lastScavenge;
            LastSiegeWin = lastSiegeWin;
            LastWarWin = lastWarWin;
            Items = items ?? new SortedDictionary<int, int>();
        }

        public ulong Id { get; private set; }
        public string Name { get; private set; } = "";
        public string Discriminator { get; private set; } = "0000";
        public decimal Balance { get; set; }
        public decimal NetWorth { get; set; }
        public int Stage { get; set; } = 1;
        public int DailyStreak { get; set; }
        public bool SiegePing { get; set; }
        public bool WarPing { get; set; }
        public int RaceWins { get; set; }
        public int SiegeWins { get; set; }
        public int WarWins { get; set; }
        public DateTime LastDaily { get; set; } = DateTime.MinValue;
        public DateTime LastRaceWin { get; set; } = DateTime.MinValue;
        public DateTime LastScavenge { get; set; } = DateTime.MinValue;
        public DateTime LastSiegeWin { get; set; } = DateTime.MinValue;
        public DateTime LastWarWin { get; set; } = DateTime.MinValue;
        public string WarEmoji { get; set; } = "\uD83D\uDD35";
        public SortedDictionary<int, int> Items { get; } = new SortedDictionary<int, int>();

        public Ammo? GetAmmo(Weapon weapon)
        {
            for (int i = weapon.Ammo.Length - 1; i >= 0; i--)
            {
                if (Items.ContainsKey(weapon.Ammo[i]) &&
                    Items[weapon.Ammo[i]] >= weapon.Hits)
                {
                    return Item.Find<Ammo>(weapon.Ammo[i].ToString("000"));
                }
            }

            return null;
        }

        public Shield? GetShield()
        {
            var shields = Items.Select(item => Item.Find<Item>(item.Key))
                .Where(item => item is Shield)
                .ToArray();

            if (!shields.Any())
            {
                return null;
            }

            return (Shield)shields.Last();
        }

        public Spikes? GetSpikes()
        {
            var spikes = Items.Select(item => Item.Find<Item>(item.Key))
                .Where(item => item is Spikes)
                .ToArray();

            if (!spikes.Any())
            {
                return null;
            }

            return (Spikes)spikes.Last();
        }

        public static MarbleBotUser Find(ulong id)
        {
            var userDict = GetUsers();
            MarbleBotUser user;
            if (userDict.ContainsKey(id))
            {
                user = userDict[id];
            }
            else
            {
                throw new InvalidOperationException("The requested user was not found.");
            }

            return user;
        }

        public static MarbleBotUser Find(ICommandContext context)
        {
            var userDict = GetUsers();
            MarbleBotUser user;
            if (userDict.ContainsKey(context.User.Id))
            {
                user = userDict[context.User.Id];
                user.Name = context.User.Username;
                user.Discriminator = context.User.Discriminator;
            }
            else
            {
                user = new MarbleBotUser
                {
                    Id = context.User.Id,
                    Name = context.User.Username,
                    Discriminator = context.User.Discriminator
                };
            }

            return user;
        }

        public static MarbleBotUser Find(ICommandContext context, ulong id)
        {
            var userDict = GetUsers();
            MarbleBotUser user;
            if (userDict.ContainsKey(id))
            {
                user = userDict[id];
                user.Name = context.User.Username;
                user.Discriminator = context.User.Discriminator;
            }
            else
            {
                user = new MarbleBotUser
                {
                    Id = context.User.Id,
                    Name = context.User.Username,
                    Discriminator = context.User.Discriminator
                };
            }

            return user;
        }

        public static MarbleBotUser Find(ICommandContext context, IDictionary<ulong, MarbleBotUser> usersDict)
        {
            MarbleBotUser user;
            if (usersDict.ContainsKey(context.User.Id))
            {
                user = usersDict[context.User.Id];
                user.Name = context.User.Username;
                user.Discriminator = context.User.Discriminator;
            }
            else
            {
                user = new MarbleBotUser
                {
                    Id = context.User.Id,
                    Name = context.User.Username,
                    Discriminator = context.User.Discriminator
                };
            }

            return user;
        }

        public static async Task<MarbleBotUser> FindAsync(ICommandContext context,
            IDictionary<ulong, MarbleBotUser> usersDict, ulong id)
        {
            MarbleBotUser user;
            if (usersDict.ContainsKey(id))
            {
                user = usersDict[id];
            }
            else
            {
                user = new MarbleBotUser
                {
                    Id = context.User.Id,
                    Name = (await context.Client.GetUserAsync(id)).Username,
                    Discriminator = (await context.Client.GetUserAsync(id)).Discriminator
                };
            }

            return user;
        }

        public static IDictionary<ulong, MarbleBotUser> GetUsers()
        {
            string json;
            using (var usersDict = new StreamReader($"Data{Path.DirectorySeparatorChar}Users.json"))
            {
                json = usersDict.ReadToEnd();
            }

            return JsonConvert.DeserializeObject<IDictionary<ulong, MarbleBotUser>>(json);
        }

        public static void UpdateUser(MarbleBotUser user)
        {
            var usersDict = GetUsers();

            if (usersDict.ContainsKey(user.Id))
            {
                usersDict.Remove(user.Id);
            }

            usersDict.Add(user.Id, user);
            using var userWriter = new JsonTextWriter(new StreamWriter($"Data{Path.DirectorySeparatorChar}Users.json"));
            var serialiser = new JsonSerializer { Formatting = Formatting.Indented };
            serialiser.Serialize(userWriter, usersDict);
        }

        public static void UpdateUser(IDictionary<ulong, MarbleBotUser> usersDict, IUser socketUser,
            MarbleBotUser newMarbleBotUser)
        {
            if (usersDict.ContainsKey(socketUser.Id))
            {
                usersDict.Remove(socketUser.Id);
            }

            usersDict.Add(socketUser.Id, newMarbleBotUser);
            using var userWriter = new JsonTextWriter(new StreamWriter($"Data{Path.DirectorySeparatorChar}Users.json"));
            var serialiser = new JsonSerializer { Formatting = Formatting.Indented };
            serialiser.Serialize(userWriter, usersDict);
        }

        public static void UpdateUsers(IDictionary<ulong, MarbleBotUser> usersDict)
        {
            using var userWriter = new JsonTextWriter(new StreamWriter($"Data{Path.DirectorySeparatorChar}Users.json"));
            var serialiser = new JsonSerializer { Formatting = Formatting.Indented };
            serialiser.Serialize(userWriter, usersDict);
        }

        public override string ToString()
        {
            return $"{Name}#{Discriminator}";
        }
    }
}
