﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MarbleBot.Common;
using MarbleBot.Extensions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static MarbleBot.Global;

namespace MarbleBot.Modules.Games
{
    [Group("war")]
    [Summary("Participate in a Marble War battle!")]
    [Remarks("Requires a channel in which slowmode is enabled.")]
    public class WarCommand : GameModule
    {
        private const GameType Type = GameType.War;

        [Command("signup")]
        [Alias("join")]
        [Summary("Sign up to the Marble War!")]
        public async Task WarSignupCommand(string itemId, [Remainder] string marbleName = "")
        => await Signup(Context, Type, marbleName, 20, async () => { await WarStartCommand(); }, itemId);

        [Command("start")]
        [Alias("commence")]
        [Summary("Start the Marble War!")]
        public async Task WarStartCommand()
        {
            ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
            var marbles = new List<WarMarble>();
            bool marblesPresent = false;
            using (var marbleList = new StreamReader($"Data{Path.DirectorySeparatorChar}{fileId}war.csv"))
            {
                while (!marbleList.EndOfStream)
                {
                    marblesPresent = true;
                    var line = (await marbleList.ReadLineAsync()).RemoveChar('\n').Split(',');
                    var userId = ulong.Parse(line[1]);
                    var user = GetUser(Context, userId);
                    marbles.Add(new WarMarble(userId, 40, line[0], GetItem<Weapon>(line[2]),
                        user.Items.ContainsKey(63) && user.Items[63] > 1 ? GetItem<Item>("063") : GetItem<Item>("000"),
                        user.Items.Where(i => GetItem<Item>(i.Key.ToString("000")).Name.Contains("Spikes")).LastOrDefault().Key));
                }
            }

            if (!marblesPresent)
            {
                await SendErrorAsync("It doesn't look like anyone has signed up!");
                return;
            }

            // Shuffles marble list
            for (var i = 0; i < marbles.Count - 1; ++i)
            {
                var r = Rand.Next(i, marbles.Count);
                var temp = marbles[i];
                marbles[i] = marbles[r];
                marbles[r] = temp;
            }

            var team1 = new List<WarMarble>();
            var team2 = new List<WarMarble>();
            var t1Output = new StringBuilder();
            var t2Output = new StringBuilder();
            var pings = new StringBuilder();
            SocketUser currentUser = null;
            for (int i = 0; i < marbles.Count; i++)
            {
                WarMarble marble = marbles[i];
                if (i < (int)Math.Ceiling(marbles.Count / 2d))
                {
                    team1.Add(marble);
                    currentUser = Context.Client.GetUser(marble.Id);
                    marble.Team = 1;
                    t1Output.AppendLine($"`[{team1.Count}]` **{marble.Name}** [{currentUser.Username}#{currentUser.Discriminator}]");
                    if (GetUser(Context, marble.Id).SiegePing) pings.Append($"<@{marble.Id}> ");
                }
                else
                {
                    team2.Add(marble);
                    currentUser = Context.Client.GetUser(marble.Id);
                    marble.Team = 2;
                    t2Output.AppendLine($"`[{team2.Count}]` **{marble.Name}** [{currentUser.Username}#{currentUser.Discriminator}]");
                    if (GetUser(Context, marble.Id).SiegePing) pings.Append($"<@{marble.Id}> ");
                }
            }

            WarMarble aiMarble = null;
            if ((team1.Count + team2.Count) % 2 > 0)
            {
                var allMarbles = team1.Union(team2);
                if (Math.Round(allMarbles.Sum(m => GetUser(Context, m.Id).Stage) / (double)allMarbles.Count()) == 2)
                    aiMarble = new WarMarble(Context.Client.CurrentUser.Id, 40, "MarbleBot", GetItem<Weapon>(Rand.Next(0, 9) switch
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
                    }), GetItem<Item>("063"), Rand.Next(0, 4) switch
                    {
                        0 => 66u,
                        1 => 71u,
                        2 => 74u,
                        _ => 80u
                    });
                else aiMarble = new WarMarble(Context.Client.CurrentUser.Id, 35, "MarbleBot",
                    GetItem<Weapon>(Rand.Next(0, 2) switch
                    {
                        0 => "094",
                        1 => "095",
                        _ => "096"
                    }), GetItem<Item>("000"));
                aiMarble.Team = 2;
                team2.Add(aiMarble);
                t2Output.AppendLine($"`[{team2.Count}]` **MarbleBot** [MarbleBot#7194]");
            }

