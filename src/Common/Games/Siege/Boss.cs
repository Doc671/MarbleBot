using MarbleBot.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text.Json;

namespace MarbleBot.Common.Games.Siege
{
    public class Boss
    {
        private int _health;
        public int Health
        {
            get => _health;
            set => _health = Math.Clamp(value, 0, MaxHealth);
        }

        public int MaxHealth { get; }

        public string Name { get; }
        public ImmutableArray<Attack> Attacks { get; }
        public Difficulty Difficulty { get; }
        public ImmutableArray<BossDropInfo> Drops { get; }
        public string ImageUrl { get; }
        public int Stage { get; }

        public Boss(string name, int health, Difficulty difficulty, int stage, string imageUrl,
            ImmutableArray<Attack> attacks, ImmutableArray<BossDropInfo> drops)
        {
            Name = name;
            _health = MaxHealth = health;
            Difficulty = difficulty;
            Stage = stage;
            ImageUrl = imageUrl;
            Attacks = attacks;
            Drops = drops;
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
            using (var itemFile = new StreamReader($"Resources{Path.DirectorySeparatorChar}Bosses.json"))
            {
                json = itemFile.ReadToEnd();
            }
            return JsonSerializer.Deserialize<IDictionary<string, Boss>>(json)!;
        }
    }
}
