using System.Collections.Generic;
using System.Linq;

namespace MarbleBot.BaseClasses
{
    public class Boss {
        public string Name { get; set; }
        private int _HP;
        public int HP {
            get { return _HP; }
            set { _HP = value > MaxHP ? MaxHP : value < 1 ? 0 : value; }
        }

        public int MaxHP { get; }
        public Attack[] Attacks { get; set; }
        public Difficulty Difficulty { get; set; }
        public BossDrops[] Drops { get; set; }
        public string ImageUrl { get; set; }

        public static Boss Empty = new Boss("", 0, Difficulty.None, "",
            new Attack[] { Attack.Empty },
            new BossDrops[] { new BossDrops(0, 0, 0, 0) }
        );

        public void ResetHP() { _HP = MaxHP; }
     
        public Boss(string name, int hp, Difficulty diff, string imgUrl, Attack[] atks, IEnumerable<BossDrops> itemDrops) {
            Name = name;
            _HP = hp;
            MaxHP = hp;
            Difficulty = diff;
            Attacks = atks;
            Drops = itemDrops.ToArray();
            ImageUrl = imgUrl;
        }
    }
}