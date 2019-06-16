using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace MarbleBot.Core
{   
    /// <summary> Represents a server. </summary>
    public class MBServer
    {
        /// <summary> The ID of the server. </summary>
        public ulong Id { get; set; }
        /// <summary> The channel where update announcements are posted. </summary>
        public ulong AnnouncementChannel { get; set; }
        /// <summary> The channel where autoresponses can be used. </summary>
        public ulong AutoresponseChannel { get; set; }
        /// <summary> The colour displayed in embeds. </summary>
        public string Color { get; set; }
        /// <summary> The roles that can be given or taken by the role commands. </summary>
        public List<ulong> Roles { get; set; }
        /// <summary> The channels where commands can be used. If empty, commands can be used anywhere in the server. </summary>
        public List<ulong> UsableChannels { get; set; }

        /// <summary> Represents a server. </summary>
        /// <param name="id"> The ID of the server. </param>
        public MBServer(ulong id)
        {
            Id = id;
            AnnouncementChannel = 0;
            AutoresponseChannel = 0;
            Color = "607D8B";
            Roles = new List<ulong>(new ulong[0]);
            UsableChannels = new List<ulong>(new ulong[0]);
        }

        /// <summary> Represents a server. </summary>
        /// <param name="id"> The ID of the server. </param>
        /// <param name="announcementChannel"> The channel where update announcements are posted. </param>
        /// <param name="autoresponseChannel"> The channel where autoresponses can be used. </param>
        /// <param name="color"> The colour used in embeds. </param>
        /// <param name="roles"> The role list roles of the server. </param>
        /// <param name="usableChannels"> The channels where commands can be used. If empty, commands can be used anywhere in the server. </param>
        [JsonConstructor]
        public MBServer(ulong id, ulong announcementChannel, ulong autoresponseChannel, string color, IEnumerable<ulong> roles, IEnumerable<ulong> usableChannels)
        {
            Id = id;
            AnnouncementChannel = announcementChannel;
            AutoresponseChannel = autoresponseChannel;
            Color = color;
            Roles = roles.ToList();
            UsableChannels = usableChannels.ToList();
        }
    }
}
