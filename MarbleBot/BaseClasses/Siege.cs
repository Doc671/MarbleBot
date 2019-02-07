using System;
using System.Collections.Generic;

namespace MarbleBot.BaseClasses
{
    public class Siege
    {
        public bool Active;
        public Boss Boss;
        public double DMGMultiplier;
        public DateTime LastMorale;
        public List<Marble> Marbles;
        public int Morales;
        public string PowerUp;
        public string PUImageUrl;

        public void SetPowerUp(string PU)
        {
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

        public Siege(Boss boss, Marble[] marbles)
        {
            Active = false;
            Boss = boss;
            DMGMultiplier = 1; 
            LastMorale = DateTime.Parse("01/01/2019 00:00:00");
            Marbles = new List<Marble>(marbles);
            Morales = 0;
            PowerUp = "";
        }

        public Siege(Boss boss)
        {
            Active = false;
            Boss = boss;
            DMGMultiplier = 1;
            LastMorale = DateTime.Parse("01/01/2019 00:00:00");
            Marbles = new List<Marble>();
            Morales = 0;
            PowerUp = "";
        }

        public Siege(Marble[] marbles)
        {
            Active = false;
            Boss = Boss.Empty;
            DMGMultiplier = 1;
            LastMorale = DateTime.Parse("01/01/2019 00:00:00");
            Marbles = new List<Marble>(marbles);
            Morales = 0;
            PowerUp = "";
        }
    }
}
