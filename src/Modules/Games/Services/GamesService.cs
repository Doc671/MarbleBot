using MarbleBot.Common;
using System.Collections.Concurrent;

namespace MarbleBot.Modules.Games.Services
{
    public class GamesService
    {
        public ConcurrentDictionary<ulong, Scavenge> Scavenges { get; set; } = new ConcurrentDictionary<ulong, Scavenge>();
        public ConcurrentDictionary<ulong, Siege> Sieges { get; set; } = new ConcurrentDictionary<ulong, Siege>();
        public ConcurrentDictionary<ulong, War> Wars { get; set; } = new ConcurrentDictionary<ulong, War>();
    }
}
