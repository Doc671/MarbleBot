using System;
using System.Collections.Generic;
using System.Linq;

namespace MarbleBot.BaseClasses
{
    public class Siege
    {
        public bool Active { get; set; }
        public Boss Boss { get; set; } = Boss.Empty;
        public double DamageMultiplier { get {
                var deathCount = Marbles.Aggregate(0, (totalDeaths, m) => {
                    if (m.HP < 1) totalDeaths++;
                    return totalDeaths;
                });
                return (1.0 + (deathCount * 0.2)) * (Morales + 1);
            }
        }
        public DateTime LastMorale { get; set; } = DateTime.Parse("2019-01-01 00:00:00");
        public List<Marble> Marbles { get; set; } = new List<Marble>();
        public byte Morales { get; set; }
        public string PowerUp { get; set; } = "";
        public string PUImageUrl { get; set; } = "";

        public void SetPowerUp(string PU) {
            PowerUp = PU;
            switch (PU) {
                case "Clone": PUImageUrl = "https://cdn.discordapp.com/attachments/296376584238137355/541373091495018496/PUClone.png"; break;
                case "Cure": PUImageUrl = "https://cdn.discordapp.com/attachments/296376584238137355/541373094724501524/PUCure.png"; break;
                case "Heal": PUImageUrl = "https://cdn.discordapp.com/attachments/296376584238137355/541373096238514202/PUHeal.png"; break;
                case "Morale Boost": PUImageUrl = "https://cdn.discordapp.com/attachments/296376584238137355/541373099090903070/PUMoraleBoost.png"; break;
                case "Overclock": PUImageUrl = "https://cdn.discordapp.com/attachments/296376584238137355/541373101649428480/PUOverclock.png"; break;
                case "Summon": PUImageUrl = "https://cdn.discordapp.com/attachments/296376584238137355/541373120129531939/PUSummon.png"; break;
                default: PUImageUrl = ""; break;
            }
        }

        public override string ToString() {
            return $"{Boss.Name}: {Marbles.Count}";
        }

        public Siege(Marble[] marbles) { Marbles = new List<Marble>(marbles); }
    }
}
