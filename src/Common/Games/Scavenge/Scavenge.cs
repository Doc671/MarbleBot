using Discord;
using Discord.Commands;
using MarbleBot.Extensions;
using MarbleBot.Modules;
using MarbleBot.Modules.Games.Services;
using MarbleBot.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarbleBot.Common
{
    /// <summary> Represents a scavenge game. </summary>
    public class Scavenge : IMarbleBotGame
    {
        /// <summary> The scavenge session. </summary>
        public Task? Actions { get; set; }
        /// <summary> The ID of the user performing the command. </summary>
        public ulong Id { get; set; }
        /// <summary> The items currently available during the game. </summary>
        public Queue<Item> Items { get; set; } = new Queue<Item>();
        /// <summary> The location of the scavenge. </summary>
        public ScavengeLocation Location { get; set; }
        /// <summary> The ores currently available during the game. </summary>
        public Queue<Item> Ores { get; set; } = new Queue<Item>();
        /// <summary> The items that have already been grabbed/sold. </summary>
        public Queue<Item> UsedItems { get; set; } = new Queue<Item>();
        /// <summary> The ores that have already been drilled. </summary>
        public Queue<Item> UsedOres { get; set; } = new Queue<Item>();

        private bool _disposed = false;
        private bool _itemHasAppeared = false;
        private bool _oreHasAppeared = false;
        private readonly SocketCommandContext _context;
        private readonly IUserMessage _originalMessage;
        private readonly GamesService _gamesService;
        private readonly RandomService _randomService;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _gamesService.ScavengeInfo.TryRemove(Id, out _);
            if (disposing && Actions != null)
            {
                Actions.Wait();
                Actions.Dispose();
            }
        }

        public Scavenge(SocketCommandContext context, GamesService gamesService, RandomService randomService, ScavengeLocation location, IUserMessage message)
        {
            _context = context;
            _gamesService = gamesService;
            _randomService = randomService;
            Actions = Task.Run(async () => { await Session(); });
            Id = context.User.Id;
            Location = location;
            _originalMessage = message;
        }

        /// <summary> The scavenge session. </summary>
        private async Task Session()
        {
            var stopwatch = new Stopwatch();
            var collectableItems = new List<Item>();
            var itemObject = MarbleBotModule.GetItemsObject();
            var items = itemObject.ToObject<Dictionary<string, Item>>()!;
            foreach (var itemPair in items)
            {
                if (itemPair.Value.ScavengeLocation == Location)
                {
                    var outputItem = itemPair.Value;
                    outputItem = new Item(outputItem, uint.Parse(itemPair.Key));
                    collectableItems.Add(outputItem);
                }
            }

            stopwatch.Start();
            do
            {
                await Task.Delay(8000);
                if (_randomService.Rand.Next(0, 5) < 4)
                {
                    var item = collectableItems[_randomService.Rand.Next(0, collectableItems.Count)];
                    if (item.Name.Contains("Ore"))
                    {
                        Ores.Enqueue(item);
                        _oreHasAppeared = true;
                    }
                    else
                    {
                        Items.Enqueue(item);
                        _itemHasAppeared = true;
                    }

                    await UpdateEmbed();
                }
            } while (stopwatch.Elapsed.TotalSeconds < 63);
            stopwatch.Stop();

            await OnGameEnd();
        }

        public async Task OnGameEnd()
        {
            var usersObject = MarbleBotModule.GetUsersObject();
            var user = MarbleBotModule.GetUser(_context, usersObject);
            user.LastScavenge = DateTime.UtcNow;
            foreach (var item in Items)
            {
                if (item.Name.Contains("Ore"))
                {
                    continue;
                }

                if (user.Items.ContainsKey(item.Id))
                {
                    user.Items[item.Id]++;
                }
                else
                {
                    user.Items.Add(item.Id, 1);
                }
            }
            MarbleBotModule.WriteUsers(usersObject, _context.User, user);

            await UpdateEmbed(true, user.Stage);

            Dispose(true);
        }

        /// <summary> Updates the original message with the current items and ores available. </summary>
        /// <param name="gameEnded"> Whether the game has ended. </param>
        /// <param name="stage"> The stage of the user. </param>
        public async Task UpdateEmbed(bool gameEnded = false, int stage = 1)
        {
            bool first = false;
            var itemOutput = new StringBuilder();
            foreach (var item in UsedItems)
            {
                itemOutput.AppendLine($"~~{item.Name}~~");
            }

            foreach (var item in Items)
            {
                itemOutput.AppendLine(first || gameEnded ? item.Name : $"**{item.Name}**");
                first = true;
            }

            first = false;
            var oreOutput = new StringBuilder();
            foreach (var ore in UsedOres)
            {
                oreOutput.AppendLine($"~~{ore.Name}~~");
            }

            foreach (var ore in Ores)
            {
                oreOutput.AppendLine(first || gameEnded ? ore.Name : $"**{ore.Name}**");
                first = true;
            }

            var fields = new List<EmbedFieldBuilder>();

            if (_itemHasAppeared)
            {
                fields.Add(new EmbedFieldBuilder()
                    .WithName("Items")
                    .WithValue($"{itemOutput.ToString()}{(gameEnded ? "" : "\nUse `mb/scavenge grab` to add the bolded item to your inventory or use `mb/scavenge sell` to sell it. ")}"));
            }

            if (_oreHasAppeared)
            {
                fields.Add(new EmbedFieldBuilder()
                    .WithName("Ores")
                    .WithValue($"{oreOutput.ToString()}{(gameEnded ? "" : "\nUse `mb/scavenge drill` to add the bolded ore to your inventory. A drill is required to drill ores.")}"));
            }

            var embed = _originalMessage.Embeds.First();
            await _originalMessage.ModifyAsync(m => m.Embed = new EmbedBuilder()
            {
                Color = embed.Color,
                Description = gameEnded ? stage == 1 ? "The scavenge session is over! Any remaining items have been added to your inventory!"
                    : "The scavenge session is over! Any remaining non-ore items have been added to your inventory!"
                    : "Scavenge session ongoing.",
                Fields = fields,
                Timestamp = embed.Timestamp,
                Title = $"Item Scavenge: {Enum.GetName(typeof(ScavengeLocation), Location)!.CamelToTitleCase()}"
            }.Build());
        }

        ~Scavenge() => Dispose(false);
    }
}
