using System.Collections.Generic;

namespace MarbleBot.Common
{
    public class Boss
    {
        public string Name { get; set; }
        private int _HP;
        public int HP
        {
            get { return _HP; }
            set { _HP = value > MaxHP ? MaxHP : value < 1 ? 0 : value; }
        }
        public int MaxHP { get; }
        public IReadOnlyCollection<Attack> Attacks { get; set; }
        public Difficulty Difficulty { get; set; }
        public IReadOnlyCollection<BossDrops> Drops { get; set; }
        public string ImageUrl { get; set; }
        public int Stage { get; set; }

        public static Boss Empty => new Boss("", 0, Difficulty.None, 1, "",
            new Attack[] { Attack.Empty },
            new BossDrops[] { new BossDrops(0, 0, 0, 0) }
        );

        public Boss(string name, int hp, Difficulty diff, int stage, string imgUrl, IReadOnlyCollection<Attack> atks, IReadOnlyCollection<BossDrops> itemDrops)
        {
            Name = name;
            _HP = hp;
            MaxHP = hp;
            Difficulty = diff;
            Stage = stage;
            Attacks = atks;
            Drops = itemDrops;
            ImageUrl = imgUrl;
        }
    }
}
