using System;

namespace MarbleBot.BaseClasses
{
    public class Attack {
        public string Name;
        public byte Damage;
        public byte Accuracy;

        public static Attack Empty = new Attack("", 0, 0);
  
        public Attack(string name, byte pwr, byte acc) {
            Name = name;
            Damage = pwr;
            Accuracy = acc;
        }
    }
}