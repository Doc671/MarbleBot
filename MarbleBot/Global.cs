﻿using Discord.Commands;
using MarbleBot.Core;
using MarbleBot.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MarbleBot
{
    /// <summary> Contains global variables. </summary>
    internal static class Global
    {
        internal const ulong BotId = 286228526234075136;
        internal const string UoM = "<:unitofmoney:372385317581488128>";
        internal static DateTime ARLastUse { get; set; }
        internal static Dictionary<string, string> Autoresponses { get; set; } = new Dictionary<string, string>();
        internal static CommandService CommandService { get; set; }
        internal static ushort DailyTimeout  { get; set; } = 48;
        internal static Random Rand { get; } = new Random();
        internal static Lazy<List<MBServer>> Servers  { get; set; } = new Lazy<List<MBServer>>();
        internal static Lazy<DateTime> StartTime { get; set; }
        internal static string YTKey { get; set; } = "";

        // Games
        internal static ConcurrentDictionary<ulong, byte> RaceAlive { get; set; } = new ConcurrentDictionary<ulong, byte>();
        internal static ConcurrentDictionary<ulong, Scavenge> ScavengeInfo { get; set; } = new ConcurrentDictionary<ulong, Scavenge>();
        internal static ConcurrentDictionary<ulong, Siege> SiegeInfo { get; set; } = new ConcurrentDictionary<ulong, Siege>();
        internal static ConcurrentDictionary<ulong, War> WarInfo { get; set; } = new ConcurrentDictionary<ulong, War>();
    }
}
