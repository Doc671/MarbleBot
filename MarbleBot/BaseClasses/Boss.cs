namespace MarbleBot.BaseClasses
{
    public class Boss {
        public string Name { get; set; }
        public int HP { get; set; }
        public int MaxHP { get; }
        public Difficulty Difficulty { get; set; }
        public Attack[] Attacks { get; set; }
        public string ImageUrl { get; set; }

        public static Boss Empty = new Boss("", 0, Difficulty.None, "", new Attack[] { Attack.Empty });

        public void ResetHP() { HP = MaxHP; }
     
        public Boss(string name, int hp, Difficulty diff, string imgUrl, Attack[] atks) {
            Name = name;
            HP = hp;
            MaxHP = hp;
            Difficulty = diff;
            Attacks = atks;
            ImageUrl = imgUrl;
        }
    }
}