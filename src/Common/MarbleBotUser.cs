using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
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
        public SortedDictionary<int, int> Items { get; } = new();

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

        public static async Task<MarbleBotUser> Find(ICommandContext context)
        {
            var usersDict = GetUsers();
            return await GetUserFromDictionary(context, context.User.Id, usersDict);
        }

        public static async Task<MarbleBotUser> Find(ICommandContext context, ulong id)
        {
            var usersDict = GetUsers();
            return await GetUserFromDictionary(context, id, usersDict);
        }

        public static async Task<MarbleBotUser> Find(ICommandContext context, IDictionary<ulong, MarbleBotUser> usersDict)
        {
            return await GetUserFromDictionary(context, context.User.Id, usersDict);
        }

        public static async Task<MarbleBotUser> Find(ICommandContext context, ulong id, IDictionary<ulong, MarbleBotUser> usersDict)
        {
            return await GetUserFromDictionary(context, id, usersDict);
        }

        private static async Task<MarbleBotUser> GetUserFromDictionary(ICommandContext context, ulong id,
            IDictionary<ulong, MarbleBotUser> usersDict)
        {
            if (!usersDict.TryGetValue(id, out MarbleBotUser? user))
            {
                IUser? discordUser = await context.Client.GetUserAsync(id);
                string defaultUsername;
                string defaultDiscriminator;
                if (discordUser == null)
                {
                    defaultUsername = context.User.Username;
                    defaultDiscriminator = context.User.Discriminator;
                }
                else
                {
                    defaultUsername = discordUser.Username;
                    defaultDiscriminator = discordUser.Discriminator;
                }

                user = new MarbleBotUser
                {
                    Id = id,
                    Name = defaultUsername,
                    Discriminator = defaultDiscriminator
                };
            }

            return user;
        }

        public static IDictionary<ulong, MarbleBotUser> GetUsers()
        {
            string json; 
            using (var itemFile = new StreamReader($"Data{Path.DirectorySeparatorChar}Users.json"))
            {
                json = itemFile.ReadToEnd();
            }
            return JsonSerializer.Deserialize<IDictionary<string, MarbleBotUser>>(json)!
                    .ToDictionary(pair => ulong.Parse(pair.Key), pair => pair.Value);
        }

        public static void UpdateUser(MarbleBotUser user)
        {
            var usersDict = GetUsers();

            if (usersDict.ContainsKey(user.Id))
            {
                usersDict.Remove(user.Id);
            }

            usersDict.Add(user.Id, user);
            UpdateUsers(usersDict);
        }

        public static void UpdateUser(IDictionary<ulong, MarbleBotUser> usersDict, IUser socketUser,
            MarbleBotUser newMarbleBotUser)
        {
            if (usersDict.ContainsKey(socketUser.Id))
            {
                usersDict.Remove(socketUser.Id);
            }

            usersDict.Add(socketUser.Id, newMarbleBotUser);
            UpdateUsers(usersDict);
        }

        public static void UpdateUsers(IDictionary<ulong, MarbleBotUser> usersDict)
        {
            using var userWriter = new StreamWriter($"Data{Path.DirectorySeparatorChar}Users.json");
            using var userJsonWriter = new Utf8JsonWriter(userWriter.BaseStream, new JsonWriterOptions { Indented = true });
            JsonSerializer.Serialize(userJsonWriter, usersDict);
        }

        public override string ToString()
        {
            return $"{Name}#{Discriminator}";
        }
    }
}
