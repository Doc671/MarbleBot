using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace MarbleBot.Common
{
    public class MarbleBotUser
    {
        public ulong Id { get; set; } = 0;
        public string Name { get; set; } = "";
        public string Discriminator { get; set; } = "0000";
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
        public SortedDictionary<int, int> Items { get; set; } = new SortedDictionary<int, int>();

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

        public override string ToString() => $"{Name}#{Discriminator}";
    }
}
