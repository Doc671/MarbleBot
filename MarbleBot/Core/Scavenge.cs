using Discord;
using Discord.Commands;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarbleBot.Core
{
    /// <summary> Represents a scavenge game. </summary>
    public class Scavenge : IDisposable
    {
        /// <summary> The scavenge session. </summary>
        public Task Actions { get; set; }
        /// <summary> The ID of the user performing the command. </summary>
        public ulong Id { get; set; }
        /// <summary> The items currently available during the game. </summary>
        public Queue<Item> Items { get; set; } = new Queue<Item>();
        /// <summary> The location of the scavenge. </summary>
        public ScavengeLocation Location { get; set; }
        /// <summary> The ores currently available during the game. </summary>
        public Queue<Item> Ores { get; set; } = new Queue<Item>();
        /// <summary> The original message sent to modify. </summary>

        private bool _disposed = false;
        private readonly IUserMessage _originalMessage;

        public void Dispose() => Dispose(true);

        private void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;
            Global.ScavengeInfo.TryRemove(Id, out _);
            if (disposing)
            {
                Actions.Wait();
                Actions.Dispose();
            }
        }

        public Scavenge(SocketCommandContext context, ScavengeLocation location, IUserMessage message)
        {
            Actions = Task.Run(async () => { await ScavengeSessionAsync(context); });
            Id = context.User.Id;
            Location = location;
            _originalMessage = message;
        }

        /// <summary> The scavenge session function. </summary>
        public async Task ScavengeSessionAsync(SocketCommandContext context)
        {
            var startTime = DateTime.UtcNow;
            var collectableItems = new List<Item>();
            string json;
            using (var users = new StreamReader($"Resources{Path.DirectorySeparatorChar}Items.json")) json = users.ReadToEnd();
            var obj = JObject.Parse(json);
            var items = obj.ToObject<Dictionary<string, Item>>();
            foreach (var itemPair in items)
            {
                if (itemPair.Value.ScavengeLocation == Location)
                {
                    var outputItem = itemPair.Value;
                    outputItem = new Item(outputItem, int.Parse(itemPair.Key));
                    collectableItems.Add(outputItem);
                }
            }
            do
            {
                await Task.Delay(8000);
                if (Global.Rand.Next(0, 5) < 4)
                {
                    var item = collectableItems[Global.Rand.Next(0, collectableItems.Count)];
                    if (item.Name.Contains("Ore"))
                        Ores.Enqueue(item);
                    else
                        Items.Enqueue(item);
                    await UpdateEmbedAsync();
                }
            } while (!(DateTime.UtcNow.Subtract(startTime).TotalSeconds > 63));

            string json2;
            using (var users = new StreamReader($"Data{Path.DirectorySeparatorChar}Users.json")) json2 = users.ReadToEnd();
            var obj2 = JObject.Parse(json2);
            var user = MarbleBotModule.GetUser(context, obj2);
            user.LastScavenge = DateTime.UtcNow;
            foreach (var item in Items)
            {
                if (item.Name.Contains("Ore")) continue;
                if (user.Items.ContainsKey(item.Id)) user.Items[item.Id]++;
                else user.Items.Add(item.Id, 1);
            }
            MarbleBotModule.WriteUsers(obj2, context.User, user);

            await UpdateEmbedAsync(true, user.Stage);

            Dispose(true);
        }

        internal async Task UpdateEmbedAsync(bool gameEnded = false, int stage = 1)
        {
            bool first = false;
            var itemOutput = new StringBuilder();
            foreach (var item in Items)
            {
                itemOutput.AppendLine(first || gameEnded ? item.Name : $"**{item.Name}**");
                first = true;
            }

            first = false;
            var oreOutput = new StringBuilder();
            foreach (var ore in Ores)
            {
                oreOutput.AppendLine(first || gameEnded ? ore.Name : $"**{ore.Name}**");
                first = true;
            }

            var fields = new List<EmbedFieldBuilder>();
            if (Items.Count > 0)
                fields.Add(new EmbedFieldBuilder()
                    .WithName("Items")
                    .WithValue($"{itemOutput.ToString()}{(gameEnded ? "" : "\nUse `mb/scavenge grab` to add the bolded item to your inventory or use `mb/scavenge sell` to sell it. ")}"));
            if (Ores.Count > 0)
                fields.Add(new EmbedFieldBuilder()
                    .WithName("Ores")
                    .WithValue($"{oreOutput.ToString()}{(gameEnded ? "" : "\nUse `mb/scavenge drill` to add the bolded ore to your inventory. A drill is required to drill ores.")}"));

            var embed = _originalMessage.Embeds.First();
            await _originalMessage.ModifyAsync(m => m.Embed = new EmbedBuilder()
            {
                Color = embed.Color,
                Description = gameEnded ? stage == 1 ? "The scavenge session is over! Any remaining items have been added to your inventory!" 
                    : "The scavenge session is over! Any remaining non-ore items have been added to your inventory!" 
                    : "Scavenge session ongoing.",
                Fields = fields,
                Timestamp = embed.Timestamp,
                Title = $"Item Scavenge: {Enum.GetName(typeof(ScavengeLocation), Location)}"
            }.Build());
        }

        ~Scavenge() => Dispose(true);
    }
}
