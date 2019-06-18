﻿using Discord;
using Discord.Commands;
using MarbleBot.Core;
using MarbleBot.Extensions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static MarbleBot.Global;

namespace MarbleBot.Modules
{
    public partial class Games
    {
        [Group("war")]
        [Summary("Participate in a Marble War battle!")]
        [Remarks("Requires a channel in which slowmode is enabled.")]
        public class WarCommand : MarbleBotModule
        {
            private const GameType Type = GameType.War;

            [Command("signup")]
            [Alias("join")]
            [Summary("Sign up to the Marble War!")]
            [RequireSlowmode]
            public async Task WarSignupCommandAsync(string itemId, [Remainder] string marbleName = "")
            => await SignupAsync(Context, Type, marbleName, 20, async () => { await WarStartCommandAsync(); }, itemId);

            [Command("start")]
            [Alias("commence")]
            [Summary("Start the Marble War!")]
            [RequireSlowmode]
            public async Task WarStartCommandAsync()
            {
                await Context.Channel.TriggerTypingAsync();
                ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                var marbles = new List<WarMarble>();
                using (var marbleList = new StreamReader($"Data{Path.DirectorySeparatorChar}{fileId}war.csv"))
                {
                    while (!marbleList.EndOfStream)
                    {
                        var line = (await marbleList.ReadLineAsync()).RemoveChar('\n').Split(',');
                        var userId = ulong.Parse(line[1]);
                        var user = GetUser(Context, userId);
                        marbles.Add(new WarMarble(userId, 35, line[0], GetItem(line[2]),
                            user.Items.ContainsKey(63) && user.Items[63] > 1 ? GetItem("063") : GetItem("000"),
                            user.Items.Where(i => GetItem(i.Key.ToString("000")).Name.Contains("Spikes")).LastOrDefault().Key));
                    }
                }
                // Shuffles marble list
                for (var i = 0; i < marbles.Count - 1; ++i)
                {
                    var r = Rand.Next(i, marbles.Count);
                    var temp = marbles[i];
                    marbles[i] = marbles[r];
                    marbles[r] = temp;
                }
                var war = new War
                {
                    Id = fileId
                };
                var t1Output = new StringBuilder();
                var t2Output = new StringBuilder();
                var pings = new StringBuilder();
                for (int i = 0; i < marbles.Count; i++)
                {
                    WarMarble marble = marbles[i];
                    if (i < (int)Math.Ceiling(marbles.Count / 2d))
                    {
                        war.Team1.Add(marble);
                        var user = Context.Client.GetUser(marble.Id);
                        marble.Team = 1;
                        t1Output.AppendLine($"**{marble.Name}** [{user.Username}#{user.Discriminator}]");
                        if (GetUser(Context, marble.Id).SiegePing) pings.Append($"<@{marble.Id}> ");
                    }
                    else
                    {
                        war.Team2.Add(marble);
                        var user = Context.Client.GetUser(marble.Id);
                        marble.Team = 2;
                        t2Output.AppendLine($"**{marble.Name}** [{user.Username}#{user.Discriminator}]");
                        if (GetUser(Context, marble.Id).SiegePing) pings.Append($"<@{marble.Id}> ");
                    }
                }
                var nameList = new List<string>();
                using (var teamNames = new StreamReader($"Resources{Path.DirectorySeparatorChar}WarTeamNames.txt"))
                {
                    while (!teamNames.EndOfStream)
                        nameList.Add(await teamNames.ReadLineAsync());
                }
                war.Team1Name = nameList[Rand.Next(0, nameList.Count)];
                do war.Team2Name = nameList[Rand.Next(0, nameList.Count)];
                while (string.Compare(war.Team1Name, war.Team2Name, false) == 0);
                if (war.AllMarbles.Count() % 2 > 0)
                {
                    WarMarble aiMarble;
                    if (Math.Round(war.AllMarbles.Sum(m => GetUser(Context, m.Id).Stage) / (double)war.AllMarbles.Count()) == 2)
                        aiMarble = new WarMarble(BotId, 35, "MarbleBot", GetItem(Rand.Next(0, 9) switch
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
                        }), GetItem("063"), Rand.Next(0, 4) switch
                        {
                            0 => 66,
                            1 => 71,
                            2 => 74,
                            _ => 80
                        });
                    else aiMarble = new WarMarble(BotId, 35, "MarbleBot",
                        GetItem(Rand.Next(0, 2) switch
                        {
                            0 => "094",
                            1 => "095",
                            _ => "096"
                        }), GetItem("000"));
                    aiMarble.Team = 2;
                    war.Team2.Add(aiMarble);
                    t2Output.AppendLine("**MarbleBot** [MarbleBot#7194]");
                    war.SetAIMarble(aiMarble);
                }
                await ReplyAsync(embed: new EmbedBuilder()
                    .WithColor(GetColor(Context))
                    .WithCurrentTimestamp()
                    .WithDescription("Use `mb/war attack <marble name>` to attack with your weapon and `mb/war bash <marble name>` to attack without.")
                    .WithTitle("Let the battle commence!")
                    .AddField($"Team {war.Team1Name}", t1Output.ToString())
                    .AddField($"Team {war.Team2Name}", t2Output.ToString())
                    .Build());
                if (pings.Length != 0) await ReplyAsync(pings.ToString());
                WarInfo.Add(fileId, war);
                war.Actions = Task.Run(async () => { await war.WarActions(Context); });
            }

