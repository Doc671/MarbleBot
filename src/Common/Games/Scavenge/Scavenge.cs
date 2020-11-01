using Discord;
using Discord.Commands;
using MarbleBot.Extensions;
using MarbleBot.Modules.Games.Services;
using MarbleBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace MarbleBot.Common.Games.Scavenge
{
    public class Scavenge
    {
        public Queue<Item> Items { get; } = new Queue<Item>();
        public ScavengeLocation Location { get; }
        public Queue<Item> Ores { get; } = new Queue<Item>();
        public Queue<Item> UsedItems { get; } = new Queue<Item>();
        public Queue<Item> UsedOres { get; } = new Queue<Item>();

        private readonly List<Item> _collectableItems;
        private readonly SocketCommandContext _context;
        private readonly GamesService _gamesService;
        private readonly ulong _id;
        private readonly IUserMessage _originalMessage;
        private readonly RandomService _randomService;
        private readonly DateTime _startTime;
        private readonly Timer _timer = new Timer(4000);
        private bool _finished;
        private bool _itemHasAppeared;
        private bool _oreHasAppeared;

        public Scavenge(SocketCommandContext context, GamesService gamesService, RandomService randomService,
            ScavengeLocation location, IUserMessage message)
        {
            _collectableItems = new List<Item>();
            _context = context;
            _gamesService = gamesService;
            _randomService = randomService;
            _originalMessage = message;

            _id = context.User.Id;
            Location = location;

            _timer.Elapsed += Timer_Elapsed;

            PopulateCollectableItems();

            _startTime = DateTime.Now;
            _timer.Start();
        }

        public void Finalise()
        {
            if (_finished)
            {
                return;
            }

            _finished = true;
            _gamesService.Scavenges.TryRemove(_id, out _);
        }

        private async Task OnGameEnd()
        {
            var user = MarbleBotUser.Find(_context);
            user.LastScavenge = DateTime.UtcNow;
            foreach (var item in Items)
            {
                if (user.Items.ContainsKey(item.Id))
                {
                    user.Items[item.Id]++;
                }
                else
                {
                    user.Items.Add(item.Id, 1);
                }
            }

            MarbleBotUser.UpdateUser(user);

            await UpdateEmbed(true, user.Stage);

            Finalise();
        }

        private void PopulateCollectableItems()
        {
            var items = Item.GetItems();
            foreach ((int itemId, Item item) in items)
            {
                if (item.ScavengeLocation == Location)
                {
                    var outputItem = item;
                    outputItem = new Item(outputItem, itemId);
                    _collectableItems.Add(outputItem);
                }
            }
        }

        private async void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (_randomService.Rand.Next(0, 5) != 0)
            {
                var item = _collectableItems[_randomService.Rand.Next(0, _collectableItems.Count)];
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

            if ((e.SignalTime - _startTime).TotalSeconds >= 32)
            {
                _timer.Stop();
                await OnGameEnd();
            }
        }

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
                    .WithValue($"{itemOutput}{(gameEnded ? "" : "\nUse `mb/scavenge grab` to add the bolded item to your inventory or use `mb/scavenge sell` to sell it. ")}"));
            }

            if (_oreHasAppeared)
            {
                fields.Add(new EmbedFieldBuilder()
                    .WithName("Ores")
                    .WithValue($"{oreOutput}{(gameEnded ? "" : "\nUse `mb/scavenge drill` to add the bolded ore to your inventory. A drill is required to drill ores.")}"));
            }

            var embed = _originalMessage.Embeds.First();

            if (await _context.Channel.GetMessageAsync(_originalMessage.Id) == null)
            {
                Finalise();
                return;
            }

            await _originalMessage.ModifyAsync(m => m.Embed = new EmbedBuilder
            {
                Color = embed.Color,
                Description = gameEnded
                    ? stage == 1
                        ? "The scavenge session is over! Any remaining items have been added to your inventory!"
                        : "The scavenge session is over! Any remaining non-ore items have been added to your inventory!"
                    : "Scavenge session ongoing.",
                Fields = fields,
                Title = $"Item Scavenge: {Location.ToString().CamelToTitleCase()}"
            }.Build());
        }
    }
}
