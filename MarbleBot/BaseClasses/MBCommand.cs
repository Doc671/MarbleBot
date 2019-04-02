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
    }
}
