using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MarbleBot.Common;
using MarbleBot.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MarbleBot.Modules
{
    public abstract class MarbleBotModule : ModuleBase<SocketCommandContext>
    {
        // Server IDs
        protected internal const ulong CommunityMarble = 223616088263491595;
        protected internal const ulong TheHatStoar = 224277738608001024;

        protected internal const string UnitOfMoney = "<:unitofmoney:372385317581488128>";

        protected Logger Logger { get; } = LogManager.GetCurrentClassLogger();

        protected internal static Color GetColor(SocketCommandContext context)
        {
            if (context.IsPrivate)
            {
                return Color.DarkerGrey;
            }
            else
            {
                return new Color(uint.Parse(GetGuild(context).Color, System.Globalization.NumberStyles.HexNumber));
            }
        }

        protected internal static string GetDateString(TimeSpan dateTime)
        {
            var output = new StringBuilder();
            if (dateTime.Days > 1)
            {
                output.Append(dateTime.Days + " days, ");
            }
            else if (dateTime.Days > 0)
            {
                output.Append(dateTime.Days + " day, ");
            }

            if (dateTime.Hours > 1)
            {
                output.Append(dateTime.Hours + " hours, ");
            }
            else if (dateTime.Hours > 0)
            {
                output.Append(dateTime.Hours + " hour, ");
            }

            if (dateTime.Minutes > 1)
            {
                output.Append(dateTime.Minutes + " minutes ");
            }
            else if (dateTime.Minutes > 0)
            {
                output.Append(dateTime.Minutes + " minute ");
            }

            if (dateTime.Seconds > 1)
            {
                if (dateTime.Minutes > 0)
                {
                    output.Append("and " + dateTime.Seconds + " seconds");
                }
                else
                {
                    output.Append(dateTime.Seconds + " seconds");
                }
            }
            else if (dateTime.Seconds > 0)
            {
                if (dateTime.Minutes > 0)
                {
                    output.Append("and " + dateTime.Seconds + " second");
                }
                else
                {
                    output.Append(dateTime.Seconds + " second");
                }
            }
            else if (dateTime.TotalSeconds < 1)
            {
                if (dateTime.Minutes > 0)
                {
                    output.Append("and <1 second");
                }
                else
                {
                    output.Append("<1 second");
                }
            }
            return output.ToString();
        }

        protected internal static T GetItem<T>(string searchTerm) where T : Item
        {
            T item;
            if (int.TryParse(searchTerm, out int itemId))
            {
                var obj = GetItemsObject();
                if (obj[itemId.ToString("000")] != null)
                {
                    item = obj[itemId.ToString("000")]!.ToObject<T>()!;
                    item.Id = itemId;
                    return item;
                }
                else
                {
                    throw new Exception("The requested item could not be found.");
                }
            }
            else
            {
                var newSearchTerm = searchTerm.ToLower().RemoveChar(' ');
                string json;
                using (var userFile = new StreamReader($"Resources{Path.DirectorySeparatorChar}Items.json"))
                {
                    json = userFile.ReadToEnd();
                }

                var obj = JObject.Parse(json);
                foreach (var objItemPair in obj)
                {
                    item = objItemPair.Value!.ToObject<T>()!;
                    if (item.Name.ToLower().Contains(newSearchTerm) || newSearchTerm.Contains(item.Name.ToLower()))
                    {
                        item.Id = int.Parse(objItemPair.Key);
                        return item;
                    }
                }
                throw new Exception("The requested item could not be found.");
            }
        }

        protected internal static JObject GetItemsObject()
        {
            string json;
            using (var itemFile = new StreamReader($"Resources{Path.DirectorySeparatorChar}Items.json"))
            {
                json = itemFile.ReadToEnd();
            }

            return JObject.Parse(json);
        }

        protected internal static MarbleBotGuild GetGuild(SocketCommandContext context)
        {
            var obj = GetGuildsObject();
            MarbleBotGuild guild;
            if (obj.ContainsKey(context.Guild.Id.ToString()))
            {
                guild = obj[context.Guild.Id.ToString()]!.ToObject<MarbleBotGuild>()!;
                guild.Id = context.Guild.Id;
            }
            else
            {
                guild = new MarbleBotGuild(context.Guild.Id);
            }
            return guild;
        }

        protected internal static JObject GetGuildsObject()
        {
            string json;
            using (var itemFile = new StreamReader($"Data{Path.DirectorySeparatorChar}Guilds.json"))
            {
                json = itemFile.ReadToEnd();
            }

            return JObject.Parse(json);
        }

        protected internal static MarbleBotUser GetUser(ICommandContext context)
        {
            var obj = GetUsersObject();
            MarbleBotUser user;
            if (obj.ContainsKey(context.User.Id.ToString()))
            {
                user = obj[context.User.Id.ToString()]!.ToObject<MarbleBotUser>()!;
                user.Id = context.User.Id;
                if (string.IsNullOrEmpty(obj[context.User.Id.ToString()]?.ToString()))
                {
                    user.Items = new SortedDictionary<int, int>();
                }
            }
            else
            {
                user = new MarbleBotUser()
                {
                    Id = context.User.Id,
                    Name = context.User.Username,
                    Discriminator = context.User.Discriminator,
                };
            }
            return user;
        }

        protected internal static MarbleBotUser GetUser(ICommandContext context, ulong id)
        {
            var obj = GetUsersObject();
            MarbleBotUser user;
            if (obj.ContainsKey(id.ToString()))
            {
                user = obj[id.ToString()]!.ToObject<MarbleBotUser>()!;
                user.Id = id;
                if (string.IsNullOrEmpty(obj[id.ToString()]?.ToString()))
                {
                    user.Items = new SortedDictionary<int, int>();
                }
            }
            else
            {
                user = new MarbleBotUser()
                {
                    Id = context.User.Id,
                    Name = context.User.Username,
                    Discriminator = context.User.Discriminator,
                };
            }
            return user;
        }

        protected internal static MarbleBotUser GetUser(ICommandContext context, JObject obj)
        {
            MarbleBotUser user;
            if (obj.ContainsKey(context.User.Id.ToString()))
            {
                user = obj[context.User.Id.ToString()]!.ToObject<MarbleBotUser>()!;
                user.Id = context.User.Id;
                if (string.IsNullOrEmpty(obj[context.User.Id.ToString()]?.ToString()))
                {
                    user.Items = new SortedDictionary<int, int>();
                }
            }
            else
            {
                user = new MarbleBotUser()
                {
                    Id = context.User.Id,
                    Name = context.User.Username,
                    Discriminator = context.User.Discriminator,
                };
            }
            return user;
        }

        protected internal static async Task<MarbleBotUser> GetUserAsync(ICommandContext context, JObject obj, ulong id)
        {
            MarbleBotUser user;
            if (obj.ContainsKey(id.ToString()))
            {
                user = obj[id.ToString()]!.ToObject<MarbleBotUser>()!;
                user.Id = id;
                if (string.IsNullOrEmpty(obj[context.User.Id.ToString()]?.ToString()))
                {
                    user.Items = new SortedDictionary<int, int>();
                }
            }
            else
            {
                user = new MarbleBotUser()
                {
                    Id = context.User.Id,
                    Name = (await context.Client.GetUserAsync(id)).Username,
                    Discriminator = (await context.Client.GetUserAsync(id)).Discriminator,
                };
            }
            return user;
        }

        protected internal static JObject GetUsersObject()
        {
            string json;
            using (var users = new StreamReader($"Data{Path.DirectorySeparatorChar}Users.json"))
            {
                json = users.ReadToEnd();
            }

            return JObject.Parse(json);
        }

        protected internal async Task<IUserMessage> SendErrorAsync(string messageContent)
            => await ReplyAsync($":warning: | {messageContent}");

        protected internal static string StageTooHighString()
            => (new Random().Next(0, 6)) switch
            {
                0 => "*Your inexperience blinds you...*",
                1 => "*Your vision is blurry...*",
                2 => "*Incomprehensible noises rattle in your head...*",
                3 => "*You sense a desk restricting your path...*",
                4 => "*You feel as if there is more to be done...*",
                _ => "*Your mind is wracked with pain...*",
            };

        protected internal static void WriteGuilds(JObject obj, SocketGuild socketGuild, MarbleBotGuild mbGuild)
        {
            if (obj.ContainsKey(socketGuild.Id.ToString()))
            {
                obj.Remove(socketGuild.Id.ToString());
            }

            obj.Add(new JProperty(socketGuild.Id.ToString(), JObject.FromObject(mbGuild)));
            using var guilds = new JsonTextWriter(new StreamWriter($"Data{Path.DirectorySeparatorChar}Guilds.json"));
            var serialiser = new JsonSerializer() { Formatting = Formatting.Indented };
            serialiser.Serialize(guilds, obj);
        }

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
