namespace MarbleBot.BaseClasses
{
    public class Boss {
        public string Name;
        public int HP;
        public readonly int MaxHP;
        public Difficulty Difficulty;
        public Attack[] Attacks;
        public string ImageUrl;

        public static Boss Empty = new Boss("", 0, Difficulty.None, "", new Attack[] { Attack.Empty });

        public void ResetHP()
        {
            HP = MaxHP;
        }

        public Boss(string name, int hp, Difficulty diff, string ImgUrl, Attack[] atks) {
            Name = name;
            HP = hp;
            MaxHP = hp;
            Difficulty = diff;
            Attacks = atks;
            ImageUrl = ImgUrl;
        }
    }
}