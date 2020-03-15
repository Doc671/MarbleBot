using MarbleBot.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace MarbleBot.Common
{
    public class Boss
    {
        public string Name { get; set; }
        private int _health;
        public int Health
        {
            get => _health;
            set => _health = value > MaxHealth ? MaxHealth : value < 1 ? 0 : value;
        }
        public int MaxHealth { get; }
        public IReadOnlyCollection<Attack> Attacks { get; set; }
        public Difficulty Difficulty { get; set; }
        public IReadOnlyCollection<BossDrops> Drops { get; set; }
        public string ImageUrl { get; set; }
        public int Stage { get; set; }

        public static Boss Empty => new Boss("", 0, Difficulty.None, 1, "",
            new Attack[] { Attack.Empty },
            new BossDrops[] { new BossDrops(0, 0, 0, 0) }
        );

        public Boss(string name, int health, Difficulty diff, int stage, string imgUrl, IReadOnlyCollection<Attack> atks, IReadOnlyCollection<BossDrops> itemDrops)
        {
            Name = name;
            _health = health;
            MaxHealth = health;
            Difficulty = diff;
            Stage = stage;
            Attacks = atks;
            Drops = itemDrops;
            ImageUrl = imgUrl;
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
