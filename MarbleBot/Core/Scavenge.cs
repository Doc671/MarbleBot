using Discord.Commands;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MarbleBot.Core
{
    /// <summary> Represents a scavenge game. </summary>
    public class Scavenge : IDisposable
    {
        /// <summary> The scavenge session. </summary>
        public Task Actions { get; set; }
        /// <summary> The items currently available during the game. </summary>
        public Queue<Item> Items { get; set; } = new Queue<Item>();
        /// <summary> The location of the scavenge. </summary>
        public ScavengeLocation Location { get; set; }

        private bool _disposed = false;

        public void Dispose() => Dispose(true);

        private void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing) Actions.Dispose();
            Items = null;
            _disposed = true;
        }

        public Scavenge(SocketCommandContext context, ScavengeLocation location)
        {
            Actions = Task.Run(async () => { await ScavengeSessionAsync(context); });
            Location = location;
        }

        /// <summary> The scavenge session function. </summary>
        public async Task ScavengeSessionAsync(SocketCommandContext context)
        {
            var startTime = DateTime.UtcNow;
            var collectableItems = new List<Item>();
            string json;
            using (var users = new StreamReader("Resources\\Items.json")) json = users.ReadToEnd();
            var obj = JObject.Parse(json);
            var items = obj.ToObject<Dictionary<string, Item>>();
            foreach (var itemPair in items)
            {
                if (itemPair.Value.ScavengeLocation == Location)
                {
                    var outputItem = itemPair.Value;
                    outputItem.Id = int.Parse(itemPair.Key);
                    collectableItems.Add(outputItem);
                }
            }
            do
            {
                await Task.Delay(8000);
                if (Global.Rand.Next(0, 5) < 4)
                {
                    var item = collectableItems[Global.Rand.Next(0, collectableItems.Count)];
                    Items.Enqueue(item);
                    if (item.Name.Contains("Ore"))
                        await context.Channel.SendMessageAsync($"**{context.User.Username}**, you have found **{item.Name}** x**1**! Use `mb/scavenge drill` to mine it.");
                    else
                        await context.Channel.SendMessageAsync($"**{context.User.Username}**, you have found **{item.Name}** x**1**! Use `mb/scavenge grab` to keep it or `mb/scavenge sell` to sell it.");
                }
            } while (!(DateTime.UtcNow.Subtract(startTime).TotalSeconds > 63));

            string json2;
            using (var users = new StreamReader("Data\\Users.json")) json2 = users.ReadToEnd();
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

            if (user.Stage > 1)
                await context.Channel.SendMessageAsync("The scavenge session is over! Any remaining non-ore items have been added to your inventory!");
            else
                await context.Channel.SendMessageAsync("The scavenge session is over! Any remaining items have been added to your inventory!");

            Dispose(true);
        }

        ~Scavenge() => Dispose(true);
    }
}
