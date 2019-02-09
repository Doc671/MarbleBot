namespace MarbleBot.BaseClasses
{
    public class Marble
    {
        public ulong Id { get; set; }
        public string Name { get; set; }
        public int HP { get; set; }
        public int MaxHP { get; set; }
        public int BossHits { get; set; }
        public int PUHits { get; set; }
        public bool Cloned { get; set; }

        public override string ToString()
        {
            return $"{Name} HP: {HP}/20 [{Id}] BH: {BossHits} PH: {PUHits} Cloned: {Cloned}";
        }
    }
}
