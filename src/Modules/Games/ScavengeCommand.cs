using Discord;
using Discord.Commands;
using MarbleBot.Common;
using MarbleBot.Common.Games;
using MarbleBot.Common.Games.Scavenge;
using MarbleBot.Extensions;
using MarbleBot.Modules.Games.Services;
using MarbleBot.Services;
using System;
using System.Text;
using System.Threading.Tasks;

namespace MarbleBot.Modules.Games
{
    [Group("scavenge")]
    [Summary("Scavenge for items!")]
    public class ScavengeCommand : GameModule
    {
        private const GameType Type = GameType.Race;

        public ScavengeCommand(BotCredentials botCredentials, GamesService gamesService, RandomService randomService) :
            base(botCredentials, gamesService, randomService)
        {
        }

        public async Task ScavengeStartAsync(MarbleBotUser user, ScavengeLocation location)
        {
            if ((DateTime.UtcNow - user.LastScavenge).TotalHours < 6)
            {
                DateTime sixHoursAgo = DateTime.UtcNow.AddHours(-6);
                await SendErrorAsync($"**{Context.User.Username}**, you need to wait for **{GetTimeSpanSentence(user.LastScavenge - sixHoursAgo)}** until you can scavenge again.");
            }
            else
            {
                if (_gamesService.Scavenges.ContainsKey(Context.User.Id))
                {
                    await SendErrorAsync($"**{Context.User.Username}**, you are already scavenging!");
                }
                else
                {
                    var scavengeMessage = await ReplyAsync(embed: new EmbedBuilder()
                        .WithColor(GetColor(Context))
                        .WithDescription($"**{Context.User.Username}** has begun scavenging in **{location.ToString().CamelToTitleCase()}**!")
                        .WithTitle("Item Scavenge Begin!").Build());
                    _gamesService.Scavenges.GetOrAdd(Context.User.Id, new Scavenge(Context, _gamesService, _randomService, location, scavengeMessage));
                }
            }
        }

        [Command("canarybeach")]
        [Alias("canary beach")]
        [Summary("Starts a scavenge session in Canary Beach.")]
        public async Task ScavengeCanaryCommand()
        {
            await ScavengeStartAsync(MarbleBotUser.Find(Context), ScavengeLocation.CanaryBeach);
        }

        [Command("destroyersremains")]
        [Alias("destroyer'sremains", "destroyer's remains")]
        [Remarks("Stage2")]
        [Summary("Starts a scavenge session in Destroyer's Remains.")]
        public async Task ScavengeDestroyerCommand()
        {
            if (MarbleBotUser.Find(Context).Stage < 2)
            {
                await ReplyAsync(embed: new EmbedBuilder()
                    .WithColor(GetColor(Context))
                    .WithDescription($"{StageTooHighString()}\n\nYou cannot scavenge in this location!")
                    .WithTitle("Scavenge: Destroyer's Remains")
                    .Build());
            }
            else
            {
                await ScavengeStartAsync(MarbleBotUser.Find(Context), ScavengeLocation.DestroyersRemains);
            }
        }

        [Command("treewurld")]
        [Alias("tree wurld")]
        [Summary("Starts a scavenge session in Tree Wurld.")]
        public async Task ScavengeTreeCommand()
        {
            await ScavengeStartAsync(MarbleBotUser.Find(Context), ScavengeLocation.TreeWurld);
        }

        [Command("violetvolcanoes")]
        [Alias("violet volcanoes")]
        [Remarks("Stage2")]
        [Summary("Starts a scavenge session in the Violet Volcanoes.")]
        public async Task ScavengeVolcanoCommand()
        {
            if (MarbleBotUser.Find(Context).Stage < 2)
            {
                await ReplyAsync(embed: new EmbedBuilder()
                    .WithColor(GetColor(Context))
                    .WithDescription($"{StageTooHighString()}\n\nYou cannot scavenge in this location!")
                    .WithTitle("Scavenge: Violet Volcanoes")
                    .Build());
            }
            else
            {
                await ScavengeStartAsync(MarbleBotUser.Find(Context), ScavengeLocation.VioletVolcanoes);
            }
        }

