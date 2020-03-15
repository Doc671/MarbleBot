using Discord;
using Discord.Commands;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
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

        public static MarbleBotGuild Find(SocketCommandContext context)
        {
            var obj = GetGuilds();
            MarbleBotGuild guild;
            if (obj.ContainsKey(context.Guild.Id))
            {
                guild = obj[context.Guild.Id];
            }
            else
            {
                guild = new MarbleBotGuild(context.Guild.Id);
            }
            return guild;
        }

        public static IDictionary<ulong, MarbleBotGuild> GetGuilds()
        {
            string json;
            using (var itemFile = new StreamReader($"Data{Path.DirectorySeparatorChar}Guilds.json"))
            {
                json = itemFile.ReadToEnd();
            }
            return JsonConvert.DeserializeObject<IDictionary<ulong, MarbleBotGuild>>(json);
        }

        public static void UpdateGuild(MarbleBotGuild guild)
        {
            var guildsDict = GetGuilds();

            if (guildsDict.ContainsKey(guild.Id))
            {
                guildsDict.Remove(guild.Id);
            }

            guildsDict.Add(guild.Id, guild);
            using var guilds = new JsonTextWriter(new StreamWriter($"Data{Path.DirectorySeparatorChar}Guilds.json"));
            var serialiser = new JsonSerializer() { Formatting = Formatting.Indented };
            serialiser.Serialize(guilds, guildsDict);
        }

        public static void UpdateGuilds(IDictionary<ulong, MarbleBotGuild> guildsDict, IGuild socketGuild, MarbleBotGuild mbGuild)
        {
            if (guildsDict.ContainsKey(socketGuild.Id))
            {
                guildsDict.Remove(socketGuild.Id);
            }

            guildsDict.Add(socketGuild.Id, mbGuild);
            using var guilds = new JsonTextWriter(new StreamWriter($"Data{Path.DirectorySeparatorChar}Guilds.json"));
            var serialiser = new JsonSerializer() { Formatting = Formatting.Indented };
            serialiser.Serialize(guilds, guildsDict);
        }
    }
}
