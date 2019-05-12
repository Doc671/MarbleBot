using System.Collections.Generic;
using System.Linq;

namespace MarbleBot.BaseClasses
{
    /// <summary> Stores info about a command. </summary>
    public struct HelpCommand
    {
        /// <summary> The command's name. </summary>
        public string Name;
        /// <summary> The command's description. </summary>
        public string Desc;
        /// <summary> The command's usage. </summary>
        public string Usage;
        /// <summary> The command's aliases. </summary>
        public string[] Aliases;
        /// <summary> An example of the command being used. </summary>
        public string Example;
        /// <summary> A warning about the command. </summary>
        public string Warning;

        /// <summary> Stores info about a command. </summary>
        /// <param name="name"> The command's name. </param>
        /// <param name="desc"> The command's description. </param>
        /// <param name="usage"> The command's usage. </param>
        /// <param name="aliases"> The command's aliases. </param>
        /// <param name="example"> An example of the command being used. </param>
        /// <param name="warning"> A warning about the command. </param>
        public HelpCommand(string name = "",
                           string desc = "",
                           string usage = "",
                           IEnumerable<string> aliases = null,
                           string example = "",
                           string warning = "")
        {
            Name = name;
            Desc = desc;
            Usage = usage;
            Aliases = aliases == null ? new string[] { "" } : aliases.ToArray();
            Example = example;
            Warning = warning;
        }
    }
}
