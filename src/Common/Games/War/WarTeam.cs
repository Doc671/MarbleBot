using System.Collections.Generic;
using System.Linq;

namespace MarbleBot.Common.Games.War
{
    public class WarTeam
    {
        public WarBoost Boost { get; }
        public bool BoostUsed { get; set; }
        public bool IsLeftTeam { get; }
        public IReadOnlyCollection<WarMarble> Marbles { get; }
        public string Name { get; }

        public WarTeam(string name, IEnumerable<WarMarble> marbles, bool isLeft, WarBoost boost)
        {
            Boost = boost;
            IsLeftTeam = isLeft;
            Marbles = marbles.ToArray();
            Name = name;
        }
    }
}