            var team1Boost = (WarBoost)Rand.Next(1, 4);
            var team2Boost = (WarBoost)Rand.Next(1, 4);

            var war = new War(GamesService, fileId, team1, team2, aiMarble, team1Boost, team2Boost);
            await ReplyAsync(embed: new EmbedBuilder()
                .WithColor(GetColor(Context))
                .WithCurrentTimestamp()
                .WithDescription("Use `mb/war attack <marble name>` to attack with your weapon and `mb/war bash <marble name>` to attack without.")
                .WithTitle("Let the battle commence!")
                .AddField($"Team {war.Team1.Name}", $"Boost: **{Enum.GetName(typeof(WarBoost), team1Boost).CamelToTitleCase()}**\n{t1Output}")
                .AddField($"Team {war.Team2.Name}", $"Boost: **{Enum.GetName(typeof(WarBoost), team2Boost).CamelToTitleCase()}**\n{t2Output}")
                .Build());
            if (pings.Length != 0) await ReplyAsync(pings.ToString());
            GamesService.WarInfo.GetOrAdd(fileId, war);
            war.Actions = Task.Run(async () => { await war.WarActions(Context); });
        }

        [Command("stop")]
        [RequireOwner]
        public async Task WarStopCommand()
        {
            GamesService.WarInfo[Context.IsPrivate ? Context.User.Id : Context.Guild.Id].Dispose();
            await ReplyAsync("War successfully stopped.");
        }

