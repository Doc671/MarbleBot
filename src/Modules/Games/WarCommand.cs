using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MarbleBot.Common;
using MarbleBot.Common.Games;
using MarbleBot.Common.Games.War;
using MarbleBot.Extensions;
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
            var marbles = new List<WarMarble>();

            if (!File.Exists($"Data{Path.DirectorySeparatorChar}{fileId}.war"))
            {
                await SendErrorAsync($"**{Context.User.Username}**, no-one is signed up!");
                return;
            }

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
                    MarbleBotUser user = MarbleBotUser.Find(Context, id);
                    marbles.Add(new WarMarble(id, name, 40, Item.Find<Weapon>(itemId),
                        user.GetShield(), user.GetSpikes()));
                }
            }

            var team1 = new List<WarMarble>();
            var team2 = new List<WarMarble>();
            var team1Output = new StringBuilder();
            var team2Output = new StringBuilder();
            var mentions = new StringBuilder();
            SocketUser? currentUser = null;
            for (int i = 0; i < marbles.Count; i++)
            {
                WarMarble marble = marbles[i];
                if (i < (int)Math.Ceiling(marbles.Count / 2d))
                {
                    team1.Add(marble);
                    currentUser = Context.Client.GetUser(marble.Id);
                    marble.Team = 1;
                    team1Output.AppendLine($"`[{team1.Count}]` **{marble.Name}** [{currentUser.Username}#{currentUser.Discriminator}]");
                    if (MarbleBotUser.Find(Context, marble.Id).SiegePing)
                    {
                        mentions.Append($"<@{marble.Id}> ");
                    }
                }
                else
                {
                    team2.Add(marble);
                    currentUser = Context.Client.GetUser(marble.Id);
                    marble.Team = 2;
                    team2Output.AppendLine($"`[{team2.Count}]` **{marble.Name}** [{currentUser.Username}#{currentUser.Discriminator}]");
                    if (MarbleBotUser.Find(Context, marble.Id).SiegePing)
                    {
                        mentions.Append($"<@{marble.Id}> ");
                    }
                }
            }

            WarMarble? aiMarble = null;
            if ((team1.Count + team2.Count) % 2 > 0)
            {
                IEnumerable<WarMarble> allMarbles = team1.Union(team2);
                if (Math.Abs(MathF.Round(allMarbles.Sum(m => MarbleBotUser.Find(Context, m.Id).Stage) /
                                         (float)allMarbles.Count()) - 2) < float.Epsilon)
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
                    }));
                }
                else
                {
                    aiMarble = new WarMarble(Context.Client.CurrentUser.Id, "MarbleBot", 35,
                        Item.Find<Weapon>(_randomService.Rand.Next(0, 2) switch
                        {
                            0 => "094",
                            1 => "095",
                            _ => "096"
                        }), null, null);
                }

                aiMarble.Team = 2;
                team2.Add(aiMarble);
                team2Output.AppendLine($"`[{team2.Count}]` **MarbleBot** [MarbleBot#7194]");
            }

            var team1Boost = (WarBoost)_randomService.Rand.Next(1, 4);
            var team2Boost = (WarBoost)_randomService.Rand.Next(1, 4);

            var war = new War(Context, _gamesService, _randomService, fileId, team1, team2, aiMarble, team1Boost, team2Boost);
            await ReplyAsync(embed: new EmbedBuilder()
                .WithColor(GetColor(Context))
                .WithDescription("Use `mb/war attack <marble code>` to attack with your weapon and `mb/war bash <marble code>` to attack without.")
                .WithTitle("Let the battle commence! :crossed_swords:")
                .AddField($"Team {war.Team1.Name}",
                    $"Boost: **{team1Boost.ToString().CamelToTitleCase()}**\n{team1Output}")
                .AddField($"Team {war.Team2.Name}",
                    $"Boost: **{team2Boost.ToString().CamelToTitleCase()}**\n{team2Output}")
                .Build());

            if (mentions.Length != 0)
            {
                await ReplyAsync(mentions.ToString());
            }

            _gamesService.Wars.GetOrAdd(fileId, war);

            war.Start();
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

            WarMarble currentMarble = war!.AllMarbles.FirstOrDefault(m => m.Id == Context.User.Id);
            if (currentMarble == null)
            {
                await SendErrorAsync($"**{Context.User.Username}**, you are not in this battle!");
                return;
            }

            if (currentMarble.Health < 1)
            {
                await SendErrorAsync($"**{Context.User.Username}**, you are out and can no longer attack!");
                return;
            }

            double totalSeconds = (DateTime.UtcNow - currentMarble.LastMoveUsed).TotalSeconds;
            if (totalSeconds < 5)
            {
                await SendErrorAsync($"**{Context.User.Username}**, you must wait for {GetDateString(5 - totalSeconds)} until you can attack again!");
                return;
            }

            if (currentMarble.Rage && (DateTime.UtcNow - currentMarble.LastRage).Seconds > 20)
            {
                currentMarble.DamageMultiplier /= 2;
                currentMarble.Rage = false;
            }

            MarbleBotUser user = MarbleBotUser.Find(Context);
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

            WarTeam enemyTeam = currentMarble.Team == 1 ? war.Team2 : war.Team1;
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

            if (enemyMarble.Health < 0)
            {
                await SendErrorAsync($"**{Context.User.Username}**, you cannot attack a dead marble!");
                return;
            }

            var builder = new EmbedBuilder()
                .WithColor(GetColor(Context))
                .WithTitle($"**{currentMarble.Name}** attacks! :boom:");
            currentMarble.LastMoveUsed = DateTime.UtcNow;
            if (currentMarble.Weapon.Hits == 1)
            {
                if (_randomService.Rand.Next(0, 100) < currentMarble.Weapon.Accuracy)
                {
                    int baseDamage = currentMarble.Weapon.Damage + (currentMarble.WeaponClass == WeaponClass.Ranged ? ammo.Damage : 0);
                    int damage = war.DealDamage(currentMarble, enemyMarble, baseDamage);
                    await ReplyAsync(embed: builder
                        .AddField("Remaining Health", $"**{enemyMarble.Health}**/{enemyMarble.MaxHealth}")
                        .WithDescription($"**{currentMarble.Name}** dealt **{damage}** damage to **{enemyMarble.Name}** with **{currentMarble.Weapon.Name}**!")
                        .Build());
                }
                else
                {
                    await ReplyAsync(embed: builder
                        .WithDescription($"**{currentMarble.Name}** tried to attack **{enemyMarble.Name}** but missed!")
                        .Build());
                }
            }
            else
            {
                int totalDamage = 0;
                for (int i = 0; i < currentMarble.Weapon.Hits; i++)
                {
                    if (_randomService.Rand.Next(0, 100) < currentMarble.Weapon.Accuracy)
                    {
                        int baseDamage = currentMarble.Weapon.Damage + (currentMarble.WeaponClass == WeaponClass.Ranged ? ammo.Damage : 0);
                        int damage = war.DealDamage(currentMarble, enemyMarble, baseDamage);
                        totalDamage += damage;
                        builder.AddField($"Attack {i}", $"**{damage}** damage to **{enemyMarble.Name}**.");
                    }
                    else
                    {
                        builder.AddField($"Attack {i}", "Missed!");
                    }
                }

                await ReplyAsync(embed: builder
                    .WithDescription($"**{currentMarble.Name}** dealt a total of **{totalDamage}** to **{enemyMarble.Name}** with **{currentMarble.Weapon.Name}**!")
                    .Build());
            }

            if (war.Team1.Marbles.Sum(m => m.Health) < 1 || war.Team2.Marbles.Sum(m => m.Health) < 1)
            {
                await war.OnGameEnd(Context);
            }
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

            WarMarble currentMarble = war!.AllMarbles.FirstOrDefault(marble => marble.Id == Context.User.Id);
            if (currentMarble == null)
            {
                await SendErrorAsync($"**{Context.User.Username}**, you are not in this battle!");
                return;
            }

            if (currentMarble.Health < 1)
            {
                await SendErrorAsync($"**{Context.User.Username}**, you are out and can no longer attack!");
                return;
            }

            double totalSeconds = (DateTime.UtcNow - currentMarble.LastMoveUsed).TotalSeconds;
            if (totalSeconds < 5)
            {
                await SendErrorAsync($"**{Context.User.Username}**, you must wait for {GetDateString(5 - totalSeconds)} until you can attack again!");
                return;
            }

            if (currentMarble.Rage && (DateTime.UtcNow - currentMarble.LastRage).Seconds > 20)
            {
                currentMarble.DamageMultiplier /= 2;
                currentMarble.Rage = false;
            }

            WarTeam enemyTeam = currentMarble.Team == 1 ? war.Team2 : war.Team1;
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

                await SendErrorAsync($"**{currentMarble.Name}**, could not find the enemy!");
                return;
            }

            if (enemyMarble.Health < 0)
            {
                await SendErrorAsync($"**{Context.User.Username}**, you cannot attack a dead marble!");
                return;
            }

            currentMarble.LastMoveUsed = DateTime.UtcNow;
            const int bashBaseDamage = 3;
            int damage = war.DealDamage(currentMarble, enemyMarble, bashBaseDamage);
            await ReplyAsync(embed: new EmbedBuilder()
                .AddField("Remaining Health", $"**{enemyMarble.Health}**/{enemyMarble.MaxHealth}")
                .WithColor(GetColor(Context))
                .WithDescription($"**{currentMarble.Name}** dealt **{damage}** damage to **{enemyMarble.Name}**!")
                .WithTitle($"**{currentMarble.Name}** attacks! :boom:")
                .Build());

            if (war.Team1.Marbles.Sum(m => m.Health) == 0 || war.Team2.Marbles.Sum(m => m.Health) == 0)
            {
                await war.OnGameEnd(Context);
            }
        }

        [Command("boost", RunMode = RunMode.Async)]
        [Alias("useboost")]
        [Summary("Activates the team's boost if enough team members have boosted.")]
        public async Task WarBoostCommand()
        {
            ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;

            if (!_gamesService.Wars.TryGetValue(fileId, out War? war))
            {
                await SendErrorAsync($"**{Context.User.Username}**, there is no currently ongoing war!");
                return;
            }

            WarMarble currentMarble = war!.AllMarbles.FirstOrDefault(m => m.Id == Context.User.Id);
            if (currentMarble == null)
            {
                await SendErrorAsync($"**{Context.User.Username}**, you are not in this battle!");
                return;
            }

            if (currentMarble.Health < 1)
            {
                await SendErrorAsync($"**{Context.User.Username}**, you are out and can no longer attack!");
                return;
            }

            WarTeam currentTeam = currentMarble.Team == 1 ? war.Team1 : war.Team2;
            if (currentTeam.BoostUsed)
            {
                await SendErrorAsync($"**{Context.User.Username}**, your team's boost has already been used!");
                return;
            }

            currentMarble.Boosted = true;
            WarTeam enemyTeam = currentMarble.Team == 1 ? war.Team2 : war.Team1;

            var builder = new EmbedBuilder()
                .WithAuthor(Context.User)
                .WithColor(GetColor(Context))
                .WithDescription($"**{Context.User.Username}** has attempted to use Team {currentTeam.Name}'s boost!");

            // Activate boost if enough team members (half rounded up) have chosen to boost
            int boosters = currentTeam.Marbles.Aggregate(0, (total, marble) => marble.Boosted ? total + 1 : total);
            int boostsRequired = (int)MathF.Ceiling(currentTeam.Marbles.Count / 2f);
            var output = new StringBuilder();
            if (boosters >= boostsRequired)
            {
                currentTeam.BoostUsed = true;
                switch (currentTeam.Boost)
                {
                    case WarBoost.HealKit:
                    {
                        IEnumerable<WarMarble> teammatesToHeal = currentTeam.Marbles.OrderBy(m => Guid.NewGuid()).Take(boostsRequired);
                        foreach (WarMarble teammate in teammatesToHeal)
                        {
                            if (teammate.Health > 0)
                            {
                                teammate.Health += 8;
                                output.AppendLine($"**{teammate.Name}** recovered **8** Health! (**{teammate.Health}**/{teammate.MaxHealth})");
                            }
                        }

                        break;
                    }
                    case WarBoost.MissileStrike:
                    {
                        foreach (WarMarble enemy in enemyTeam.Marbles)
                        {
                            if (enemy.Health > 0)
                            {
                                enemy.Health -= 5;
                            }
                        }

                        output.Append($"All of Team **{enemyTeam.Name}** took **5** damage!");
                        break;
                    }
                    case WarBoost.Rage:
                    {
                        foreach (WarMarble teammate in currentTeam.Marbles)
                        {
                            teammate.DamageMultiplier *= 2;
                            teammate.LastRage = DateTime.UtcNow;
                            teammate.Rage = true;
                        }

                        output.Append($"Team **{currentTeam.Name}** can deal x2 damage for the next 10 seconds!");
                        break;
                    }
                    case WarBoost.SpikeTrap:
                    {
                        IEnumerable<WarMarble> enemiesToDamage = enemyTeam.Marbles.OrderBy(m => Guid.NewGuid()).Take(boostsRequired);
                        foreach (WarMarble enemy in enemiesToDamage)
                        {
                            if (enemy.Health > 0)
                            {
                                enemy.Health -= 8;
                                output.AppendLine($"**{enemy.Name}** took **8** damage! (**{enemy.Health}**/{enemy.MaxHealth})");
                            }
                        }

                        break;
                    }
                }

                builder.AddField("Boost successful!", output.ToString())
                    .WithTitle($"{currentTeam.Name}: **{currentTeam.Boost.ToString().CamelToTitleCase()}** used!");
            }
            else
            {
                builder.AddField("Boost failed!",
                    $"**{boosters}** out of the required **{boostsRequired}** team members have chosen to use Team {currentTeam.Name}'s **{currentTeam.Boost.ToString().CamelToTitleCase()}**.");
            }

            await ReplyAsync(embed: builder.Build());
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

        [Command("info")]
        [Summary("Shows information about the war.")]
        public async Task WarInfoCommand()
        {
            ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
            var builder = new EmbedBuilder()
                .WithColor(GetColor(Context))
                .WithTitle("War Info");
            if (_gamesService.Wars.ContainsKey(fileId))
            {
                War war = _gamesService.Wars[fileId];

                var team1Output = new StringBuilder();
                foreach (WarMarble marble in war.Team1.Marbles)
                {
                    SocketUser user = Context.Client.GetUser(marble.Id);
                    team1Output.AppendLine($"{marble.Name} (Health: **{marble.Health}**/{marble.MaxHealth}, Wpn: {marble.Weapon}) [{user.Username}#{user.Discriminator}]");
                }

                var team2Output = new StringBuilder();
                foreach (WarMarble marble in war.Team2.Marbles)
                {
                    SocketUser user = Context.Client.GetUser(marble.Id);
                    team2Output.AppendLine($"{marble.Name} (Health: **{marble.Health}**/{marble.MaxHealth}, Wpn: {marble.Weapon}) [{user.Username}#{user.Discriminator}]");
                }

                builder.AddField($"Team {war.Team1.Name}", team1Output.ToString())
                    .AddField($"Team {war.Team2.Name}", team2Output.ToString());
            }
            else
            {
                if (!File.Exists($"Data{Path.DirectorySeparatorChar}{fileId}.war"))
                {
                    await SendErrorAsync($"**{Context.User.Username}**, no-one is signed up!");
                    return;
                }

                var marbleOutput = new StringBuilder();
                using (var marbleListFile = new StreamReader($"Data{Path.DirectorySeparatorChar}{fileId}.war"))
                {
                    if (marbleListFile.BaseStream.Length == 0)
                    {
                        marbleOutput.Append("No-one is signed up!");
                    }
                    else
                    {
                        var formatter = new BinaryFormatter();
                        var marbles = (List<(ulong id, string name, int itemId)>)formatter.Deserialize(marbleListFile.BaseStream);

                        if (marbles.Count == 0)
                        {
                            marbleOutput.Append("No-one is signed up!");
                        }
                        else
                        {
                            foreach ((ulong id, string name, int itemId) in marbles)
                            {
                                SocketUser user = Context.Client.GetUser(id);
                                marbleOutput.AppendLine($"{Bold(name)} (Weapon: **{Item.Find<Item>(itemId.ToString()).Name}**) [{user.Username}#{user.Discriminator}]");
                            }
                        }
                    }
                }

                builder.AddField("Marbles", marbleOutput.ToString());
                builder.WithDescription("War not started yet.");
            }

            await ReplyAsync(embed: builder.Build());
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
                    string? racerInfo = (await mostUsedMarbleFile.ReadLineAsync())!;
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

            var builder = new EmbedBuilder()
                .WithColor(GetColor(Context))
                .WithTitle("War Leaderboard: Most Used");

            await SendLargeEmbedDescriptionAsync(builder, Leaderboard(winList, page));
        }

        [Command("ping")]
        [Summary("Toggles whether you are pinged when a war that you are in starts.")]
        public async Task WarPingCommand(string option = "")
        {
            MarbleBotUser user = MarbleBotUser.Find(Context);
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
                    .Append("\nWhen the war begins, use `mb/war attack <marble code>` to attack an enemy with your weapon")
                    .AppendLine($" and `mb/war bash <marble code>` to attack without.{(MarbleBotUser.Find(Context).Stage > 1 ? "Spikes are twice as effective with `mb/war bash`." : "")}")
                    .Append("\nEveryone is split into two teams. If there is an odd number of contestants, an AI marble joins")
                    .AppendLine(" the team that has fewer members!")
                    .ToString())
                .AddField("Valid weapons", new StringBuilder()
                    .AppendLine("Any item that displays a 'War Class' when you use `mb/item` on it is valid. See `mb/war valid` for more.")
                    .AppendLine("Use `mb/item <item ID>` to see the stats for each item. ")
                    .AppendLine("Ranged weapons require ammo to work.")
                    .Append("Bashing is 100% accurate but only has a base damage of 3.")
                    .ToString())
                .AddField("Boost",
                    "Each team is given a boost at the beginning. If enough people on a team use `mb/war boost`, the boost will activate!")
                .WithColor(GetColor(Context))
                .WithTitle("Marble War!")
                .Build());
        }
    }
}
