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

namespace MarbleBot
{
    public abstract class MarbleBotModule : ModuleBase<SocketCommandContext>
    {
        // Server IDs
        protected const ulong CM = 223616088263491595; // Community Marble
        protected const ulong THS = 224277738608001024; // The Hat Stoar
        protected const ulong THSC = 318053169999511554; // The Hat Stoar Crew
        protected const ulong VFC = 394086559676235776; // Vinh Fan Club
        protected const ulong MT = 408694288604463114; // Melmon Test

        // Gets colour for embed depending on server
        protected static Color GetColor(SocketCommandContext Context) {
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

        // Gets a date string
        protected static string GetDateString(TimeSpan dateTime) {
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

        // Returns an item using its ID
        protected static Item GetItem(string searchTerm) {
            var item = new Item();
            if (int.TryParse(searchTerm, out int itemID)) {
                var itemFound = false;
                using (var items = new StreamReader("Resources\\ShopItems.csv")) {
                    while (!items.EndOfStream) {
                        var properties = items.ReadLine().Split(',');
                        if (properties[0].ToInt() == itemID) {
                            itemFound = true;
                            item.Id = properties[0].ToInt();
                            item.Name = properties[1];
                            if (properties[2].ToLower().Contains("unsellable")) item.Price = -1;
                            else item.Price = properties[2].ToDecimal();
                            item.Description = properties[3];
                            item.OnSale = bool.Parse(properties[4]);
                            item.DiveCollectable = bool.Parse(properties[5]);
                        }
                    }
                }
                if (itemFound) return item;
                else {
                    item.Id = -1;
                    return item;
                }
            } else {
                item.Id = -2;
                return item;
            }
        }

        // Returns a MoneyUser with the ID of the user
        protected static MBUser GetUser(SocketCommandContext Context) {
            string json;
            using (var users = new StreamReader("Users.json")) json = users.ReadToEnd();
            var obj = JObject.Parse(json);
            MBUser User;
            if (obj.ContainsKey(Context.User.Id.ToString())) {
                User = obj[Context.User.Id.ToString()].ToObject<MBUser>();
                if (string.IsNullOrEmpty(obj[Context.User.Id.ToString()]?.ToString())) User.Items = new Dictionary<int, int>();
            } else {
                User = new MBUser() {
                    Name = Context.User.Username,
                    Discriminator = Context.User.Discriminator,
                };
            }
            return User;
        }

        protected static MBUser GetUser(SocketCommandContext Context, ulong Id) {
            string json;
            using (var users = new StreamReader("Users.json")) json = users.ReadToEnd();
            var obj = JObject.Parse(json);
            MBUser User;
            if (obj.ContainsKey(Id.ToString())) {
                User = obj[Id.ToString()].ToObject<MBUser>();
                if (string.IsNullOrEmpty(obj[Id.ToString()]?.ToString())) User.Items = new Dictionary<int, int>();
            } else {
                User = new MBUser() {
                    Name = Context.User.Username,
                    Discriminator = Context.User.Discriminator,
                };
            }
            return User;
        }

        protected static MBUser GetUser(SocketCommandContext Context, JObject obj) {
            MBUser User;
            if (obj.ContainsKey(Context.User.Id.ToString())) {
                User = obj[Context.User.Id.ToString()].ToObject<MBUser>();
                if (string.IsNullOrEmpty(obj[Context.User.Id.ToString()]?.ToString())) User.Items = new Dictionary<int, int>();
            } else {
                User = new MBUser() {
                    Name = Context.User.Username,
                    Discriminator = Context.User.Discriminator,
                };
            }
            return User;
        }

        protected static MBUser GetUser(SocketCommandContext Context, JObject obj, ulong id) {
            MBUser User;
            if (obj.ContainsKey(id.ToString())) {
                User = obj[id.ToString()].ToObject<MBUser>();
                if (string.IsNullOrEmpty(obj[Context.User.Id.ToString()]?.ToString())) User.Items = new Dictionary<int, int>();
            } else {
                if (Context.IsPrivate) {
                    User = new MBUser() {
                        Name = Context.User.Username,
                        Discriminator = Context.User.Discriminator,
                    };
                } else {
                    User = new MBUser() {
                        Name = Context.Guild.GetUser(id).Username,
                        Discriminator = Context.Guild.GetUser(id).Discriminator,
                    };
                }
            }
            return User;
        }

        // Writes users to appropriate JSON file
        protected static void WriteUsers(JObject obj) {
            using (var users = new StreamWriter("Users.json")) {
                using (var users2 = new JsonTextWriter(users)) {
                    var serialiser = new JsonSerializer() { Formatting = Formatting.Indented };
                    serialiser.Serialize(users2, obj);
                }
            }
        }

        protected static void WriteUsers(JObject obj, SocketUser socketUser, MBUser mbUser) {
            mbUser.Name = socketUser.Username;
            mbUser.Discriminator = socketUser.Discriminator;
            obj.Remove(socketUser.Id.ToString());
            obj.Add(new JProperty(socketUser.Id.ToString(), JObject.FromObject(mbUser)));
            using (var users = new StreamWriter("Users.json")) {
                using (var users2 = new JsonTextWriter(users)) {
                    var serialiser = new JsonSerializer() { Formatting = Formatting.Indented };
                    serialiser.Serialize(users2, obj);
                }
            }
        } 
    }
}