        [Command("attack", RunMode = RunMode.Async)]
        [Summary("Attacks a member of the opposing team with the equipped weapon.")]
        public async Task WarAttackCommand([Remainder] string target)
        {
            ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;

            if (!GamesService.WarInfo.ContainsKey(fileId))
            {
                await SendErrorAsync($"**{Context.User.Username}**, there is no currently ongoing war!");
                return;
            }

            var war = GamesService.WarInfo[fileId];
            var currentMarble = war.AllMarbles.Where(m => m.Id == Context.User.Id).FirstOrDefault();
            if (currentMarble == null)
            {
                await SendErrorAsync($"**{Context.User.Username}**, you are not in this battle!");
                return;
            }

            if (currentMarble.HP < 1)
            {
                await SendErrorAsync($"**{Context.User.Username}**, you are out and can no longer attack!");
                return;
            }

            if (DateTime.UtcNow.Subtract(currentMarble.LastMoveUsed).TotalSeconds < 5)
            {
                await SendErrorAsync($"**{Context.User.Username}**, you must wait for {GetDateString(currentMarble.LastMoveUsed.Subtract(DateTime.UtcNow.AddSeconds(-5)))} until you can attack again!");
                return;
            }

            if (currentMarble.Rage && DateTime.UtcNow.Subtract(currentMarble.LastRage).Seconds > 20)
            {
                currentMarble.DamageIncrease = (currentMarble.DamageIncrease - 100) / 2;
                currentMarble.Rage = false;
            }

            var user = GetUser(Context);
            var ammo = new Ammo();
            if (currentMarble.WarClass == WeaponClass.Ranged)
            {
                var ammoId = 0u;
                for (int i = currentMarble.Weapon.Ammo.Length - 1; i >= 0; i--)
                {
                    if (user.Items.ContainsKey(currentMarble.Weapon.Ammo[i]) && user.Items[currentMarble.Weapon.Ammo[i]] >= currentMarble.Weapon.Uses)
                    {
                        ammoId = currentMarble.Weapon.Ammo[i];
                        break;
                    }
                }
                if (ammoId == 0)
                {
                    await SendErrorAsync($"{Context.User.Username}, you do not have enough ammo to use the weapon {currentMarble.Weapon.Name}!");
                    return;
                }

                ammo = GetItem<Ammo>(ammoId.ToString("000"));
                var obj = GetUsersObject();
                user.Items[ammo.Id] -= currentMarble.Weapon.Uses;
                user.NetWorth -= ammo.Price * currentMarble.Weapon.Uses;
                WriteUsers(obj, Context.User, user);
            }

            var enemyTeam = currentMarble.Team == 1 ? war.Team2 : war.Team1;
            WarMarble enemyMarble = null;
            if (int.TryParse(target, out int index) && enemyTeam.Marbles.Count >= index)
            {
                enemyMarble = enemyTeam.Marbles.ElementAt(index - 1);
            }
            else
            {
                foreach (WarMarble enemy in enemyTeam.Marbles)
                {
                    if (string.Compare(enemy.Name, target, true) == 0)
                    {
                        enemyMarble = enemy;
                        break;
                    }
                }
                await ReplyAsync($"**{currentMarble.Name}**, could not find the enemy!");
                return;
            }

            if (enemyMarble.HP < 0)
            {
                await SendErrorAsync($"**{Context.User.Username}**, you cannot attack a dead marble!");
                return;
            }

            var builder = new EmbedBuilder()
                .WithColor(GetColor(Context))
                .WithCurrentTimestamp()
                .WithTitle($"**{currentMarble.Name}** attacks!");
            currentMarble.LastMoveUsed = DateTime.UtcNow;
            if (currentMarble.Weapon.Uses == 1)
            {
                if (Rand.Next(0, 100) < currentMarble.Weapon.Accuracy)
                {
                    var damage = (int)Math.Round((currentMarble.Weapon.Damage + (currentMarble.WarClass == WeaponClass.Ranged ? ammo.Damage : 0)) * (1 + currentMarble.DamageIncrease / 100d) * (1 - 0.2 * Convert.ToDouble(enemyMarble.Shield.Id == 63) * (0.5 + Rand.NextDouble())));
                    enemyMarble.HP -= damage;
                    currentMarble.DamageDealt += damage;
                    await ReplyAsync(embed: builder
                        .AddField("Remaining HP", $"**{enemyMarble.HP}**/{enemyMarble.MaxHP}")
                        .WithDescription($"**{currentMarble.Name}** dealt **{damage}** damage to **{enemyMarble.Name}** with **{currentMarble.Weapon.Name}**!")
                        .Build());
                }
                else await ReplyAsync(embed: builder
                    .WithDescription($"**{currentMarble.Name}** tried to attack **{enemyMarble.Name}** but missed!")
                    .Build());
            }
            else
            {
                var totalDamage = 0;
                for (int i = 0; i < currentMarble.Weapon.Uses; i++)
                {
                    if (Rand.Next(0, 100) < currentMarble.Weapon.Accuracy)
                    {
                        var damage = (int)Math.Round(currentMarble.Weapon.Damage + (currentMarble.WarClass == WeaponClass.Ranged ? ammo.Damage : 0) * (1 + currentMarble.DamageIncrease / 100d) * (1 - 0.2 * Convert.ToDouble(enemyMarble.Shield.Id == 63) * (0.5 + Rand.NextDouble())));
                        enemyMarble.HP -= damage;
                        totalDamage += damage;
                        builder.AddField($"Attack {i}", $"**{damage}** damage to **{enemyMarble.Name}**.");
                    }
                    else builder.AddField($"Attack {i}", "Missed!");
                }
                currentMarble.DamageDealt += totalDamage;
                await ReplyAsync(embed: builder
                    .WithDescription($"**{currentMarble.Name}** dealt a total of **{totalDamage}** to **{enemyMarble.Name}** with **{currentMarble.Weapon.Name}**!")
                    .Build());
            }

            if (war.Team1.Marbles.Sum(m => m.HP) < 1 || war.Team2.Marbles.Sum(m => m.HP) < 1)
                await war.End(Context);
        }