            [Command("stop")]
            [RequireOwner]
            public async Task WarStopCommandAsync()
            {
                WarInfo[Context.IsPrivate ? Context.User.Id : Context.Guild.Id].Dispose();
                await ReplyAsync("War successfully stopped.");
            }

            [Command("attack")]
            [Summary("Attacks a member of the opposing team with the equipped weapon.")]
            [RequireSlowmode]
            public async Task WarAttackCommandAsync([Remainder] string target)
            {
                await Context.Channel.TriggerTypingAsync();
                ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                var war = WarInfo[fileId];
                var currentMarble = war.AllMarbles.Where(m => m.Id == Context.User.Id).First();
                if (currentMarble.HP < 1)
                {
                    await ReplyAsync($"**{Context.User.Username}**, you are out and can no longer attack!");
                    return;
                }
                var user = GetUser(Context);
                var ammo = new Item();
                if (currentMarble.WarClass == WarClass.Ranged)
                {
                    var ammoId = 0;
                    for (int i = currentMarble.Weapon.Ammo.Length - 1; i >= 0; i--)
                    {
                        if (user.Items.ContainsKey(currentMarble.Weapon.Ammo[i]) && user.Items[currentMarble.Weapon.Ammo[i]] > 0)
                        {
                            ammoId = currentMarble.Weapon.Ammo[i];
                            break;
                        }
                    }
                    if (ammoId == 0)
                    {
                        await ReplyAsync($"{Context.User.Username}, you do not have enough ammo to use the weapon {currentMarble.Weapon.Name}!");
                        return;
                    }
                    else
                    {
                        user.Items[ammoId]--;
                        ammo = GetItem(ammoId.ToString("000"));
                    }
                }
                List<WarMarble> enemyTeam = currentMarble.Team == 1 ? war.Team2 : war.Team1;
                for (int i = 0; i < enemyTeam.Count; i++)
                {
                    WarMarble enemy = enemyTeam[i];
                    if (string.Compare(enemy.Name, target, true) == 0)
                    {
                        if (Rand.Next(0, 10) < 9)
                        {
                            if (enemy.HP < 0)
                            {
                                await ReplyAsync($"**{Context.User.Username}**, you cannot attack a dead marble!");
                                return;
                            }
                            var dmg = currentMarble.WarClass == WarClass.Ranged
                            ? (int)Math.Round(currentMarble.Weapon.Damage + ammo.Damage * (1 + currentMarble.DamageIncrease / 100d) * (1 - 0.2 * Convert.ToDouble(enemy.Shield.Id == 63) * (0.5 + Rand.NextDouble())))
                            : (int)Math.Round(currentMarble.Weapon.Damage * (1 + currentMarble.DamageIncrease / 100d) * (1 - 0.2 * Convert.ToDouble(enemy.Shield.Id == 63) * (0.5 + Rand.NextDouble())));
                            enemy.HP -= dmg;
                            currentMarble.DamageDealt += dmg;
                            await ReplyAsync(embed: new EmbedBuilder()
                                .AddField("Remaining HP", $"**{enemy.HP}**/{enemy.MaxHP}")
                                .WithColor(GetColor(Context))
                                .WithCurrentTimestamp()
                                .WithDescription($"**{currentMarble.Name}** dealt **{dmg}** damage to **{enemy.Name}** with **{currentMarble.Weapon.Name}**!")
                                .WithTitle($"**{currentMarble.Name}** attacks!")
                                .Build());
                            if (war.Team1.Sum(m => m.HP) < 1 || war.Team2.Sum(m => m.HP) < 1) await war.WarEndAsync(Context);
                        }
                        else await Context.Channel.SendMessageAsync(embed: new EmbedBuilder()
                            .WithColor(GetColor(Context))
                            .WithCurrentTimestamp()
                            .WithDescription($"**{currentMarble.Name}** tried to attack **{enemy.Name}** but missed!")
                            .WithTitle($"**{currentMarble.Name}** attacks!")
                            .Build());
                        return;
                    }
                }
                await ReplyAsync("Could not find the enemy!");
            }

