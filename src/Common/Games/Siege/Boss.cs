using MarbleBot.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

namespace MarbleBot.Common
{
    public class Boss
    {
        public string Name { get; }
        private int _health;
        public int Health
        {
            get => _health;
            set => _health = value > MaxHealth ? MaxHealth : value < 1 ? 0 : value;
        }
        public int MaxHealth { get; }
        public ImmutableArray<Attack> Attacks { get; }
        public Difficulty Difficulty { get; }
        public ImmutableArray<BossDrops> Drops { get; }
        public string ImageUrl { get; }
        public int Stage { get; }

        public static Boss Empty => new Boss("", 0, Difficulty.None, 1, "",
            new Attack[] { Attack.Empty },
            new BossDrops[] { new BossDrops(0, 0, 0, 0) }
        );

        public Boss(string name, int health, Difficulty difficulty, int stage, string imageUrl,
            IEnumerable<Attack> attacks, IEnumerable<BossDrops> itemDrops)
        {
            Name = name;
            _health = health;
            MaxHealth = health;
            Difficulty = difficulty;
            Stage = stage;
            Attacks = attacks.ToImmutableArray();
            Drops = itemDrops == null ? ImmutableArray.Create<BossDrops>() : itemDrops.ToImmutableArray();
            ImageUrl = imageUrl;
        }

        public static Boss GetBoss(string searchTerm)
        {
            var bossesDict = GetBosses();
            searchTerm = searchTerm.ToPascalCase();

            foreach (var boss in bossesDict)
            {
                if (string.Compare(boss.Key, searchTerm, true) == 0)
                {
                    return boss.Value;
                }
            }

            throw new Exception("Could not find the given boss.");
        }

        public static IDictionary<string, Boss> GetBosses()
        {
            string json;
            using (var bosses = new StreamReader($"Resources{Path.DirectorySeparatorChar}Bosses.json"))
            {
                json = bosses.ReadToEnd();
            }
            return JsonConvert.DeserializeObject<IDictionary<string, Boss>>(json);
        }
    }
}