        [Command("bash", RunMode = RunMode.Async)]
        [Alias("bonk", "charge")]
        [Summary("Attacks a member of the opposing team without a weapon.")]
        public async Task WarBashCommand([Remainder] string target)
        {
            ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;

            if (!GamesService.WarInfo.ContainsKey(fileId))
            {
                await SendErrorAsync($"**{Context.User.Username}**, there is no currently ongoing war!");
                return;
            }

            var war = GamesService.WarInfo[fileId];
            var currentMarble = war.AllMarbles.Where(m => m.Id == Context.User.Id).FirstOrDefault();
            if (currentMarble == null)
            {
                await SendErrorAsync($"**{Context.User.Username}**, you are not in this battle!");
                return;
            }

            if (currentMarble.HP < 1)
            {
                await SendErrorAsync($"**{Context.User.Username}**, you are out and can no longer attack!");
                return;
            }

            if (DateTime.UtcNow.Subtract(currentMarble.LastMoveUsed).TotalSeconds < 5)
            {
                await SendErrorAsync($"**{Context.User.Username}**, you must wait for {GetDateString(currentMarble.LastMoveUsed.Subtract(DateTime.UtcNow.AddSeconds(-5)))} until you can attack again!");
                return;
            }

            if (currentMarble.Rage && DateTime.UtcNow.Subtract(currentMarble.LastRage).Seconds > 20)
            {
                currentMarble.DamageIncrease = (currentMarble.DamageIncrease - 100) / 2;
                currentMarble.Rage = false;
            }

            var user = GetUser(Context);
            var enemyTeam = currentMarble.Team == 1 ? war.Team2 : war.Team1;
            WarMarble enemyMarble = null;
            if (int.TryParse(target, out int index) && enemyTeam.Marbles.Count >= index + 1)
            {
                enemyMarble = enemyTeam.Marbles.ElementAt(index - 1);
            }
            else
            {
                foreach (WarMarble enemy in enemyTeam.Marbles)
                {
                    if (string.Compare(enemy.Name, target, true) == 0)
                    {
                        enemyMarble = enemy;
                        break;
                    }
                }
                await ReplyAsync($"**{currentMarble.Name}**, could not find the enemy!");
                return;
            }

            if (enemyMarble.HP < 0)
            {
                await SendErrorAsync($"**{Context.User.Username}**, you cannot attack a dead marble!");
                return;
            }

            currentMarble.LastMoveUsed = DateTime.UtcNow;
            var dmg = (int)Math.Round(3 * (1 + currentMarble.DamageIncrease / 50d) * (1 - 0.2 * Convert.ToDouble(enemyMarble.Shield.Id == 63) * (1 + 0.5 * Rand.NextDouble())));
            enemyMarble.HP -= dmg;
            currentMarble.DamageDealt += dmg;
            await ReplyAsync(embed: new EmbedBuilder()
                .AddField("Remaining HP", $"**{enemyMarble.HP}**/{enemyMarble.MaxHP}")
                .WithColor(GetColor(Context))
                .WithCurrentTimestamp()
                .WithDescription($"**{currentMarble.Name}** dealt **{dmg}** damage to **{enemyMarble.Name}**!")
                .WithTitle($"**{currentMarble.Name}** attacks!")
                .Build());

            if (war.Team1.Marbles.Sum(m => m.HP) < 1 || war.Team2.Marbles.Sum(m => m.HP) < 1)
                await war.End(Context);
        }

