namespace MarbleBot.BaseClasses
{
    public class Boss {
        public string Name { get; set; }
        private int _HP;
        public int HP
        {
            get { return _HP; }
            set { _HP = value > MaxHP ? MaxHP : value < 1 ? 0 : value; }
        }

        public int MaxHP { get; }
        public Difficulty Difficulty { get; set; }
        public Attack[] Attacks { get; set; }
        public string ImageUrl { get; set; }

        public static Boss Empty = new Boss("", 0, Difficulty.None, "", new Attack[] { Attack.Empty });

        public void ResetHP() { _HP = MaxHP; }
     
        public Boss(string name, int hp, Difficulty diff, string imgUrl, Attack[] atks) {
            Name = name;
            _HP = hp;
            MaxHP = hp;
            Difficulty = diff;
            Attacks = atks;
            ImageUrl = imgUrl;
        }
    }
}