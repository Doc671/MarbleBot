using Discord.Commands;

namespace MarbleBot.BaseClasses
{
    /// <summary> Stores info about a command. </summary>
    struct MBCommand 
    {
        public string Name;
        public string Desc;
        public string Usage;
        public string Example;
        public string Warning;
        public string Group;

        /*public MBCommand(CommandInfo info, UsageAttribute usage, string warning) {
            Name = info.Name;
            Desc = info.Summary;
            Usage = usage.Usage;
            Example = usage.Example;
            Warning = warning;
            Group = info.Module.Group;
        }*/
    }
}