        [Command("boost", RunMode = RunMode.Async)]
        [Alias("useboost")]
        [Summary("Activates the team's boost if enough team members have boosted.")]
        public async Task WarBoostCommand()
        {
            ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;

            if (!GamesService.WarInfo.ContainsKey(fileId))
            {
                await SendErrorAsync($"**{Context.User.Username}**, there is no currently ongoing war!");
                return;
            }

            var war = GamesService.WarInfo[fileId];
            var currentMarble = war.AllMarbles.Where(m => m.Id == Context.User.Id).FirstOrDefault();
            if (currentMarble == null)
            {
                await SendErrorAsync($"**{Context.User.Username}**, you are not in this battle!");
                return;
            }

            if (currentMarble.HP < 1)
            {
                await SendErrorAsync($"**{Context.User.Username}**, you are out and can no longer attack!");
                return;
            }

            var currentTeam = currentMarble.Team == 1 ? war.Team1 : war.Team2;
            if (currentTeam.BoostUsed)
            {
                await SendErrorAsync($"**{Context.User.Username}**, your team's boost has already been used!");
                return;
            }

            currentMarble.Boosted = true;
            var enemyTeam = currentMarble.Team == 1 ? war.Team2 : war.Team1;

            var builder = new EmbedBuilder()
                .WithAuthor(Context.User)
                .WithColor(GetColor(Context))
                .WithCurrentTimestamp()
                .WithDescription($"**{Context.User.Username}** has attempted to use Team {currentTeam.Name}'s boost!");

            // Activate boost if enough team members (half rounded up) have chosen to boost
            var boosters = currentTeam.Marbles.Aggregate(0, (total, m) => m.Boosted ? total + 1 : total);
            var boostsRequired = Convert.ToInt32(Math.Ceiling(currentTeam.Marbles.Count / 2d));
            var output = new StringBuilder();
            if (boosters >= boostsRequired)
            {
                currentTeam.BoostUsed = true;
                switch (currentTeam.Boost)
                {
                    case WarBoost.HealKit:
                        {
                            var teammatesToHeal = currentTeam.Marbles.OrderBy(m => Guid.NewGuid()).Take(boostsRequired);
                            foreach (var teammate in teammatesToHeal)
                            {
                                if (teammate.HP > 0)
                                {
                                    teammate.HP += 8;
                                    output.AppendLine($"**{teammate.Name}** recovered **8** HP! (**{teammate.HP}**/{teammate.MaxHP})");
                                }
                            }
                            break;
                        }
                    case WarBoost.MissileStrike:
                        {
                            foreach (var enemy in enemyTeam.Marbles)
                            {
                                if (enemy.HP > 0) enemy.HP -= 5;
                            }
                            output.Append($"All of Team **{enemyTeam.Name}** took **5** damage!");
                            break;
                        }
                    case WarBoost.Rage:
                        {
                            foreach (var teammate in currentTeam.Marbles)
                            {
                                teammate.DamageIncrease += 100 + teammate.DamageIncrease;
                                teammate.LastRage = DateTime.UtcNow;
                                teammate.Rage = true;
                            }
                            output.Append($"Team **{currentTeam.Name}** can deal x2 damage for the next 10 seconds!");
                            break;
                        }
                    case WarBoost.SpikeTrap:
                        {
                            var enemiesToDamage = enemyTeam.Marbles.OrderBy(m => Guid.NewGuid()).Take(boostsRequired);
                            foreach (var enemy in enemiesToDamage)
                            {
                                if (enemy.HP > 0)
                                {
                                    enemy.HP -= 8;
                                    output.AppendLine($"**{enemy.Name}** took **8** damage! (**{enemy.HP}**/{enemy.MaxHP})");
                                }
                            }
                            break;
                        }
                }
                builder.AddField("Boost successful!", output.ToString())
                    .WithTitle($"{currentTeam.Name}: **{Enum.GetName(typeof(WarBoost), currentTeam.Boost).CamelToTitleCase()}** used!");
            }
            else
            {
                builder.AddField("Boost failed!",
                    $"**{boosters}** out of the required **{boostsRequired}** team members have chosen to use Team {currentTeam.Name}'s **{Enum.GetName(typeof(WarBoost), currentTeam.Boost).CamelToTitleCase()}**.");
            }

            await ReplyAsync(embed: builder.Build());
        }

        [Command("checkearn")]
        [Summary("Shows whether you can earn money from wars and if not, when.")]
        public async Task WarCheckearnCommand()
        => await Checkearn(Context, Type);

