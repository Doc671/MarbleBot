using Newtonsoft.Json;
using System.Collections.Generic;

namespace MarbleBot.Common
{
    public class WarTournamentInfo
    {
        public ulong GuildId { get; }
        public uint Spaces { get; }
        public uint TeamSize { get; }
        public Dictionary<string, List<ulong>> Marbles { get; set; }

        [JsonConstructor]
        public WarTournamentInfo(ulong guildId, uint spaces, uint teamSize, Dictionary<string, List<ulong>> marbles)
        {
            GuildId = guildId;
            Spaces = spaces;
            TeamSize = teamSize;
            Marbles = marbles;
        }
    }
}
