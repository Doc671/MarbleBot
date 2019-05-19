using Discord;
using Discord.Commands;
using MarbleBot.Core;
using MarbleBot.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarbleBot.Modules
{
    public partial class Games
    {
        [Group("war")]
        [Summary("Participate in a Marble War battle!")]
        [Remarks("Requires a channel in which slowmode is enabled.")]
        public class WarCommand : MarbleBotModule
        {
            [Command("signup")]
            [Alias("join")]
            [Summary("Sign up to the marble war!")]
            [RequireSlowmode]
            public async Task WarSignupCommandAsync(string itemId, [Remainder] string marbleName = "")
            {
                await Context.Channel.TriggerTypingAsync();
                var item = GetItem(itemId);
                if (item.WarClass == 0)
                {
                    await ReplyAsync($"**{Context.User.Username}**, this item cannot be used as a weapon!");
                    return;
                }

                ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                if (marbleName.IsEmpty() || marbleName.Contains("@")) marbleName = Context.User.Username;
                else if (marbleName.Length > 100)
                {
                    await ReplyAsync($"**{Context.User.Username}**, your entry exceeds the 100 character limit.");
                    return;
                }
                else if (Global.WarInfo.ContainsKey(fileId))
                {
                    await ReplyAsync($"**{Context.User.Username}**, a battle is currently ongoing!");
                    return;
                }

                if (!File.Exists($"Data\\{fileId}war.csv")) File.Create($"Data\\{fileId}war.csv").Close();
                using (var marbleList = new StreamReader($"Data\\{fileId}war.csv"))
                {
                    if ((await marbleList.ReadToEndAsync()).Contains(Context.User.Id.ToString()))
                    {
                        await ReplyAsync("You've already joined!");
                        return;
                    }
                }
                marbleName = marbleName.Replace("\n", " ").Replace(",", ";");
                var builder = new EmbedBuilder()
                    .WithColor(GetColor(Context))
                    .WithCurrentTimestamp()
                    .AddField("Marble War: Signed up!", $"**{Context.User.Username}** has successfully signed up as **{marbleName}** with the weapon **{item.Name}**!");
                using (var fighters = new StreamWriter("Data\\WarMostUsed.txt", true))
                    await fighters.WriteLineAsync(marbleName);
                using (var marbleList = new StreamWriter($"Data\\{fileId}war.csv", true))
                    await marbleList.WriteLineAsync($"{marbleName},{Context.User.Id},{item.Id:000}");
                int alive;
                using (var marbleList = new StreamReader($"Data\\{fileId}war.csv"))
                    alive = (await marbleList.ReadToEndAsync()).Split('\n').Length;
                await ReplyAsync(embed: builder.Build());
                if (alive > 20)
                {
                    await ReplyAsync("The limit of 20 fighters has been reached!");
                    await WarStartCommandAsync();
                }
            }

            [Command("start")]
            [Alias("commence")]
            [Summary("Start the Marble War!")]
            [RequireSlowmode]
            public async Task WarStartCommandAsync()
            {
                await Context.Channel.TriggerTypingAsync();
                ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                var marbles = new List<WarMarble>();
                using (var marbleList = new StreamReader($"Data\\{fileId}war.csv"))
                {
                    while (!marbleList.EndOfStream)
                    {
                        var line = (await marbleList.ReadLineAsync()).RemoveChar('\n').Split(',');
                        var userId = ulong.Parse(line[1]);
                        var user = GetUser(Context, userId);
                        marbles.Add(new WarMarble(userId, 50, line[0], GetItem(line[2]),
                            user.Items.ContainsKey(63) && user.Items[63] > 1 ? GetItem("063") : GetItem("000"),
                            user.Items.Where(i => GetItem(i.Key.ToString("000")).Name.Contains("Spikes")).LastOrDefault().Key));
                    }
                }
                var war = new War();
                war.Id = fileId;
                var t1Output = new StringBuilder();
                var t2Output = new StringBuilder();
                foreach (var marble in marbles)
                {
                    if (Global.Rand.Next(0, 2) > 0)
                    {
                        war.Team1.Add(marble);
                        var user = Context.Client.GetUser(marble.Id);
                        marble.Team = 1;
                        t1Output.AppendLine($"**{marble.Name}** [{user.Username}#{user.Discriminator}]");
                    }
                    else
                    {
                        war.Team2.Add(marble);
                        var user = Context.Client.GetUser(marble.Id);
                        marble.Team = 2;
                        t2Output.AppendLine($"**{marble.Name}** [{user.Username}#{user.Discriminator}]");
                    }
                }
                using (var teamNames = new StreamReader("Resources\\WarTeamNames.txt"))
                {
                    var nameArray = teamNames.ReadToEnd().Split('\n');
                    war.Team1Name = nameArray[Global.Rand.Next(0, nameArray.Length)];
                    do
                    {
                        war.Team2Name = nameArray[Global.Rand.Next(0, nameArray.Length)].Trim('\n');
                    }
                    while (string.Compare(war.Team1Name, war.Team2Name, false) == 0);
                }
                if (war.AllMarbles.Count() % 2 > 0)
                {
                    WarMarble aiMarble;
                    if (Math.Round(war.AllMarbles.Sum(m => GetUser(Context, m.Id).Stage) / (double)war.AllMarbles.Count()) == 2)
                        aiMarble = new WarMarble(Global.BotId, 50, "MarbleBot", GetItem(Global.Rand.Next(0, 9) switch
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
                        }), GetItem("063"), Global.Rand.Next(0, 4) switch
                        {
                            0 => 66,
                            1 => 71,
                            2 => 74,
                            _ => 80
                        });
                    else aiMarble = new WarMarble(Global.BotId, 50, "MarbleBot", 
                        GetItem(Global.Rand.Next(0, 2) switch {
                            0 => "094",
                            1 => "095",
                            _ => "096"
                        }), GetItem("000"));
                    if (war.Team1.Count > war.Team2.Count)
                    {
                        aiMarble.Team = 2;
                        war.Team2.Add(aiMarble);
                        t2Output.AppendLine("**MarbleBot** [MarbleBot#7194]");
                    }
                    else
                    {
                        aiMarble.Team = 1;
                        war.Team1.Add(aiMarble);
                        t1Output.AppendLine("**MarbleBot** [MarbleBot#7194]");
                    }
                    war.SetAIMarble(aiMarble);
                }
                await ReplyAsync(embed: new EmbedBuilder()
                    .WithColor(GetColor(Context))
                    .WithCurrentTimestamp()
                    .WithTitle("Let the battle commence!")
                    .AddField($"Team {war.Team1Name}", t1Output.ToString())
                    .AddField($"Team {war.Team2Name}", t2Output.ToString())
                    .Build());
                Global.WarInfo.Add(fileId, war);
                war.Actions = Task.Run(async () => { await war.WarActions(Context); });
            }

            [Command("stop")]
            [RequireOwner]
            public async Task WarStopCommandAsync()
            {
                Global.WarInfo[Context.IsPrivate ? Context.User.Id : Context.Guild.Id].Dispose();
                await ReplyAsync("War successfully stopped.");
            }

            [Command("attack")]
            [Summary("Attacks a member of the opposing team with the equipped weapon.")]
            [RequireSlowmode]
            public async Task WarAttackCommandAsync([Remainder] string target)
            {
                await Context.Channel.TriggerTypingAsync();
                ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                var war = Global.WarInfo[fileId];
                var currentMarble = war.AllMarbles.Where(m => m.Id == Context.User.Id).First();
                var user = GetUser(Context);
                var ammo = new Item();
                if (currentMarble.WarClass == WarClass.Ranged)
                {
                    var ammoId = 0;
                    for (int i = currentMarble.Weapon.Ammo.Length - 1; i >= 0; i--)
                    {
                        if (user.Items[currentMarble.Weapon.Ammo[i]] > 0)
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
                        var dmg = currentMarble.WarClass == WarClass.Ranged 
                            ? (int)Math.Round(currentMarble.Weapon.Damage + ammo.Damage * (1 + currentMarble.DamageIncrease / 100d) * (1 - 0.2 * Convert.ToDouble(user.Items.ContainsKey(63))))
                            : (int)Math.Round(currentMarble.Weapon.Damage * (1 + currentMarble.DamageIncrease / 100d) * (1 - 0.2 * Convert.ToDouble(user.Items.ContainsKey(63))));
                        enemy.HP -= dmg;
                        currentMarble.DamageDealt += dmg;
                        await ReplyAsync(embed: new EmbedBuilder()
                            .WithColor(GetColor(Context))
                            .WithCurrentTimestamp()
                            .WithDescription($"**{currentMarble.Name}** dealt **{dmg}** damage to **{enemy.Name}** with **{currentMarble.Weapon.Name}**!.")
                            .WithTitle($"**{currentMarble.Name}** attacks!")
                            .Build());
                        if (war.Team1.Sum(m => m.HP) > 0 || war.Team2.Sum(m => m.HP) > 0) await war.WarEndAsync(Context);
                        return;
                    }
                }
                await ReplyAsync("Could not find the enemy!");
            }

            [Command("bonk")]
            [Alias("bash", "charge")]
            [Summary("Attacks a member of the opposing team with the equipped weapon.")]
            [RequireSlowmode]
            public async Task WarBonkCommandAsync([Remainder] string target)
            {
                await Context.Channel.TriggerTypingAsync();
                ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                var war = Global.WarInfo[fileId];
                var currentMarble = war.AllMarbles.Where(m => m.Id == Context.User.Id).First();
                var user = GetUser(Context);
                List<WarMarble> enemyTeam = currentMarble.Team == 1 ? war.Team2 : war.Team1;
                for (int i = 0; i < enemyTeam.Count; i++)
                {
                    WarMarble enemy = enemyTeam[i];
                    if (string.Compare(enemy.Name, target, true) == 0)
                    {
                        var dmg = (int)Math.Round(3 * (1 + currentMarble.DamageIncrease / 50d) * (1 - 0.2 * Convert.ToDouble(user.Items.ContainsKey(63))));
                        enemy.HP -= dmg;
                        currentMarble.DamageDealt += dmg;
                        await ReplyAsync(embed: new EmbedBuilder()
                            .WithColor(GetColor(Context))
                            .WithCurrentTimestamp()
                            .WithDescription($"**{currentMarble.Name}** dealt **{dmg}** damage to **{enemy.Name}** with **{currentMarble.Weapon.Name}**!.")
                            .WithTitle($"**{currentMarble.Name}** attacks!")
                            .Build());
                        if (war.Team1.Sum(m => m.HP) > 0 || war.Team2.Sum(m => m.HP) > 0) await war.WarEndAsync(Context);
                        return;
                    }
                }
                await ReplyAsync("Could not find the enemy!");
            }

            [Command("contestants")]
            [Alias("marbles", "participants")]
            [Summary("Shows a list of all the contestants in the War.")]
            [RequireSlowmode]
            public async Task WarContestantsCommandAsync()
            {
                ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                var marbles = new StringBuilder();
                byte cCount = 0;
                using (var marbleList = new StreamReader($"Data\\{fileId}war.csv"))
                {
                    var allMarbles = (await marbleList.ReadToEndAsync()).Split('\n');
                    foreach (var marble in allMarbles)
                    {
                        if (marble.Length > 16)
                        {
                            var mSplit = marble.Split(',');
                            var user = Context.Client.GetUser(ulong.Parse(mSplit[1]));
                            var weapon = GetItem(int.Parse(mSplit[2]).ToString("000"));
                            if (Context.IsPrivate) marbles.AppendLine($"**{mSplit[0]} (Weapon: **{weapon}**)**");
                            else marbles.AppendLine($"**{mSplit[0]}** (Weapon: **{weapon}**) [{user.Username}#{user.Discriminator}]");
                            cCount++;
                        }
                    }
                }
                if (marbles.ToString().IsEmpty()) await ReplyAsync("It looks like there aren't any contestants...");
                else await ReplyAsync(embed: new EmbedBuilder()
                    .AddField("Contestants", marbles.ToString())
                    .WithColor(GetColor(Context))
                    .WithCurrentTimestamp()
                    .WithFooter("Contestant count: " + cCount)
                    .WithTitle("Marble War: Contestants")
                    .Build());
            }

            [Command("info")]
            [Summary("Shows information about the War.")]
            [RequireSlowmode]
            public async Task WarInfoCommandAsync()
            {
                ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                var builder = new EmbedBuilder()
                    .WithColor(GetColor(Context))
                    .WithCurrentTimestamp()
                    .WithTitle("War Info");
                if (Global.WarInfo.ContainsKey(fileId))
                {
                    var t1Output = new StringBuilder();
                    var t2Output = new StringBuilder();
                    var war = Global.WarInfo[fileId];
                    foreach (var marble in war.Team1) {
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
                    using (var marbleList = new StreamReader($"Data\\{fileId}War.csv"))
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

            [Command("")]
            [Alias("help")]
            [Priority(-1)]
            [Summary("Race help.")]
            public async Task WarHelpCommandAsync([Remainder] string _ = "")
                => await ReplyAsync(embed: new EmbedBuilder()
                    .AddField("How to play",
                        new StringBuilder()
                            .AppendLine("Use `mb/war signup <weapon ID> <marble name>` to sign up as a marble!")
                            .AppendLine("When everyone's done, use `mb/war start`! The War begins automatically if 20 people have signed up.")
                            .Append("\nWhen the war begins, use `mb/attack <marble name>` to attack an enemy with your weapon")
                            .AppendLine(" and `mb/bash <marble name>` to attack without. Spikes are twice as effective with `mb/bash`.")
                            .AppendLine("\nYou can earn Units of Money if you win! (6 hour cooldown)")
                            .ToString())
                    .AddField("Valid weapons", new StringBuilder()
                        .AppendLine("Any item that displays a 'War Class' when you use `mb/item` on it is valid.")
                        .AppendLine("Remember that ranged weapons need ammo to work!")
                        .ToString())
                    .WithColor(GetColor(Context))
                    .WithCurrentTimestamp()
                    .WithTitle("Marble War!")
                    .Build());
        }
    }
}
