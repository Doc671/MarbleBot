using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MarbleBot.BaseClasses;
using MarbleBot.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MarbleBot
{
    public abstract class MarbleBotModule : ModuleBase<SocketCommandContext>
    {
        // Server IDs
        public const ulong CM = 223616088263491595; // Community Marble
        public const ulong THS = 224277738608001024; // The Hat Stoar
        public const ulong THSC = 318053169999511554; // The Hat Stoar Crew
        public const ulong VFC = 394086559676235776; // Vinh Fan Club
        public const ulong MT = 408694288604463114; // Melmon Test

        /// <summary> Gets colour for embed depending on server </summary>
        public static Color GetColor(SocketCommandContext Context) {
            Color coloure;
            var id = 0ul;
            if (!Context.IsPrivate) id = Context.Guild.Id;
            switch (id) {
                case CM: coloure = Color.Teal; break;
                case THS: coloure = Color.Orange; break;
                case MT: coloure = Color.DarkGrey; break;
                case VFC: coloure = Color.Blue; break;
                case THSC: goto case THS;
                default: coloure = Color.DarkerGrey; break;
            }
            return coloure;
        }

        /// <summary> Gets a date string </summary>
        public static string GetDateString(TimeSpan dateTime) {
            var output = new StringBuilder();
            if (dateTime.Days > 1) output.Append(dateTime.Days + " days, ");
            else if (dateTime.Days > 0) output.Append(dateTime.Days + " day, ");
            if (dateTime.Hours > 1) output.Append(dateTime.Hours + " hours, ");
            else if (dateTime.Hours > 0) output.Append(dateTime.Hours + " hour, ");
            if (dateTime.Minutes > 1) output.Append(dateTime.Minutes + " minutes ");
            else if (dateTime.Minutes > 0) output.Append(dateTime.Minutes + " minute ");
            if (dateTime.Seconds > 1) {
                if (dateTime.Minutes > 0) output.Append("and " + dateTime.Seconds + " seconds");
                else output.Append(dateTime.Seconds + " seconds");
            } else if (dateTime.Seconds > 0) {
                if (dateTime.Minutes > 0) output.Append("and " + dateTime.Seconds + " second");
                else output.Append(dateTime.Seconds + " second");
            } else if (dateTime.TotalSeconds < 1) {
                if (dateTime.Minutes > 0) output.Append("and <1 second");
                else output.Append("<1 second");
            }
            return output.ToString();
        }

        /// <summary> Returns an item using its ID </summary> 
        public static Item GetItem(string searchTerm) {
            var item = new Item();
            if (int.TryParse(searchTerm, out int itemID)) {
                string json;
                using (var userFile = new StreamReader("Resources\\Items.json")) json = userFile.ReadToEnd();
                var obj = JObject.Parse(json);
                if (obj[itemID.ToString("000")] != null){
                    item = obj[itemID.ToString("000")].ToObject<Item>();
                    item.Id = itemID;
                    item.Description.Replace(';', ',');
                    if (item.Stage == 0) item.Stage = 1;
                    if (item.CraftingRecipe == null) item.CraftingRecipe = new Dictionary<string, int>();
                    return item;
                } else {
                    item.Id = -1;
                    return item;
                }
            } else {
                var newSearchTerm = searchTerm.ToLower().RemoveChar(' ');
                string json;
                using (var userFile = new StreamReader("Resources\\Items.json")) json = userFile.ReadToEnd();
                var obj = JObject.Parse(json);
                foreach (var objItemPair in obj) {
                    var objItem = objItemPair.Value.ToObject<Item>();
                    objItem.Id = int.Parse(objItemPair.Key);
                    if (objItem.Name.ToLower().Contains(newSearchTerm) || newSearchTerm.Contains(objItem.Name.ToLower())) {
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

        /// <summary> Returns an instance of a MBUser with the ID of the SocketGuildUser </summary>
        public static MBUser GetUser(SocketCommandContext context) {
            var obj = GetUsersObj();
            MBUser user;
            if (obj.ContainsKey(context.User.Id.ToString())) {
                user = obj[context.User.Id.ToString()].ToObject<MBUser>();
                if (string.IsNullOrEmpty(obj[context.User.Id.ToString()]?.ToString())) user.Items = new SortedDictionary<int, int>();
            } else {
                user = new MBUser() {
                    Name = context.User.Username,
                    Discriminator = context.User.Discriminator,
                };
            }
            return user;
        }

        public static MBUser GetUser(SocketCommandContext context, ulong Id) {
            var obj = GetUsersObj();
            MBUser user;
            if (obj.ContainsKey(Id.ToString())) {
                user = obj[Id.ToString()].ToObject<MBUser>();
                if (string.IsNullOrEmpty(obj[Id.ToString()]?.ToString())) user.Items = new SortedDictionary<int, int>();
            } else {
                user = new MBUser() {
                    Name = context.User.Username,
                    Discriminator = context.User.Discriminator,
                };
            }
            return user;
        }

        public static MBUser GetUser(SocketCommandContext context, JObject obj) {
            MBUser user;
            if (obj.ContainsKey(context.User.Id.ToString())) {
                user = obj[context.User.Id.ToString()].ToObject<MBUser>();
                if (string.IsNullOrEmpty(obj[context.User.Id.ToString()]?.ToString())) user.Items = new SortedDictionary<int, int>();
            } else {
                user = new MBUser() {
                    Name = context.User.Username,
                    Discriminator = context.User.Discriminator,
                };
            }
            return user;
        }

        public static MBUser GetUser(SocketCommandContext context, JObject obj, ulong id) {
            MBUser user;
            if (obj.ContainsKey(id.ToString())) {
                user = obj[id.ToString()].ToObject<MBUser>();
                if (string.IsNullOrEmpty(obj[context.User.Id.ToString()]?.ToString())) user.Items = new SortedDictionary<int, int>();
            } else {
                if (context.IsPrivate) {
                    user = new MBUser() {
                        Name = context.User.Username,
                        Discriminator = context.User.Discriminator,
                    };
                } else {
                    user = new MBUser() {
                        Name = context.Guild.GetUser(id).Username,
                        Discriminator = context.Guild.GetUser(id).Discriminator,
                    };
                }
            }
            return user;
        }

        public static JObject GetUsersObj() {
            string json;
            using (var users = new StreamReader("Users.json")) json = users.ReadToEnd();
            return JObject.Parse(json);
        }

        public async static Task Log(string log) => await Program.Log(log);

        /// <summary> Writes users to appropriate JSON file </summary>
        public static void WriteUsers(JObject obj) {
            using (var users = new JsonTextWriter(new StreamWriter("Users.json"))) {
                var serialiser = new JsonSerializer() { Formatting = Formatting.Indented };
                serialiser.Serialize(users, obj);
            }
        }

        public static void WriteUsers(JObject obj, SocketUser socketUser, MBUser mbUser) {
            mbUser.Name = socketUser.Username;
            mbUser.Discriminator = socketUser.Discriminator;
            obj.Remove(socketUser.Id.ToString());
            obj.Add(new JProperty(socketUser.Id.ToString(), JObject.FromObject(mbUser)));
            using (var users = new JsonTextWriter(new StreamWriter("Users.json"))) {
                var serialiser = new JsonSerializer() { Formatting = Formatting.Indented };
                serialiser.Serialize(users, obj);
            }
        } 
    }
}
