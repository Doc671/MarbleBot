using MarbleBot.Common;
using System.Collections.Concurrent;

namespace MarbleBot.Modules.Games.Services
{
    public class GamesService
    {
        public ConcurrentDictionary<ulong, Scavenge> ScavengeInfo { get; set; } = new ConcurrentDictionary<ulong, Scavenge>();
        public ConcurrentDictionary<ulong, Siege> SiegeInfo { get; set; } = new ConcurrentDictionary<ulong, Siege>();
        public ConcurrentDictionary<ulong, War> WarInfo { get; set; } = new ConcurrentDictionary<ulong, War>();
    }
}