        [Command("grab")]
        [Alias("take")]
        [Summary("Grabs an item found in a scavenge session.")]
        public async Task ScavengeGrabCommand()
        {
            var user = MarbleBotUser.Find(Context);

            if (!_gamesService.Scavenges.ContainsKey(Context.User.Id) ||
                _gamesService.Scavenges[Context.User.Id] == null)
            {
                await SendErrorAsync($"**{Context.User.Username}**, you are not scavenging!");
                return;
            }

            if (_gamesService.Scavenges[Context.User.Id].Items.Count == 0)
            {
                await SendErrorAsync($"**{Context.User.Username}**, there is no item to grab!");
                return;
            }

            var item = _gamesService.Scavenges[Context.User.Id].Items.Dequeue();
            _gamesService.Scavenges[Context.User.Id].UsedItems.Enqueue(item);
            if (user.Items.ContainsKey(item.Id))
            {
                user.Items[item.Id]++;
            }
            else
            {
                user.Items.Add(item.Id, 1);
            }

            user.NetWorth += item.Price;
            MarbleBotUser.UpdateUser(user);
            await _gamesService.Scavenges[Context.User.Id].UpdateEmbed();
            var confirmationMessage = await ReplyAsync($"**{Context.User.Username}**, you have successfully added **{item.Name}** x**1** to your inventory!");

            // Clean up the messages created if the bot can delete messages
            if (!Context.IsPrivate && Context.Guild.CurrentUser.GuildPermissions.ManageMessages)
            {
                await Task.Delay(4000);
                await Context.Message.DeleteAsync();
                await confirmationMessage.DeleteAsync();
            }
        }

        [Command("drill")]
        [Alias("extract", "mine")]
        [Remarks("Stage2")]
        [Summary("Extracts an ore found in a scavenge session.")]
        public async Task ScavengeDrillCommand()
        {
            if (!_gamesService.Scavenges.ContainsKey(Context.User.Id) ||
                _gamesService.Scavenges[Context.User.Id] == null)
            {
                await SendErrorAsync($"**{Context.User.Username}**, you are not scavenging!");
                return;
            }

            if (_gamesService.Scavenges[Context.User.Id].Ores.Count == 0)
            {
                await SendErrorAsync($"**{Context.User.Username}**, there is nothing to drill!");
                return;
            }

            var user = MarbleBotUser.Find(Context);
            if (!(user.Items.ContainsKey(81) || user.Items.ContainsKey(82)))
            {
                await SendErrorAsync($"**{Context.User.Username}**, you need a drill to mine ore!");
                return;
            }

            var item = _gamesService.Scavenges[Context.User.Id].Ores.Dequeue();
            _gamesService.Scavenges[Context.User.Id].UsedOres.Enqueue(item);
            if (user.Items.ContainsKey(item.Id))
            {
                user.Items[item.Id]++;
            }
            else
            {
                user.Items.Add(item.Id, 1);
            }

            user.NetWorth += item.Price;
            MarbleBotUser.UpdateUser(user);
            await _gamesService.Scavenges[Context.User.Id].UpdateEmbed();
            var confirmationMessage =
                await ReplyAsync($"**{Context.User.Username}**, you have successfully added **{item.Name}** x**1** to your inventory!");

            // Clean up the messages created if the bot can delete messages
            if (!Context.IsPrivate && Context.Guild.CurrentUser.GuildPermissions.ManageMessages)
            {
                await Task.Delay(4000);
                await Context.Message.DeleteAsync();
                await confirmationMessage.DeleteAsync();
            }
        }

        [Command("sell")]
        [Summary("Sells an item found in a scavenge session.")]
        public async Task ScavengeSellCommand()
        {
            var user = MarbleBotUser.Find(Context);

            if (!_gamesService.Scavenges.ContainsKey(Context.User.Id) ||
                _gamesService.Scavenges[Context.User.Id] == null)
            {
                await SendErrorAsync($"**{Context.User.Username}**, you are not scavenging!");
                return;
            }

            if (_gamesService.Scavenges[Context.User.Id].Items.Count == 0)
            {
                await SendErrorAsync($"**{Context.User.Username}**, there is nothing to sell!");
                return;
            }

            var item = _gamesService.Scavenges[Context.User.Id].Items.Dequeue();
            _gamesService.Scavenges[Context.User.Id].UsedItems.Enqueue(item);
            user.Balance += item.Price;
            user.NetWorth += item.Price;
            MarbleBotUser.UpdateUser(user);
            await _gamesService.Scavenges[Context.User.Id].UpdateEmbed();
            var confirmationMessage = await ReplyAsync($"**{Context.User.Username}**, you have successfully sold **{item.Name}** x**1** for {UnitOfMoney}**{item.Price:n2}**!");

            // Clean up the messages created if the bot can delete messages
            if (!Context.IsPrivate && Context.Guild.CurrentUser.GuildPermissions.ManageMessages)
            {
                await Task.Delay(4000);
                await Context.Message.DeleteAsync();
                await confirmationMessage.DeleteAsync();
            }
        }

        [Command("locations")]
        [Summary("Shows scavenge locations.")]
        public async Task ScavengeLocationCommand()
        {
            var stageTwoLocations = MarbleBotUser.Find(Context).Stage > 1
                ? "Destroyer's Remains\nViolet Volcanoes"
                : ":lock: LOCKED\n:lock: LOCKED";
            await ReplyAsync(embed: new EmbedBuilder()
                .WithColor(GetColor(Context))
                .WithDescription($"Canary Beach\nTree Wurld\n{stageTwoLocations}\n\nUse `mb/scavenge info <location name>` to see which items you can get!")
                .WithTitle("Scavenge Locations")
                .Build());
        }

