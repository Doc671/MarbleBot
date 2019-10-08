using System.Collections.Generic;
using System.Linq;

namespace MarbleBot.Core
{
    public class WarTeam
    {
        public WarBoost Boost { get; }
        public bool BoostUsed { get; set; }
        public IReadOnlyCollection<WarMarble> Marbles { get; set; }
        public string Name { get; }

        public WarTeam(string name, IEnumerable<WarMarble> marbles, WarBoost boost)
        {
            Boost = boost;
            Marbles = marbles.ToArray();
            Name = name;
        }
    }
}
