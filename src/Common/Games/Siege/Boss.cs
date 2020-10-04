using MarbleBot.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

namespace MarbleBot.Common.Games.Siege
{
    public class Boss
    {
        private int _health;

        public int Health
        {
            get => _health;
            set => _health = value > MaxHealth ? MaxHealth : value < 1 ? 0 : value;
        }

        public string Name { get; }
        public int MaxHealth { get; }
        public ImmutableArray<Attack> Attacks { get; }
        public Difficulty Difficulty { get; }
        public ImmutableArray<BossDropInfo> Drops { get; }
        public string ImageUrl { get; }
        public int Stage { get; }

        public Boss(string name, int health, Difficulty difficulty, int stage, string imageUrl,
            IEnumerable<Attack> attacks, IEnumerable<BossDropInfo>? itemDrops)
        {
            Name = name;
            _health = health;
            MaxHealth = health;
            Difficulty = difficulty;
            Stage = stage;
            Attacks = attacks.ToImmutableArray();
            Drops = itemDrops?.ToImmutableArray() ?? ImmutableArray.Create<BossDropInfo>();
            ImageUrl = imageUrl;
        }

        public static Boss GetBoss(string searchTerm)
        {
            var bossesDict = GetBosses();
            searchTerm = searchTerm.ToPascalCase();

            foreach ((string bossName, Boss boss) in bossesDict)
            {
                if (string.Compare(bossName, searchTerm, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return boss;
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