            [Command("bash")]
            [Alias("bonk", "charge")]
            [Summary("Attacks a member of the opposing team without a weapon.")]
            [RequireSlowmode]
            public async Task WarBonkCommandAsync([Remainder] string target)
            {
                await Context.Channel.TriggerTypingAsync();
                ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                var war = WarInfo[fileId];
                var currentMarble = war.AllMarbles.Where(m => m.Id == Context.User.Id).First();
                if (currentMarble.HP < 1)
                {
                    await ReplyAsync($"**{Context.User.Username}**, you are out and can no longer attack!");
                    return;
                }
                var user = GetUser(Context);
                List<WarMarble> enemyTeam = currentMarble.Team == 1 ? war.Team2 : war.Team1;
                for (int i = 0; i < enemyTeam.Count; i++)
                {
                    WarMarble enemy = enemyTeam[i];
                    if (string.Compare(enemy.Name, target, true) == 0)
                    {
                        if (enemy.HP < 0)
                        {
                            await ReplyAsync($"**{Context.User.Username}**, you cannot attack a dead marble!");
                            return;
                        }
                        var dmg = (int)Math.Round(3 * (1 + currentMarble.DamageIncrease / 50d) * (1 - 0.2 * Convert.ToDouble(enemy.Shield.Id == 63) * (1 + 0.5 * Rand.NextDouble())));
                        enemy.HP -= dmg;
                        currentMarble.DamageDealt += dmg;
                        await ReplyAsync(embed: new EmbedBuilder()
                            .AddField("Remaining HP", $"**{enemy.HP}**/{enemy.MaxHP}")
                            .WithColor(GetColor(Context))
                            .WithCurrentTimestamp()
                            .WithDescription($"**{currentMarble.Name}** dealt **{dmg}** damage to **{enemy.Name}**!")
                            .WithTitle($"**{currentMarble.Name}** attacks!")
                            .Build());
                        if (war.Team1.Sum(m => m.HP) < 1 || war.Team2.Sum(m => m.HP) < 1) await war.WarEndAsync(Context);
                        return;
                    }
                }
                await ReplyAsync("Could not find the enemy!");
            }

            [Command("checkearn")]
            [Summary("Shows whether you can earn money from wars and if not, when.")]
            public async Task WarCheckearnCommandAsync()
            => await CheckearnAsync(Context, Type);

            [Command("clear")]
            [Summary("Clears the list of contestants.")]
            public async Task WarClearCommandAsync()
            => await ClearAsync(Context, Type);

            [Command("contestants")]
            [Alias("marbles", "participants")]
            [Summary("Shows a list of all the contestants in the war.")]
            [RequireSlowmode]
            public async Task WarContestantsCommandAsync()
            => await ContestantsAsync(Context, Type);

