using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace MarbleBot.Core
{
    /// <summary> Represents a guild. </summary>
    public class MarbleBotGuild
    {
        /// <summary> The ID of the guild. </summary>
        public ulong Id { get; set; }
        /// <summary> The channel where update announcements are posted. </summary>
        public ulong AnnouncementChannel { get; set; }
        /// <summary> The channel where autoresponses can be used. </summary>
        public ulong AutoresponseChannel { get; set; }
        /// <summary> The colour displayed in embeds. </summary>
        public string Color { get; set; } = "607D8B";
        /// <summary> The roles that can be given or taken by the role commands. </summary>
        public List<ulong> Roles { get; set; } = new List<ulong>();
        /// <summary> The channels where commands can be used. If empty, commands can be used anywhere in the guild. </summary>
        public List<ulong> UsableChannels { get; set; } = new List<ulong>();
        /// <summary> The link to the warning sheet on Google Sheets. </summary>
        public string WarningSheetLink { get; set; }

        /// <summary> Represents a guild. </summary>
        /// <param name="id"> The ID of the guild. </param>
        public MarbleBotGuild(ulong id) => Id = id;

        /// <summary> Represents a guild. </summary>
        /// <param name="id"> The ID of the guild. </param>
        /// <param name="announcementChannel"> The channel where update announcements are posted. </param>
        /// <param name="autoresponseChannel"> The channel where autoresponses can be used. </param>
        /// <param name="color"> The colour used in embeds. </param>
        /// <param name="roles"> The role list roles of the guild. </param>
        /// <param name="usableChannels"> The channels where commands can be used. If empty, commands can be used anywhere in the guild. </param>
        [JsonConstructor]
        public MarbleBotGuild(ulong id, ulong announcementChannel, ulong autoresponseChannel, string color,
                               IEnumerable<ulong> roles, IEnumerable<ulong> usableChannels,
                               string warningSheetLink = null)
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
