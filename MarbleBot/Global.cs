using Discord.Commands;
using MarbleBot.BaseClasses;
using MarbleBot.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarbleBot
{
    /// <summary> Contains global variables. </summary>
    internal static class Global
    {
        internal const ulong BotId = 286228526234075136;
        internal const string UoM = "<:unitofmoney:372385317581488128>";
        internal static DateTime ARLastUse { get; set; } = new DateTime();
        internal static Dictionary<string, string> Autoresponses { get; set; } = new Dictionary<string, string>();
        internal static CommandService CommandService { get; set; }
        internal static ushort DailyTimeout  { get; set; } = 48;
        internal static Random Rand { get; } = new Random();
        internal static List<MBServer> Servers  { get; set; } = new List<MBServer>();
        internal static DateTime StartTime { get; set; } = new DateTime();
        internal static string YTKey { get; set; } = "";

        /// <summary> Shows leaderboards for mb/race and mb/siege. </summary>
        /// <param name="orderedData"> The data to be made into a leaderboard. </param>
        /// <param name="no"> The part of the leaderboard that will be displayed. </param>
        /// <returns> A string ready to be output. </returns>
        internal static string Leaderboard(IEnumerable<Tuple<string, int>> orderedData, int no)
        {
            // This displays in groups of ten (i.e. if no is 1, first 10 displayed;
            // no = 2, next 10, etc.
            int displayedPlace = 1, dataIndex = 1, minValue = (no - 1) * 10 + 1, maxValue = no * 10;
            var output = new StringBuilder();
            foreach (var item in orderedData)
            {
                if (displayedPlace < maxValue + 1 && displayedPlace >= minValue)
                { // i.e. if item is within range
                    output.AppendLine($"{displayedPlace}{displayedPlace.Ordinal()}: {item.Item1} {item.Item2}");
                    if (dataIndex < orderedData.Count() && !(orderedData.ElementAt(dataIndex).Item2 == item.Item2))
                        displayedPlace++;
                }
                if (displayedPlace < maxValue + 1 && !(displayedPlace >= minValue)) displayedPlace++;
                else if (displayedPlace > maxValue) break;
                dataIndex++;
            }
            return output.ToString();
        }

        // Games
        internal static Dictionary<ulong, byte> RaceAlive { get; set; } = new Dictionary<ulong, byte>();
        internal static Dictionary<ulong, Queue<Item>> ScavengeInfo { get; set; } = new Dictionary<ulong, Queue<Item>>();
        internal static List<Task> ScavengeSessions { get; set; } = new List<Task>();
        internal static Dictionary<ulong, Siege> SiegeInfo { get; set; } = new Dictionary<ulong, Siege>();
    }
}
