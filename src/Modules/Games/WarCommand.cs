using Discord;
using Discord.Commands;
using MarbleBot.Common;
using MarbleBot.Common.Games;
using MarbleBot.Common.Games.War;
using MarbleBot.Modules.Games.Services;
using MarbleBot.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace MarbleBot.Modules.Games
{
    [Group("war")]
    [Summary("Participate in a Marble War battle!")]
    [Remarks("Requires a channel in which slowmode is enabled.")]
    public class WarCommand : GameModule
    {
        private const GameType Type = GameType.War;

        public WarCommand(BotCredentials botCredentials, GamesService gamesService, RandomService randomService) : base(botCredentials, gamesService, randomService)
        {
        }

        [Command("signup")]
        [Alias("join")]
        [Summary("Sign up to the Marble War!")]
        public async Task WarSignupCommand(Weapon weapon, [Remainder] string marbleName = "")
        {
            await Signup(Type, marbleName, 20, async () => { await WarStartCommand(); }, weapon);
        }

        [Command("start")]
        [Alias("commence")]
        [Summary("Start the Marble War!")]
        public async Task WarStartCommand()
        {
            ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
            if (_gamesService.Wars.ContainsKey(fileId))
            {
                await SendErrorAsync($"**{Context.User.Username}**, there is already a war going on!");
                return;
            }

            if (!File.Exists($"Data{Path.DirectorySeparatorChar}{fileId}.war"))
            {
                await SendErrorAsync($"**{Context.User.Username}**, no-one is signed up!");
                return;
            }

            var marbles = new List<WarMarble>();
            using (var marbleList = new StreamReader($"Data{Path.DirectorySeparatorChar}{fileId}.war"))
            {
                if (marbleList.BaseStream.Length == 0)
                {
                    await SendErrorAsync($"**{Context.User.Username}**, no-one is signed up!");
                    return;
                }

                var formatter = new BinaryFormatter();
                var rawMarbles = (List<(ulong id, string name, int itemId)>)formatter.Deserialize(marbleList.BaseStream);
                if (rawMarbles.Count == 0)
                {
                    await SendErrorAsync($"**{Context.User.Username}**, no-one is signed up!");
                    return;
                }

                foreach ((ulong id, string name, int itemId) in rawMarbles.OrderBy(marbleInfo => _randomService.Rand.Next()))
                {
                    var user = MarbleBotUser.Find(Context, id);
                    marbles.Add(new WarMarble(user, name, 35, Item.Find<Weapon>(itemId)));
                }
            }

            var team1 = new List<WarMarble>();
            var team2 = new List<WarMarble>();
            var mentions = new StringBuilder();
            for (int i = 0; i < marbles.Count; i++)
            {
                WarMarble marble = marbles[i];
                if (i < (int)Math.Ceiling(marbles.Count / 2d))
                {
                    team1.Add(marble);
                }
                else
                {
                    team2.Add(marble);
                }

                if (MarbleBotUser.Find(Context, marble.Id).SiegePing && Context.Client.GetUser(marble.Id).Status != UserStatus.Offline)
                {
                    mentions.Append($"<@{marble.Id}> ");
                }
            }

            WarMarble? aiMarble = null;
            if ((team1.Count + team2.Count) % 2 > 0)
            {
                WarMarble[] allMarbles = team1.Union(team2).ToArray();
                if (allMarbles.Average(m => MarbleBotUser.Find(Context, m.Id).Stage) > 1.5)
                {
                    aiMarble = new WarMarble(Context.Client.CurrentUser.Id, "MarbleBot", 40, Item.Find<Weapon>(_randomService.Rand.Next(0, 9) switch
                    {
                        0 => "086",
                        1 => "087",
                        2 => "088",
                        3 => "089",
                        4 => "093",
                        5 => "094",
                        6 => "095",
                        7 => "096",
                        _ => "097"
                    }), Item.Find<Shield>("063"), Item.Find<Spikes>(_randomService.Rand.Next(0, 4) switch
                    {
                        0 => "066",
                        1 => "071",
                        2 => "074",
                        _ => "080"
                    }), Context.Client.CurrentUser.Username, Context.Client.CurrentUser.Discriminator);
                }
                else
                {
                    aiMarble = new WarMarble(Context.Client.CurrentUser.Id, "MarbleBot", 35,
                        Item.Find<Weapon>(_randomService.Rand.Next(0, 2) switch
                        {
                            0 => "094",
                            1 => "095",
                            _ => "096"
                        }), null, null, Context.Client.CurrentUser.Username, Context.Client.CurrentUser.Discriminator);
                }

                team2.Add(aiMarble);
            }

            var war = new War(Context, _gamesService, _randomService, fileId, team1, team2, aiMarble);

            if (mentions.Length != 0)
            {
                await ReplyAsync(mentions.ToString());
            }

            _gamesService.Wars.GetOrAdd(fileId, war);

            await war.Start();
        }

        [Command("stop")]
        [RequireOwner]
        public async Task WarStopCommand()
        {
            _gamesService.Wars[Context.IsPrivate ? Context.User.Id : Context.Guild.Id].Finalise();
            await ReplyAsync("War successfully stopped.");
        }

        [Command("attack", RunMode = RunMode.Async)]
        [Summary("Attacks a member of the opposing team with the equipped weapon.")]
        public async Task WarAttackCommand([Remainder] string target)
        {
            ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;

            if (!_gamesService.Wars.TryGetValue(fileId, out War? war))
            {
                await SendErrorAsync($"**{Context.User.Username}**, there is no currently ongoing war!");
                return;
            }

            if (!war.IsMarbleTurn(Context.User.Id))
            {
                await SendErrorAsync($"**{Context.User.Username}**, it is not your turn!");
                return;
            }

            WarMarble currentMarble = war.GetCurrentMarble();
            if (currentMarble.Rage && (DateTime.UtcNow - currentMarble.LastRage).Seconds > 20)
            {
                currentMarble.DamageMultiplier /= 2;
                currentMarble.Rage = false;
            }

            var user = MarbleBotUser.Find(Context);
            var ammo = new Ammo();
            if (currentMarble.Weapon.Ammo != null && currentMarble.Weapon.Ammo.Length != 0)
            {
                ammo = user.GetAmmo(currentMarble.Weapon);

                if (ammo == null)
                {
                    await SendErrorAsync($"{Context.User.Username}, you do not have enough ammo to use the weapon {currentMarble.Weapon.Name}!");
                    return;
                }

                ammo = Item.Find<Ammo>(ammo.Id.ToString("000"));
                user.Items[ammo.Id] -= currentMarble.Weapon.Hits;
                user.NetWorth -= ammo.Price * currentMarble.Weapon.Hits;
                MarbleBotUser.UpdateUser(user);
            }

            WarTeam enemyTeam = currentMarble.Team!.IsLeftTeam ? war.RightTeam : war.LeftTeam;
            WarMarble? enemyMarble = null;
            if (int.TryParse(target, out int index) && enemyTeam.Marbles.Count >= index)
            {
                enemyMarble = enemyTeam.Marbles.ElementAt(index - 1);
            }
            else
            {
                foreach (WarMarble enemy in enemyTeam.Marbles)
                {
                    if (string.Compare(enemy.Name, target, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        enemyMarble = enemy;
                        break;
                    }
                }

                if (enemyMarble == null)
                {
                    await ReplyAsync($"**{currentMarble.Name}**, could not find the enemy!");
                    return;
                }
            }

            if (enemyMarble.Health == 0)
            {
                await SendErrorAsync($"**{Context.User.Username}**, you cannot attack a dead marble!");
                return;
            }

            if (!Context.IsPrivate && Context.Guild.CurrentUser.GuildPermissions.ManageMessages)
            {
                await Context.Message.DeleteAsync();
            }

            int baseDamage = currentMarble.Weapon.Damage + (currentMarble.Weapon.WeaponClass == WeaponClass.Ranged ? ammo.Damage : 0);
            await war.Attack(currentMarble, enemyMarble, true, baseDamage);
        }

        [Command("bash", RunMode = RunMode.Async)]
        [Alias("bonk", "charge")]
        [Summary("Attacks a member of the opposing team without a weapon.")]
        public async Task WarBashCommand([Remainder] string target)
        {
            ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;

            if (!_gamesService.Wars.TryGetValue(fileId, out War? war))
            {
                await SendErrorAsync($"**{Context.User.Username}**, there is no currently ongoing war!");
                return;
            }

            if (!war.IsMarbleTurn(Context.User.Id))
            {
                await SendErrorAsync($"**{Context.User.Username}**, it is not your turn!");
                return;
            }

            WarMarble currentMarble = war.GetCurrentMarble();
            if (currentMarble.Rage && (DateTime.UtcNow - currentMarble.LastRage).Seconds > 20)
            {
                currentMarble.DamageMultiplier /= 2;
                currentMarble.Rage = false;
            }

            WarTeam enemyTeam = currentMarble.Team!.IsLeftTeam ? war.RightTeam : war.LeftTeam;
            WarMarble? enemyMarble;
            if (int.TryParse(target, out int index) && enemyTeam.Marbles.Count >= index)
            {
                enemyMarble = enemyTeam.Marbles.ElementAt(index - 1);
            }
            else
            {
                foreach (WarMarble enemy in enemyTeam.Marbles)
                {
                    if (string.Compare(enemy.Name, target, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        enemyMarble = enemy;
                        break;
                    }
                }

                await SendErrorAsync($"**{currentMarble.Name}**, could not find the enemy!");
                return;
            }

            if (enemyMarble.Health == 0)
            {
                await SendErrorAsync($"**{Context.User.Username}**, you cannot attack a dead marble!");
                return;
            }

            if (!Context.IsPrivate && Context.Guild.CurrentUser.GuildPermissions.ManageMessages)
            {
                await Context.Message.DeleteAsync();
            }

            const int bashBaseDamage = 3;
            await war.Attack(currentMarble, enemyMarble, false, bashBaseDamage);
        }

        [Command("checkearn")]
        [Summary("Shows whether you can earn money from wars and if not, when.")]
        public async Task WarCheckearnCommand()
        {
            await Checkearn(Type);
        }

        [Command("clear")]
        [Summary("Clears the list of contestants.")]
        public async Task WarClearCommand()
        {
            await Clear(Type);
        }

        [Command("contestants")]
        [Alias("marbles", "participants")]
        [Summary("Shows a list of all the contestants in the war.")]
        public async Task WarContestantsCommand()
        {
            await ShowContestants(Type);
        }

        [Command("leaderboard")]
        [Alias("leaderboard mostused")]
        [Summary("Shows a leaderboard of most used marbles in wars.")]
        public async Task WarLeaderboardCommand(int page)
        {
            var winners = new Dictionary<string, int>();
            using (var mostUsedMarbleFile = new StreamReader($"Data{Path.DirectorySeparatorChar}WarMostUsed.txt"))
            {
                while (!mostUsedMarbleFile.EndOfStream)
                {
                    string racerInfo = (await mostUsedMarbleFile.ReadLineAsync())!;
                    if (winners.ContainsKey(racerInfo))
                    {
                        winners[racerInfo]++;
                    }
                    else
                    {
                        winners.Add(racerInfo, 1);
                    }
                }
            }

            var winList = new List<(string elementName, int value)>();
            foreach ((string marbleName, int wins) in winners)
            {
                winList.Add((marbleName, wins));
            }

            winList = (from winner in winList
                       orderby winner.value descending
                       select winner).ToList();

            EmbedBuilder? builder = new EmbedBuilder()
                .WithColor(GetColor(Context))
                .WithTitle("War Leaderboard: Most Used");

            await SendLargeEmbedDescriptionAsync(builder, Leaderboard(winList, page));
        }

        [Command("ping")]
        [Summary("Toggles whether you are pinged when a war that you are in starts.")]
        public async Task WarPingCommand(string option = "")
        {
            var user = MarbleBotUser.Find(Context);
            switch (option)
            {
                case "enable":
                case "true":
                case "on":
                    user.WarPing = true;
                    break;
                case "disable":
                case "false":
                case "off":
                    user.WarPing = false;
                    break;
                default:
                    user.WarPing = !user.WarPing;
                    break;
            }

            MarbleBotUser.UpdateUser(user);
            if (user.WarPing)
            {
                await ReplyAsync($"**{Context.User.Username}**, you will now be pinged when a war that you are in starts.\n(type `mb/war ping` to turn off)");
            }
            else
            {
                await ReplyAsync($"**{Context.User.Username}**, you will no longer be pinged when a war that you are in starts.\n(type `mb/war ping` to turn on)");
            }
        }

        [Command("remove")]
        [Summary("Removes a contestant from the contestant list.")]
        public async Task WarRemoveCommand([Remainder] string marbleToRemove)
        {
            await RemoveContestant(Type, marbleToRemove);
        }

        [Command("setemoji")]
        [Summary("Sets the emoji used to represent the user.")]
        public async Task WarSetEmojiCommand(string rawEmoji)
        {
            const string largeGreenSquare = "\uD83D\uDFE9";
            const string middleFinger = "\uD83D\uDD95";
            const string rock = "\uD83E\uDEA8";
            if (rawEmoji.Length == 2 && rawEmoji != largeGreenSquare && rawEmoji != middleFinger && rawEmoji != rock)
            {
                var user = MarbleBotUser.Find(Context);
                user.WarEmoji = rawEmoji;
                MarbleBotUser.UpdateUser(user);
                await ReplyAsync($"Successfully updated emoji to {new Emoji(rawEmoji)}.");
            }
            else
            {
                await SendErrorAsync("You must enter a valid emoji! Custom emotes are not supported.");
            }
        }

        [Command("valid")]
        [Alias("validweapons")]
        [Summary("Shows all valid weapons to use in war battles.")]
        public async Task WarValidWeaponsCommand()
        {
            IDictionary<int, Item> items = Item.GetItems();
            var output = new StringBuilder();
            foreach ((_, Item item) in items)
            {
                if (item is Weapon weapon)
                {
                    if (weapon.WeaponClass != 0 && weapon.WeaponClass != WeaponClass.Artillery &&
                        weapon.Stage <= MarbleBotUser.Find(Context).Stage)
                    {
                        output.AppendLine($"{weapon} ({weapon.WeaponClass})");
                    }
                }
            }

            await ReplyAsync(embed: new EmbedBuilder()
                .WithColor(GetColor(Context))
                .WithDescription(output.ToString())
                .WithTitle("Marble War: Valid Weapons")
                .Build());
        }

        [Command("help")]
        [Alias("")]
        [Priority(-1)]
        [Summary("War help.")]
        public async Task WarHelpCommand([Remainder] string _ = "")
        {
            await ReplyAsync(embed: new EmbedBuilder()
                .AddField("How to play", new StringBuilder()
                    .AppendLine("Use `mb/war signup <weapon ID> <marble name>` to sign up as a marble!")
                    .AppendLine("When everyone's done, use `mb/war start`! The war begins automatically if 20 marbles have signed up.")
                    .Append("\nA grid of emojis, similar to the one above, will be displayed. The grid will include green squares and rocks.")
                    .AppendLine(" Each player is represented by an emoji. You can use `mb/war setemoji <emoji>` to change yours.")
                    .AppendLine("\nEveryone is split into two teams. If there is an odd number of contestants, an AI marble joins the team that has fewer members.")
                    .AppendLine("\nWhen it's your turn, you can move towards the enemies by reacting with an arrow emoji. You can only move once per turn and you can only move into green squares.")
                    .Append("\nIf you're close enough to an enemy, you can use `mb/war attack <marble ID>` to attack with your weapon or `mb/war bash <marble ID>` to attack without.")
                    .Append(" If you can't attack, react with :negative_squared_cross_mark: to end your turn.")
                    .ToString())
                .AddField("Valid weapons", "Use `mb/war valid` to check which items you can use as weapons. If you don't have any, you'll need to craft one.")
                .AddField("Boost", "Each team is given a boost at the beginning. If enough people on a team react with :star2:, the boost will activate!")
                .WithColor(GetColor(Context))
                .WithDescription(@":green_square::green_square::green_square::green_square::green_square::green_square:
:green_square::green_square::green_square::green_square::rock::green_square:
:red_circle::green_square::green_square::green_square::green_square::blue_circle:
:green_square::green_square::green_square::rock::green_square::green_square:
:green_square::green_square::green_square::green_square::green_square::green_square:")
                .WithTitle("Marble War! :crossed_swords:")
                .Build());
        }
    }
}
