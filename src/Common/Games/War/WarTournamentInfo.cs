using Newtonsoft.Json;
using System.Collections.Generic;

namespace MarbleBot.Common
{
    public class WarTournamentInfo
    {
        public ulong GuildId { get; }
        public Dictionary<string, List<ulong>> Marbles { get; }

        [JsonConstructor]
        public WarTournamentInfo(ulong guildId, IDictionary<string, IEnumerable<ulong>> marbles)
        {
            GuildId = guildId;
            Marbles = (Dictionary<string, List<ulong>>)marbles;
        }
    }
}
