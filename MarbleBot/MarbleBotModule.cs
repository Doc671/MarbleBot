using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MarbleBot.Core;
using MarbleBot.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarbleBot
{
    /// <summary> Represents a command module for MarbleBot. </summary>
    public abstract class MarbleBotModule : ModuleBase<SocketCommandContext>
    {
        // Server IDs
        protected internal const ulong CM = 223616088263491595; // Community Marble
        protected internal const ulong THS = 224277738608001024; // The Hat Stoar
        protected internal const ulong THSC = 318053169999511554; // The Hat Stoar Crew

        /// <summary> Gets colour for embed depending on server </summary>
        protected internal static Color GetColor(SocketCommandContext context)
        {
            if (context.IsPrivate) return Color.DarkerGrey;
            else return new Color(uint.Parse(GetServer(context).Color, System.Globalization.NumberStyles.HexNumber));
        }

        /// <summary> Gets a date string </summary>
        protected internal static string GetDateString(TimeSpan dateTime)
        {
            var output = new StringBuilder();
            if (dateTime.Days > 1) output.Append(dateTime.Days + " days, ");
            else if (dateTime.Days > 0) output.Append(dateTime.Days + " day, ");
            if (dateTime.Hours > 1) output.Append(dateTime.Hours + " hours, ");
            else if (dateTime.Hours > 0) output.Append(dateTime.Hours + " hour, ");
            if (dateTime.Minutes > 1) output.Append(dateTime.Minutes + " minutes ");
            else if (dateTime.Minutes > 0) output.Append(dateTime.Minutes + " minute ");
            if (dateTime.Seconds > 1)
            {
                if (dateTime.Minutes > 0) output.Append("and " + dateTime.Seconds + " seconds");
                else output.Append(dateTime.Seconds + " seconds");
            }
            else if (dateTime.Seconds > 0)
            {
                if (dateTime.Minutes > 0) output.Append("and " + dateTime.Seconds + " second");
                else output.Append(dateTime.Seconds + " second");
            }
            else if (dateTime.TotalSeconds < 1)
            {
                if (dateTime.Minutes > 0) output.Append("and <1 second");
                else output.Append("<1 second");
            }
            return output.ToString();
        }

        /// <summary> Returns an item using its ID </summary> 
        protected internal static T GetItem<T>(string searchTerm) where T : Item
        {
            T item;
            if (uint.TryParse(searchTerm, out uint itemId))
            {
                var obj = GetItemsObject();
                if (obj[itemId.ToString("000")] != null)
                {
                    item = obj[itemId.ToString("000")].ToObject<T>();
                    item.Id = itemId;
                    return item;
                }
                else return null;
            }
            else
            {
                var newSearchTerm = searchTerm.ToLower().RemoveChar(' ');
                string json;
                using (var userFile = new StreamReader($"Resources{Path.DirectorySeparatorChar}Items.json")) json = userFile.ReadToEnd();
                var obj = JObject.Parse(json);
                foreach (var objItemPair in obj)
                {
                    item = objItemPair.Value.ToObject<T>();
                    if (item.Name.ToLower().Contains(newSearchTerm) || newSearchTerm.Contains(item.Name.ToLower()))
                    {
                        item.Id = itemId;
                        return item;
                    }
                }
                return null;
            }
        }

        /// <summary> Returns a JObject containing all the items. </summary>
        protected internal static JObject GetItemsObject()
        {
            string json;
            using (var itemFile = new StreamReader($"Resources{Path.DirectorySeparatorChar}Items.json"))
                json = itemFile.ReadToEnd();
            return JObject.Parse(json);
        }

        /// <summary> Returns a MBServer object with the ID of the current server. </summary>
        protected internal static MarbleBotServer GetServer(SocketCommandContext context)
        => Global.Servers.Find(s => s.Id == context.Guild.Id);

        /// <summary> Returns an instance of a MBUser with the ID of the SocketGuildUser. </summary>
        protected internal static MarbleBotUser GetUser(SocketCommandContext context)
        {
            var obj = GetUsersObject();
            MarbleBotUser user;
            if (obj.ContainsKey(context.User.Id.ToString()))
            {
                user = obj[context.User.Id.ToString()].ToObject<MarbleBotUser>();
                if (string.IsNullOrEmpty(obj[context.User.Id.ToString()]?.ToString())) user.Items = new SortedDictionary<uint, int>();
            }
            else
            {
                user = new MarbleBotUser()
                {
                    Name = context.User.Username,
                    Discriminator = context.User.Discriminator,
                };
            }
            return user;
        }

        /// <summary> Returns an instance of a MBUser with the given ID. </summary>
        protected internal static MarbleBotUser GetUser(SocketCommandContext context, ulong Id)
        {
            var obj = GetUsersObject();
            MarbleBotUser user;
            if (obj.ContainsKey(Id.ToString()))
            {
                user = obj[Id.ToString()].ToObject<MarbleBotUser>();
                if (string.IsNullOrEmpty(obj[Id.ToString()]?.ToString())) user.Items = new SortedDictionary<uint, int>();
            }
            else
            {
                user = new MarbleBotUser()
                {
                    Name = context.User.Username,
                    Discriminator = context.User.Discriminator,
                };
            }
            return user;
        }

        /// <summary> Returns an instance of a MBUser with the ID of the SocketGuildUser in the given JObject. </summary>
        protected internal static MarbleBotUser GetUser(SocketCommandContext context, JObject obj)
        {
            MarbleBotUser user;
            if (obj.ContainsKey(context.User.Id.ToString()))
            {
                user = obj[context.User.Id.ToString()].ToObject<MarbleBotUser>();
                if (string.IsNullOrEmpty(obj[context.User.Id.ToString()]?.ToString())) user.Items = new SortedDictionary<uint, int>();
            }
            else
            {
                user = new MarbleBotUser()
                {
                    Name = context.User.Username,
                    Discriminator = context.User.Discriminator,
                };
            }
            return user;
        }

        /// <summary> Returns an instance of a MBUser with the given ID in the given JObject. </summary>
        protected internal static MarbleBotUser GetUser(SocketCommandContext context, JObject obj, ulong id)
        {
            MarbleBotUser user;
            if (obj.ContainsKey(id.ToString()))
            {
                user = obj[id.ToString()].ToObject<MarbleBotUser>();
                if (string.IsNullOrEmpty(obj[context.User.Id.ToString()]?.ToString())) user.Items = new SortedDictionary<uint, int>();
            }
            else
            {
                if (context.IsPrivate)
                {
                    user = new MarbleBotUser()
                    {
                        Name = context.User.Username,
                        Discriminator = context.User.Discriminator,
                    };
                }
                else
                {
                    user = new MarbleBotUser()
                    {
                        Name = context.Guild.GetUser(id).Username,
                        Discriminator = context.Guild.GetUser(id).Discriminator,
                    };
                }
            }
            return user;
        }

         /// <summary> Returns a JObject containing all the users. </summary>
        protected internal static JObject GetUsersObject()
        {
            string json;
            using (var users = new StreamReader($"Data{Path.DirectorySeparatorChar}Users.json")) json = users.ReadToEnd();
            return JObject.Parse(json);
        }

        /// <summary> Logs to the console and the online logs. </summary>
        protected internal static void Log(string log) => Program.Log(log);

        /// <summary> Returns a string that indicates the user's Stage is too low. </summary>
        protected internal static string StageTooHighString() 
        => (Global.Rand.Next(0, 4)) switch
            {
                0 => "*Your inexperience blinds you...*",
                1 => "*Your vision is blurry...*",
                2 => "*Screams echo in your head...*",
                _=> "*Your mind is wracked with pain...*",
            };

        /// <summary> Writes servers to the appropriate file. </summary>
        protected internal static void WriteServers()
        {
            using var servers = new JsonTextWriter(new StreamWriter($"Data{Path.DirectorySeparatorChar}Servers.json"));
            var serialiser = new JsonSerializer() { Formatting = Formatting.Indented };
            var dict = Global.Servers.ToDictionary(s => s.Id);
            serialiser.Serialize(servers, dict);
        }

        /// <summary> Writes users to the appropriate JSON file/ </summary>
        protected internal static void WriteUsers(JObject obj)
        {
            using var users = new JsonTextWriter(new StreamWriter($"Data{Path.DirectorySeparatorChar}Users.json"));
            var serialiser = new JsonSerializer() { Formatting = Formatting.Indented };
            serialiser.Serialize(users, obj);
        }

        protected internal static void WriteUsers(JObject obj, SocketUser socketUser, MarbleBotUser mbUser)
        {
            mbUser.Name = socketUser.Username;
            mbUser.Discriminator = socketUser.Discriminator;
            obj.Remove(socketUser.Id.ToString());
            obj.Add(new JProperty(socketUser.Id.ToString(), JObject.FromObject(mbUser)));
            using var users = new JsonTextWriter(new StreamWriter($"Data{Path.DirectorySeparatorChar}Users.json"));
            var serialiser = new JsonSerializer() { Formatting = Formatting.Indented };
            serialiser.Serialize(users, obj);
        }
    }
}