        [Command("clear")]
        [Summary("Clears the list of contestants.")]
        public async Task WarClearCommand()
        => await Clear(Context, Type);

        [Command("contestants")]
        [Alias("marbles", "participants")]
        [Summary("Shows a list of all the contestants in the war.")]
        public async Task WarContestantsCommand()
        => await ShowContestants(Context, Type);

        [Command("info")]
        [Summary("Shows information about the war.")]
        public async Task WarInfoCommand()
        {
            ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
            var builder = new EmbedBuilder()
                .WithColor(GetColor(Context))
                .WithCurrentTimestamp()
                .WithTitle("War Info");
            if (GamesService.WarInfo.ContainsKey(fileId))
            {
                var t1Output = new StringBuilder();
                var t2Output = new StringBuilder();
                var war = GamesService.WarInfo[fileId];
                foreach (var marble in war.Team1.Marbles)
                {
                    var user = Context.Client.GetUser(marble.Id);
                    t1Output.AppendLine($"{marble.Name} (HP: **{marble.HP}**/{marble.MaxHP}, Wpn: {marble.Weapon}) [{user.Username}#{user.Discriminator}]");
                }
                foreach (var marble in war.Team2.Marbles)
                {
                    var user = Context.Client.GetUser(marble.Id);
                    t2Output.AppendLine($"{marble.Name} (HP: **{marble.HP}**/{marble.MaxHP}, Wpn: {marble.Weapon}) [{user.Username}#{user.Discriminator}]");
                }
                builder.AddField($"Team {war.Team1.Name}", t1Output.ToString())
                    .AddField($"Team {war.Team2.Name}", t2Output.ToString());
            }
            else
            {
                var marbles = new StringBuilder();
                using (var marbleList = new StreamReader($"Data{Path.DirectorySeparatorChar}{fileId}war.csv"))
                {
                    var allMarbles = (await marbleList.ReadToEndAsync()).Split('\n');
                    if (allMarbles.Length > 1)
                    {
                        foreach (string marble in allMarbles)
                        {
                            if (marble.Length > 16)
                            {
                                var mSplit = marble.Split(',');
                                var user = Context.Client.GetUser(ulong.Parse(mSplit[1]));
                                var item = GetItem<Item>(int.Parse(mSplit[2]).ToString("000"));
                                if (Context.IsPrivate) marbles.AppendLine($"**{mSplit[0]} (Weapon: **{item.Name}**)**");
                                else marbles.AppendLine($"**{mSplit[0]}** (Weapon: **{item.Name}**) [{user.Username}#{user.Discriminator}]");
                            }
                        }
                    }
                    else marbles.Append("No contestants have signed up!");
                }
                builder.AddField("Marbles", marbles.ToString());
                builder.WithDescription("War not started yet.");
            }
            await ReplyAsync(embed: builder.Build());
        }

        [Command("leaderboard")]
        [Alias("leaderboard mostused")]
        [Summary("Shows a leaderboard of most used marbles in wars.")]
        public async Task WarLeaderboardCommand(string rawPage = "1")
        {
            if (int.TryParse(rawPage, out int page))
            {
                var winners = new SortedDictionary<string, int>();
                using (var win = new StreamReader($"Data{Path.DirectorySeparatorChar}WarMostUsed.txt"))
                {
                    while (!win.EndOfStream)
                    {
                        var racerInfo = await win.ReadLineAsync();
                        if (winners.ContainsKey(racerInfo)) winners[racerInfo]++;
                        else winners.Add(racerInfo, 1);
                    }
                }
                var winList = new List<(string elementName, int value)>();
                foreach (var winner in winners)
                    winList.Add((winner.Key, winner.Value));
                winList = (from winner in winList orderby winner.value descending select winner).ToList();
                await ReplyAsync(embed: new EmbedBuilder()
                    .WithColor(GetColor(Context))
                    .WithCurrentTimestamp()
                    .WithDescription(Leaderboard(winList, page))
                    .WithTitle("War Leaderboard: Most Used")
                    .Build());
            }
            else await ReplyAsync("This is not a valid number! Format: `mb/war leaderboard <optional number>`");
        }