            [Command("info")]
            [Summary("Shows information about the war.")]
            [RequireSlowmode]
            public async Task WarInfoCommandAsync()
            {
                ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                var builder = new EmbedBuilder()
                    .WithColor(GetColor(Context))
                    .WithCurrentTimestamp()
                    .WithTitle("War Info");
                if (WarInfo.ContainsKey(fileId))
                {
                    var t1Output = new StringBuilder();
                    var t2Output = new StringBuilder();
                    var war = WarInfo[fileId];
                    foreach (var marble in war.Team1)
                    {
                        var user = Context.Client.GetUser(marble.Id);
                        t1Output.AppendLine($"{marble.Name} (HP: **{marble.HP}**/{marble.MaxHP}, Wpn: {marble.Weapon}) [{user.Username}#{user.Discriminator}]");
                    }
                    foreach (var marble in war.Team2)
                    {
                        var user = Context.Client.GetUser(marble.Id);
                        t2Output.AppendLine($"{marble.Name} (HP: **{marble.HP}**/{marble.MaxHP}, Wpn: {marble.Weapon}) [{user.Username}#{user.Discriminator}]");
                    }
                    builder.AddField($"Team {war.Team1Name}", t1Output.ToString())
                        .AddField($"Team {war.Team2Name}", t2Output.ToString());
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
                                    var item = GetItem(int.Parse(mSplit[2]).ToString("000"));
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
            public async Task WarLeaderboardCommandAsync(string rawNo = "1")
            {
                if (int.TryParse(rawNo, out int no))
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
                    var winList = new List<(string, int)>();
                    foreach (var winner in winners)
                        winList.Add((winner.Key, winner.Value));
                    winList = (from winner in winList orderby winner.Item2 descending select winner).ToList();
                    await ReplyAsync(embed: new EmbedBuilder()
                        .WithColor(GetColor(Context))
                        .WithCurrentTimestamp()
                        .WithDescription(Leaderboard(winList, no))
                        .WithTitle("War Leaderboard: Most Used")
                        .Build());
                }
                else await ReplyAsync("This is not a valid number! Format: `mb/war leaderboard <optional number>`");
            }

            [Command("ping")]
            [Summary("Toggles whether you are pinged when a war that you are in starts.")]
            public async Task WarPingCommandAsync(string option = "")
            {
                var obj = GetUsersObj();
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
            [RequireSlowmode]
            public async Task WarRemoveCommandAsync([Remainder] string marbleToRemove)
            => await RemoveAsync(Context, Type, marbleToRemove);

            [Command("valid")]
            [Alias("validweapons")]
            [Summary("Shows all valid weapons to use in war battles.")]
            public async Task WarValidWeaponsCommandAsync()
            {
                string json;
                using (var itemFile = new StreamReader($"Resources{Path.DirectorySeparatorChar}Items.json")) json = itemFile.ReadToEnd();
                var obj = JObject.Parse(json);
                var items = obj.ToObject<Dictionary<string, Item>>();
                var output = new StringBuilder();
                foreach (var itemPair in items)
                {
                    var item = new Item(itemPair.Value, int.Parse(itemPair.Key));
                    if (item.WarClass != 0 && item.Stage <= GetUser(Context).Stage)
                        output.AppendLine($"{item} ({Enum.GetName(typeof(WarClass), item.WarClass)})");
                }
                await ReplyAsync(embed: new EmbedBuilder()
                    .WithColor(GetColor(Context))
                    .WithCurrentTimestamp()
                    .WithDescription(output.ToString())
                    .WithTitle("Marble War: Valid Weapons")
                    .Build());
            }

            [Command("")]
            [Alias("help")]
            [Priority(-1)]
            [Summary("War help.")]
            public async Task WarHelpCommandAsync([Remainder] string _ = "")
                => await ReplyAsync(embed: new EmbedBuilder()
                    .AddField("How to play",
                        new StringBuilder()
                            .AppendLine("Use `mb/war signup <weapon ID> <marble name>` to sign up as a marble!")
                            .AppendLine("When everyone's done, use `mb/war start`! The war begins automatically if 20 people have signed up.")
                            .Append("\nWhen the war begins, use `mb/war attack <marble name>` to attack an enemy with your weapon")
                            .AppendLine(" and `mb/war bash <marble name>` to attack without. Spikes are twice as effective with `mb/war bash`.")
                            .Append("\nEveryone is split into two teams. If there is an odd number of contestants, an AI marble joins")
                            .AppendLine(" the team that has fewer members!")
                            .ToString())
                    .AddField("Valid weapons", new StringBuilder()
                        .AppendLine("Any item that displays a 'War Class' when you use `mb/item` on it is valid. See `mb/war valid` for more.")
                        .Append("\nMelee and ranged weapons both have 90% accuracy but ranged weapons require")
                        .Append(" ammo to work. Bashing is 100% accurate but only has a base damage of 3.")
                        .ToString())
                    .WithColor(GetColor(Context))
                    .WithCurrentTimestamp()
                    .WithTitle("Marble War!")
                    .Build());
        }
    }
}
