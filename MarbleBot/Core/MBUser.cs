using System;
using System.Collections.Generic;

namespace MarbleBot.Core
{
    /// <summary> Represents a user when using money or item-based commands. </summary>
    public class MarbleBotUser
    {
        /// <summary> The name of the user. </summary>
        public string Name { get; set; } = "";
        /// <summary> The discriminator of the user. </summary>
        public string Discriminator { get; set; } = "0000";
        /// <summary> The balance of the user. </summary>
        public decimal Balance { get; set; }
        /// <summary> The net worth of the user. </summary>
        public decimal NetWorth { get; set; }
        /// <summary> The Stage of the user, representing how far they are through the game. </summary>
        public int Stage { get; set; } = 1;
        /// <summary> The number of times mb/daily was used successfully in a row. </summary>
        public uint DailyStreak { get; set; }
        /// <summary> Whether or not the user is pinged at the beginning of a Siege game. </summary>
        public bool SiegePing { get; set; }
        /// <summary> Whether or not the user is pinged at the beginning of a War game. </summary>
        public bool WarPing { get; set; }
        /// <summary> The number of times the user has won a race. </summary>
        public uint RaceWins { get; set; }
        /// <summary> The number of times the user has won a Siege. </summary>
        public uint SiegeWins { get; set; }
        /// <summary> The number of times the user has won a War. </summary>
        public uint WarWins { get; set; }
        /// <summary> The last time the user has used mb/daily. </summary>
        public DateTime LastDaily { get; set; } = DateTime.Parse("2019-01-01 00:00:00");
        /// <summary> The last time the user has won a race. </summary>
        public DateTime LastRaceWin { get; set; } = DateTime.Parse("2019-01-01 00:00:00");
        /// <summary> The last time the user has played in a Scavenge game. </summary>
        public DateTime LastScavenge { get; set; } = DateTime.Parse("2019-01-01 00:00:00");
        /// <summary> The last time the user has won a Siege game. </summary>
        public DateTime LastSiegeWin { get; set; } = DateTime.Parse("2019-01-01 00:00:00");
        /// <summary> The last time the user has won a Siege game. </summary>
        public DateTime LastWarWin { get; set; } = DateTime.Parse("2019-01-01 00:00:00");
        /// <summary> The items of the user - a key value pair storing item IDs against their quantities. </summary>
        public SortedDictionary<int, int> Items { get; set; } = new SortedDictionary<int, int>();

        /// <summary> Converts the user into a string representation. </summary>
        public override string ToString() => $"{Name}#{Discriminator}";
    }
}