        [Command("ping")]
        [Summary("Toggles whether you are pinged when a war that you are in starts.")]
        public async Task WarPingCommand(string option = "")
        {
            var obj = GetUsersObject();
            var user = GetUser(Context, obj);
            switch (option)
            {
                case "enable":
                case "true":
                case "on": user.WarPing = true; break;
                case "disable":
                case "false":
                case "off": user.WarPing = false; break;
                default: user.WarPing = !user.WarPing; break;
            }
            obj.Remove(Context.User.Id.ToString());
            obj.Add(new JProperty(Context.User.Id.ToString(), JObject.FromObject(user)));
            WriteUsers(obj);
            if (user.WarPing) await ReplyAsync($"**{Context.User.Username}**, you will now be pinged when a war that you are in starts.\n(type `mb/war ping` to turn off)");
            else await ReplyAsync($"**{Context.User.Username}**, you will no longer be pinged when a war that you are in starts.\n(type `mb/war ping` to turn on)");
        }

        [Command("remove")]
        [Summary("Removes a contestant from the contestant list.")]
        public async Task WarRemoveCommand([Remainder] string marbleToRemove)
        => await RemoveContestant(Context, Type, marbleToRemove);

        [Command("valid")]
        [Alias("validweapons")]
        [Summary("Shows all valid weapons to use in war battles.")]
        public async Task WarValidWeaponsCommand()
        {
            var items = GetItemsObject().ToObject<Dictionary<string, Weapon>>();
            var output = new StringBuilder();
            foreach (var itemPair in items)
            {
                var item = itemPair.Value;
                item.Id = uint.Parse(itemPair.Key);
                if (item.WarClass != 0 && item.WarClass != WeaponClass.Artillery && item.Stage <= GetUser(Context).Stage)
                    output.AppendLine($"{item} ({Enum.GetName(typeof(WeaponClass), item.WarClass)})");
            }
            await ReplyAsync(embed: new EmbedBuilder()
                .WithColor(GetColor(Context))
                .WithCurrentTimestamp()
                .WithDescription(output.ToString())
                .WithTitle("Marble War: Valid Weapons")
                .Build());
        }

        [Command("help")]
        [Alias("")]
        [Priority(-1)]
        [Summary("War help.")]
        public async Task WarHelpCommand([Remainder] string _ = "")
            => await ReplyAsync(embed: new EmbedBuilder()
                .AddField("How to play", new StringBuilder()
                        .AppendLine("Use `mb/war signup <weapon ID> <marble name>` to sign up as a marble!")
                        .AppendLine("When everyone's done, use `mb/war start`! The war begins automatically if 20 marbles have signed up.")
                        .Append("\nWhen the war begins, use `mb/war attack <marble code>` to attack an enemy with your weapon")
                        .AppendLine($" and `mb/war bash <marble name>` to attack without.{(GetUser(Context).Stage > 1 ? "Spikes are twice as effective with `mb/war bash`." : "")}")
                        .Append("\nEveryone is split into two teams. If there is an odd number of contestants, an AI marble joins")
                        .AppendLine(" the team that has fewer members!")
                        .ToString())
                .AddField("Valid weapons", new StringBuilder()
                    .AppendLine("Any item that displays a 'War Class' when you use `mb/item` on it is valid. See `mb/war valid` for more.")
                    .AppendLine("Use `mb/item <item ID>` to see the stats for each item. ")
                    .AppendLine("Ranged weapons require ammo to work.")
                    .Append("Bashing is 100% accurate but only has a base damage of 3.")
                    .ToString())
                .AddField("Boost", "Each team is given a boost at the beginning. If enough people on a team use `mb/war boost`, the boost will activate!")
                .WithColor(GetColor(Context))
                .WithCurrentTimestamp()
                .WithTitle("Marble War!")
                .Build());
    }
}
