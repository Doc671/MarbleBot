﻿using Discord.Commands;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MarbleBot.Common
{
    public class MarbleBotGuild
    {
        public MarbleBotGuild(ulong id)
        {
            Id = id;
        }

        [JsonConstructor]
        public MarbleBotGuild(ulong id, ulong announcementChannel, string? appealFormLink, ulong autoresponseChannel,
                              string color, string prefix, List<ulong> roles, List<ulong> usableChannels,
                              string? warningSheetLink) : this(id)
        {
            AnnouncementChannel = announcementChannel;
            AppealFormLink = appealFormLink;
            AutoresponseChannel = autoresponseChannel;
            Color = color;
            Prefix = prefix;
            Roles = roles;
            UsableChannels = usableChannels;
            WarningSheetLink = warningSheetLink;
        }

        public ulong Id { get; }
        public ulong AnnouncementChannel { get; set; }
        public string? AppealFormLink { get; set; }
        public ulong AutoresponseChannel { get; set; }
        public string Color { get; set; } = "607D8B";
        public string Prefix { get; set; } = "mb/";
        public List<ulong> Roles { get; } = new();
        public List<ulong> UsableChannels { get; } = new();
        public string? WarningSheetLink { get; set; }

        public static MarbleBotGuild Find(SocketCommandContext context)
        {
            IDictionary<ulong, MarbleBotGuild> obj = GetGuilds();

            return obj.ContainsKey(context.Guild.Id)
                ? obj[context.Guild.Id]
                : new MarbleBotGuild(context.Guild.Id);
        }

        public static IDictionary<ulong, MarbleBotGuild> GetGuilds()
        {
            string json; using (var itemFile = new StreamReader($"Data{Path.DirectorySeparatorChar}Guilds.json"))
            {
                json = itemFile.ReadToEnd();
            }
            return JsonSerializer.Deserialize<IDictionary<string, MarbleBotGuild>>(json)!
                    .ToDictionary(pair => ulong.Parse(pair.Key), pair => pair.Value);
        }

        public static void UpdateGuild(MarbleBotGuild guild)
        {
            IDictionary<ulong, MarbleBotGuild> guildsDict = GetGuilds();

            if (guildsDict.ContainsKey(guild.Id))
            {
                guildsDict.Remove(guild.Id);
            }

            guildsDict.Add(guild.Id, guild);
            using var guildWriter = new StreamWriter($"Data{Path.DirectorySeparatorChar}Guilds.json");
            using var guildJsonWriter = new Utf8JsonWriter(guildWriter.BaseStream, new JsonWriterOptions { Indented = true });
            JsonSerializer.Serialize(guildJsonWriter, guildsDict);
        }
    }
}
