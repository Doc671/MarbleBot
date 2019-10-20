using System.Collections.Generic;

namespace MarbleBot.Common
{
    /// <summary> Represents a boss in a Marble Siege battle. </summary>
    public class Boss
    {
        /// <summary> The name of the boss. </summary>
        public string Name { get; set; }
        private int _HP;
        /// <summary> The number of health points the boss currently has. </summary>
        public int HP
        {
            get { return _HP; }
            set { _HP = value > MaxHP ? MaxHP : value < 1 ? 0 : value; }
        }
        /// <summary> The maximum number of health points the boss can have. </summary>
        public int MaxHP { get; }
        /// <summary> The boss' attacks. </summary>
        public IReadOnlyCollection<Attack> Attacks { get; set; }
        /// <summary> The boss' difficulty. </summary>
        public Difficulty Difficulty { get; set; }
        /// <summary> The items that drop from the boss, the number of them and the chance each one has of dropping. </summary>
        public IReadOnlyCollection<BossDrops> Drops { get; set; }
        /// <summary> A URL to the boss' image. </summary>
        public string ImageUrl { get; set; }
        /// <summary> The Stage the boss appears at. </summary>
        public int Stage { get; set; }

        /// <summary> An empty instance of a boss. </summary>
        public static Boss Empty => new Boss("", 0, Difficulty.None, 1, "",
            new Attack[] { Attack.Empty },
            new BossDrops[] { new BossDrops(0, 0, 0, 0) }
        );

        /// <summary> Represents a boss in a Marble Siege battle. </summary>
        /// <param name="name"> The name of the boss. </param>
        /// <param name="hp"> The boss' health points. </param>
        /// <param name="diff"> The boss' difficulty. </param>
        /// <param name="imgUrl"> A URL to the boss' image. </param>
        /// <param name="atks"> The boss' attacks. </param>
        /// <param name="itemDrops"> The items that drop from the boss, the number of them and the chance each one has of dropping. </param>
        public Boss(string name, int hp, Difficulty diff, int stage, string imgUrl, Attack[] atks, BossDrops[] itemDrops)
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
