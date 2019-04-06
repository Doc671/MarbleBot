using System.Collections.Generic;
using System.Linq;

namespace MarbleBot.BaseClasses
{
    /// <summary> Stores info about a command. </summary>
    public struct HelpCommand 
    {
        public string Name;
        public string Desc;
        public string Usage;
        public string[] Aliases;
        public string Example;
        public string Warning;

        public HelpCommand(string name = "",
                           string desc = "",
                           string usage = "",
                           IEnumerable<string> aliases = null,
                           string example = "",
                           string warning = "") {
            Name = name;
            Desc = desc;
            Usage = usage;
            Aliases = aliases == null ? new string[] { "" } : aliases.ToArray();
            Example = example;
            Warning = warning;
        }
    }
}
