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
        protected internal static Item GetItem(string searchTerm)
        {
            var item = new Item();
            if (int.TryParse(searchTerm, out int itemID))
            {
                string json;
                using (var userFile = new StreamReader("Resources\\Items.json")) json = userFile.ReadToEnd();
                var obj = JObject.Parse(json);
                if (obj[itemID.ToString("000")] != null)
                {
                    item = obj[itemID.ToString("000")].ToObject<Item>();
                    item.Id = itemID;
                    item.Description.Replace(';', ',');
                    if (item.Stage == 0) item.Stage = 1;
                    if (item.CraftingRecipe == null) item.CraftingRecipe = new Dictionary<string, int>();
                    return item;
                }
                else
                {
                    item.Id = -1;
                    return item;
                }
            }
            else
            {
                var newSearchTerm = searchTerm.ToLower().RemoveChar(' ');
                string json;
                using (var userFile = new StreamReader("Resources\\Items.json")) json = userFile.ReadToEnd();
                var obj = JObject.Parse(json);
                foreach (var objItemPair in obj)
                {
                    var objItem = objItemPair.Value.ToObject<Item>();
                    objItem.Id = int.Parse(objItemPair.Key);
                    if (objItem.Name.ToLower().Contains(newSearchTerm) || newSearchTerm.Contains(objItem.Name.ToLower()))
                    {
                        item = objItem;
                        item.Description.Replace(';', ',');
                        if (item.Stage == 0) item.Stage = 1;
                        if (item.CraftingRecipe == null) item.CraftingRecipe = new Dictionary<string, int>();
                        return item;
                    }
                }
                item.Id = -2;
                return item;
            }
        }

        /// <summary> Returns a MBServer object with the ID of the current server. </summary>
        protected internal static MBServer GetServer(SocketCommandContext context)
        => Global.Servers.Value.Find(s => s.Id == context.Guild.Id);

        /// <summary> Returns an instance of a MBUser with the ID of the SocketGuildUser </summary>
        protected internal static MBUser GetUser(SocketCommandContext context)
        {
            var obj = GetUsersObj();
            MBUser user;
            if (obj.ContainsKey(context.User.Id.ToString()))
            {
                user = obj[context.User.Id.ToString()].ToObject<MBUser>();
                if (string.IsNullOrEmpty(obj[context.User.Id.ToString()]?.ToString())) user.Items = new SortedDictionary<int, int>();
            }
            else
            {
                user = new MBUser()
                {
                    Name = context.User.Username,
                    Discriminator = context.User.Discriminator,
                };
            }
            return user;
        }

        protected internal static MBUser GetUser(SocketCommandContext context, ulong Id)
        {
            var obj = GetUsersObj();
            MBUser user;
            if (obj.ContainsKey(Id.ToString()))
            {
                user = obj[Id.ToString()].ToObject<MBUser>();
                if (string.IsNullOrEmpty(obj[Id.ToString()]?.ToString())) user.Items = new SortedDictionary<int, int>();
            }
            else
            {
                user = new MBUser()
                {
                    Name = context.User.Username,
                    Discriminator = context.User.Discriminator,
                };
            }
            return user;
        }

        protected internal static MBUser GetUser(SocketCommandContext context, JObject obj)
        {
            MBUser user;
            if (obj.ContainsKey(context.User.Id.ToString()))
            {
                user = obj[context.User.Id.ToString()].ToObject<MBUser>();
                if (string.IsNullOrEmpty(obj[context.User.Id.ToString()]?.ToString())) user.Items = new SortedDictionary<int, int>();
            }
            else
            {
                user = new MBUser()
                {
                    Name = context.User.Username,
                    Discriminator = context.User.Discriminator,
                };
            }
            return user;
        }

        protected internal static MBUser GetUser(SocketCommandContext context, JObject obj, ulong id)
        {
            MBUser user;
            if (obj.ContainsKey(id.ToString()))
            {
                user = obj[id.ToString()].ToObject<MBUser>();
                if (string.IsNullOrEmpty(obj[context.User.Id.ToString()]?.ToString())) user.Items = new SortedDictionary<int, int>();
            }
            else
            {
                if (context.IsPrivate)
                {
                    user = new MBUser()
                    {
                        Name = context.User.Username,
                        Discriminator = context.User.Discriminator,
                    };
                }
                else
                {
                    user = new MBUser()
                    {
                        Name = context.Guild.GetUser(id).Username,
                        Discriminator = context.Guild.GetUser(id).Discriminator,
                    };
                }
            }
            return user;
        }

        protected internal static JObject GetUsersObj()
        {
            string json;
            using (var users = new StreamReader("Data\\Users.json")) json = users.ReadToEnd();
            return JObject.Parse(json);
        }

        protected internal static async Task Log(string log) => await Program.Log(log);

        protected internal static string StageTooHighString()
        {
            return (Global.Rand.Next(0, 4)) switch
            {
                0 => "*Your inexperience blinds you...*",
                1 => "*Your vision is blurry...*",
                2 => "*Screams echo in your head...*",
                _=> "*Your mind is wracked with pain...*",
            };
        }

        /// <summary> Writes servers to the appropriate file. </summary>
        protected internal static void WriteServers()
        {
            using (var servers = new JsonTextWriter(new StreamWriter("Data\\Servers.json")))
            {
                var serialiser = new JsonSerializer() { Formatting = Formatting.Indented };
                var dict = Global.Servers.Value.ToDictionary(s => s.Id);
                serialiser.Serialize(servers, dict);
            }
        }

        /// <summary> Writes users to the appropriate JSON file/ </summary>
        protected internal static void WriteUsers(JObject obj)
        {
            using (var users = new JsonTextWriter(new StreamWriter("Data\\Users.json")))
            {
                var serialiser = new JsonSerializer() { Formatting = Formatting.Indented };
                serialiser.Serialize(users, obj);
            }
        }

        protected internal static void WriteUsers(JObject obj, SocketUser socketUser, MBUser mbUser)
        {
            mbUser.Name = socketUser.Username;
            mbUser.Discriminator = socketUser.Discriminator;
            obj.Remove(socketUser.Id.ToString());
            obj.Add(new JProperty(socketUser.Id.ToString(), JObject.FromObject(mbUser)));
            using (var users = new JsonTextWriter(new StreamWriter("Data\\Users.json")))
            {
                var serialiser = new JsonSerializer() { Formatting = Formatting.Indented };
                serialiser.Serialize(users, obj);
            }
        }
    }
}