        [Command("next")]
        [Alias("checkearn")]
        [Summary("Shows whether you can scavenge and if not, when you will be able to.")]
        public async Task ScavengeCheckearnAsync()
        {
            await Checkearn(Type);
        }

        public async Task ScavengeInfoAsync(ScavengeLocation location)
        {
            var output = new StringBuilder();
            var itemsDict = Item.GetItems();
            foreach ((int itemId, Item item) in itemsDict)
            {
                if (item.ScavengeLocation == location)
                {
                    output.AppendLine($"`[{itemId:000}]` {item.Name}");
                }
            }

            await ReplyAsync(embed: new EmbedBuilder()
                .WithColor(GetColor(Context))
                .WithDescription(output.ToString())
                .WithTitle($"Scavenge Location Info: {location}")
                .Build());
        }

        [Command("location canarybeach")]
        [Alias("location canary", "location canary beach", "info canarybeach", "info canary", "info canary beach")]
        [Summary("Shows scavenge location info for Canary Beach.")]
        public async Task ScavengeLocationCanaryCommand()
        {
            await ScavengeInfoAsync(ScavengeLocation.CanaryBeach);
        }

        [Command("location destroyersremains")]
        [Alias("location destroyer'sremains", "location destroyer's remains", "info destroyersremains",
            "info destroyer'sremains", "info destroyer's remains")]
        [Remarks("Stage2")]
        [Summary("Shows scavenge location info for Destroyer's Remains")]
        public async Task ScavengeLocationDestroyerCommand()
        {
            if (MarbleBotUser.Find(Context).Stage < 2)
            {
                await ReplyAsync(embed: new EmbedBuilder()
                    .WithColor(GetColor(Context))
                    .WithDescription($"{StageTooHighString()}\n\nYou cannot scavenge in this location!")
                    .WithTitle("Scavenge Location Info: Destroyer's Remains")
                    .Build());
            }
            else
            {
                await ScavengeInfoAsync(ScavengeLocation.DestroyersRemains);
            }
        }

        [Command("location treewurld")]
        [Alias("location tree", "location tree wurld", "info treewurld", "info tree", "info tree wurld")]
        [Summary("Shows scavenge location info for Tree Wurld")]
        public async Task ScavengeLocationTreeCommand()
        {
            await ScavengeInfoAsync(ScavengeLocation.TreeWurld);
        }

        [Command("location violetvolcanoes")]
        [Alias("location violet volcanoes", "info violetvolcanoes", "info violet volcanoes")]
        [Remarks("Stage2")]
        [Summary("Starts a scavenge session in Destroyer's Remains")]
        public async Task ScavengeLocationVolcanoCommand()
        {
            if (MarbleBotUser.Find(Context).Stage < 2)
            {
                await ReplyAsync(embed: new EmbedBuilder()
                    .WithColor(GetColor(Context))
                    .WithDescription($"{StageTooHighString()}\n\nYou cannot scavenge in this location!")
                    .WithTitle("Scavenge Location Info: Violet Volcanoes")
                    .Build());
            }
            else
            {
                await ScavengeInfoAsync(ScavengeLocation.VioletVolcanoes);
            }
        }

        [Command("help")]
        [Alias("")]
        [Priority(-1)]
        [Summary("Scavenge help.")]
        public async Task ScavengeHelpCommand([Remainder] string _ = "")
        {
            bool userCanDrill = MarbleBotUser.Find(Context).Stage > 1;
            const string helpP1 =
                "Use `mb/scavenge locations` to see where you can scavenge for items and use `mb/scavenge <location name>` to start a scavenge session!";
            const string helpP2 =
                "\n\nWhen you find an item, use `mb/scavenge sell` to sell immediately or `mb/scavenge grab` to put the item in your inventory!";
            const string helpP3 =
                "\n\nScavenge games last for 60 seconds - every 8 seconds there will be a 80% chance that you've found an item.";
            const string helpP4 =
                "\n\nIf you find an ore, you can use `mb/scavenge drill` to drill it. Drilling requires a drill in your inventory.";
            string helpP5 =
                $"\n\nAt the end of the scavenge, all {(userCanDrill ? "non-ore" : "")} items are automatically added to your inventory.";
            await ReplyAsync(embed: new EmbedBuilder()
                .AddField("How to play", $"{helpP1}{helpP2}{helpP3}{(userCanDrill ? helpP4 : "")}{helpP5}")
                .WithColor(GetColor(Context))
                .WithTitle("Item Scavenge!")
                .Build());
        }
    }
}
