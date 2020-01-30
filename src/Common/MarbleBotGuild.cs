using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace MarbleBot.Common
{
    public class MarbleBotGuild
    {
        public ulong Id { get; set; } = 0;
        public ulong AnnouncementChannel { get; set; }
        public string? AppealFormLink { get; set; }
        public ulong AutoresponseChannel { get; set; }
        public string Color { get; set; } = "607D8B";
        public string Prefix { get; set; } = "mb/";
        public List<ulong> Roles { get; set; } = new List<ulong>();
        public List<ulong> UsableChannels { get; set; } = new List<ulong>();
        public string? WarningSheetLink { get; set; }

        public MarbleBotGuild(ulong id) => Id = id;

        [JsonConstructor]
        public MarbleBotGuild(ulong id, ulong announcementChannel, ulong autoresponseChannel, string color,
IEnumerable<ulong> roles, IEnumerable<ulong> usableChannels,
string? warningSheetLink = null)
        {
            Id = id;
            AnnouncementChannel = announcementChannel;
            AutoresponseChannel = autoresponseChannel;
            Color = color;
            Roles = roles.ToList();
            UsableChannels = usableChannels.ToList();
            WarningSheetLink = warningSheetLink;
        }
    }
}
