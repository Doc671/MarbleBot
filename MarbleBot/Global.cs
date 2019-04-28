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
        internal static CommandService CommandService { get; set; }

        internal static Random Rand = new Random();
        internal static DateTime StartTime = new DateTime();
        internal static string YTKey = "";
        internal const string UoM = "<:unitofmoney:372385317581488128>";
        internal const ulong BotId = 286228526234075136;
        internal static Dictionary<string, string> Autoresponses = new Dictionary<string, string>();
        internal static DateTime ARLastUse = new DateTime();
        internal static ulong[] BotChannels = { 229280519697727488, 269922990936948737, 318053391777660929, 394090786578366474, 409655798730326016, 540638882740305932 };
        internal static ulong[] UsableChannels = { 229280519697727488, 269922990936948737, 318053391777660929, 394090786578366474, 409655798730326016, 540638882740305932, 224478087046234112, 293837572130603008 };

        /// <summary> Shows leaderboards for mb/race and mb/siege. </summary>
        /// <param name="orderedData"> The data to be made into a leaderboard. </param>
        /// <param name="no"> The part of the leaderboard that will be displayed. </param>
        /// <returns> A string ready to be output. </returns>
        internal static string Leaderboard(IEnumerable<Tuple<string, int>> orderedData, int no) {
            // This displays in groups of ten (i.e. if no is 1, first 10 displayed;
            // no = 2, next 10, etc.
            int displayedPlace = 1, dataIndex = 1, minValue = (no - 1) * 10 + 1, maxValue = no * 10;
            var output = new StringBuilder();
            foreach (var item in orderedData) {
                if (displayedPlace < maxValue + 1 && displayedPlace >= minValue) { // i.e. if item is within range
                    output.AppendLine($"{displayedPlace}{displayedPlace.Ordinal()}: {item.Item1} {item.Item2}");
                    if (dataIndex < orderedData.Count() && !(orderedData.ElementAt(dataIndex).Item2 == item.Item2))
                        displayedPlace++;
                }
                if (displayedPlace < maxValue + 1 && !(displayedPlace >= minValue)) displayedPlace++;
                else if(displayedPlace > maxValue) break;
                dataIndex++;
            }
            return output.ToString();
        }

        // Games
        internal static Dictionary<ulong, byte> RaceAlive = new Dictionary<ulong, byte>();
        internal static Dictionary<ulong, Queue<Item>> ScavengeInfo = new Dictionary<ulong, Queue<Item>>();
        internal static List<Task> ScavengeSessions = new List<Task>();
        internal static Dictionary<ulong, Siege> SiegeInfo = new Dictionary<ulong, Siege>();
    }
}
