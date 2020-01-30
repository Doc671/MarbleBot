using Newtonsoft.Json;
using System.Collections.Generic;

namespace MarbleBot.Common
{
    public class WarTournamentInfo
    {
        public ulong GuildId { get; }
        public int Spaces { get; }
        public int TeamSize { get; }
        public Dictionary<string, List<ulong>> Marbles { get; set; }

        [JsonConstructor]
        public WarTournamentInfo(ulong guildId, int spaces, int teamSize, Dictionary<string, List<ulong>> marbles)
        {
            GuildId = guildId;
            Spaces = spaces;
            TeamSize = teamSize;
            Marbles = marbles;
        }
    }
}
