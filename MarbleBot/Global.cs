using Discord.Commands;
using Google.Apis.Auth.OAuth2;
using MarbleBot.Core;
using System;
using System.Collections.Concurrent;
using System.IO;

namespace MarbleBot
{
    /// <summary> Contains global variables. </summary>
    internal static class Global
    {
        internal const ulong OwnerId = 224267581370925056;
        internal const string UoM = "<:unitofmoney:372385317581488128>";
        internal static CommandService CommandService { get; set; }
        internal static UserCredential Credential { get; set; }
        internal static ushort DailyTimeout { get; set; } = 48;
        internal static Random Rand { get; } = new Random();
        internal static DateTime StartTime { get; set; }
        internal static string YTKey
        {
            get
            {
                using var reader = new StreamReader($"Keys{Path.DirectorySeparatorChar}MBK.txt");
                return reader.ReadLine();
            }
        }

        // Games
        internal static ConcurrentDictionary<ulong, Scavenge> ScavengeInfo { get; set; } = new ConcurrentDictionary<ulong, Scavenge>();
        internal static ConcurrentDictionary<ulong, Siege> SiegeInfo { get; set; } = new ConcurrentDictionary<ulong, Siege>();
        internal static ConcurrentDictionary<ulong, War> WarInfo { get; set; } = new ConcurrentDictionary<ulong, War>();
    }
}
