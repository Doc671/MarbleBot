﻿using Discord;
using Discord.Commands;
using MarbleBot.BaseClasses;
using MarbleBot.Extensions;
using Newtonsoft.Json.Linq;
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
        [Group("siege")]
        [Summary("Participate in a Marble Siege boss battle!")]
        public class SiegeCommand : MarbleBotModule
        {
            private static bool IsBetween(int no, int lower, int upper) => lower <= no && no <= upper;

            [Command("help")]
            [Summary("Siege help.")]
            public async Task SiegeHelpCommandAsync()
                => await ReplyAsync(embed: new EmbedBuilder()
                    .AddField("How to play", new StringBuilder()
                        .AppendLine("Use `mb/siege signup <marble name>` to sign up as a marble! (you can only sign up once)")
                        .AppendLine("When everyone's done, use `mb/siege start`! The Siege begins automatically when 20 people have signed up.\n")
                        .AppendLine("When the Siege begins, use `mb/siege attack` to attack the boss!")
                        .AppendLine("Power-ups occasionally appear. Use `mb/siege grab` to try and activate the power-up (1/3 chance).\n")
                        .AppendLine("Check who's participating with `mb/siege contestants` and view Siege information with `mb/siege info`!")
                        .ToString())
                    .AddField("Mechanics", new StringBuilder()
                        .AppendLine("There are a few differences between this and normal Marble Sieges.\n\n")
                        .AppendLine("**HP Scaling**\nMarble HP scales with difficulty ((difficulty + 2) * 5).\n\n")
                        .AppendLine("**Vengeance**\nWhen a marble dies, the damage multiplier goes up by 0.2 (0.4 if Morale Boost is active).")
                        .ToString())
                    .WithColor(GetColor(Context))
                    .WithCurrentTimestamp()
                    .WithTitle("Marble Siege!")
                    .Build());

            [Command("signup")]
            [Alias("join")]
            [Summary("Sign up to the Marble Siege!")]
            public async Task SiegeSignupCommandAsync([Remainder] string marbleName)
            {
                await Context.Channel.TriggerTypingAsync();
                ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                EmbedBuilder builder = new EmbedBuilder()
                    .WithColor(GetColor(Context))
                    .WithCurrentTimestamp();

                var name = "";
                if (marbleName.IsEmpty() || marbleName.Contains("@")) name = Context.User.Username;
                else if (marbleName.Length > 100)
                    await ReplyAsync("Your entry exceeds the 100 character limit.");
                else if (Global.SiegeInfo.ContainsKey(fileId)) {
                    if (Global.SiegeInfo[fileId].Active)
                        await ReplyAsync("A battle is currently ongoing!");
                }
                if (!File.Exists(fileId.ToString() + "siege.csv")) File.Create(fileId.ToString() + "siege.csv").Close();
                var found = false;
                using (var marbleList = new StreamReader(fileId.ToString() + "siege.csv")) {
                    while (!marbleList.EndOfStream) {
                        var line = await marbleList.ReadLineAsync();
                        found = line.Contains(Context.User.Id.ToString());
                    }
                }
                if (found) await ReplyAsync("You've already joined!");
                marbleName = marbleName.Replace("\n", " ").Replace(",", ";");
                name = marbleName;
                builder.AddField("Marble Siege: Signed up!", "**" + Context.User.Username + "** has successfully signed up as **" + name + "**!");
                using (var siegers = new StreamWriter("Resources\\SiegeMostUsed.txt", true)) await siegers.WriteLineAsync(name);
                if (name.Contains(',')) {
                    var newName = new char[name.Length];
                    for (int i = 0; i < name.Length - 1; i++) {
                        if (name[i] == ',') newName[i] = ';';
                        else newName[i] = name[i];
                    }
                    name = new string(newName);
                }
                using (var marbleList = new StreamWriter(fileId.ToString() + "siege.csv", true)) {
                    await marbleList.WriteLineAsync(name + "," + Context.User.Id);
                    marbleList.Close();
                }
                byte alive = 0;
                using (var marbleList = new StreamReader(fileId.ToString() + "siege.csv")) {
                    var allLines = (await marbleList.ReadToEndAsync()).Split('\n');
                    alive = (byte)allLines.Length;
                    marbleList.Close();
                }
                await ReplyAsync(embed: builder.Build());
                if (alive > 19) {
                    await ReplyAsync("The limit of 20 contestants has been reached!");
                    await SiegeStartCommandAsync();
                }
            }

            [Command("start")]
            [Alias("begin")]
            [Summary("Starts the Marble Siege Battle.")]
            public async Task SiegeStartCommandAsync([Remainder] string over = "") 
            {
                await Context.Channel.TriggerTypingAsync();
                ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                EmbedBuilder builder = new EmbedBuilder()
                    .WithColor(GetColor(Context))
                    .WithCurrentTimestamp();

                if (Global.SiegeInfo.ContainsKey(fileId)) {
                    if (Global.SiegeInfo[fileId].Active) 
                        await ReplyAsync("A battle is currently ongoing!");
                }

                // Get marbles
                byte marbleCount = 0;
                using (var marbleList = new StreamReader(fileId.ToString() + "siege.csv")) {
                    while (!marbleList.EndOfStream) {
                        var line = await marbleList.ReadLineAsync();
                        if (!line.IsEmpty()) marbleCount++;
                        var sLine = line.Split(',');
                        var marble = new Marble() {
                            Id = ulong.Parse(sLine[1]),
                            Name = sLine[0]
                        };
                        if (Global.SiegeInfo.ContainsKey(fileId)) {
                            if (!Global.SiegeInfo[fileId].Marbles.Contains(marble)) Global.SiegeInfo[fileId].Marbles.Add(marble);
                        } else Global.SiegeInfo.Add(fileId, new Siege(new Marble[] { marble }));
                    }
                }
                if (marbleCount == 0) {
                    await ReplyAsync("It doesn't look like anyone has signed up!");
                } else {
                    Global.SiegeInfo[fileId].Active = true;
                    // Pick boss & set battle stats based on boss
                    if (over.Contains("override") && (Context.User.Id == 224267581370925056 || Context.IsPrivate)) {
                        switch (over.Split(' ')[1].ToLower()) {
                            case "pree": Global.SiegeInfo[fileId].Boss = Global.PreeTheTree; break;
                            case "preethetree": goto case "pree";
                            case "helpme": Global.SiegeInfo[fileId].Boss = Global.HelpMeTheTree; break;
                            case "help": goto case "help";
                            case "helpmethetree": goto case "helpme";
                            case "hattmann": Global.SiegeInfo[fileId].Boss = Global.HATTMANN; break;
                            case "hatt": Global.SiegeInfo[fileId].Boss = Global.HATTMANN; break;
                            case "orange": Global.SiegeInfo[fileId].Boss = Global.Orange; break;
                            case "erango": Global.SiegeInfo[fileId].Boss = Global.Erango; break;
                            case "octopheesh": Global.SiegeInfo[fileId].Boss = Global.Octopheesh; break;
                            case "green": Global.SiegeInfo[fileId].Boss = Global.Green; break;
                            case "destroyer": Global.SiegeInfo[fileId].Boss = Global.Destroyer; break;
                        }
                    } else {
                        var bossWeight = (int)Math.Round(Global.SiegeInfo[fileId].Marbles.Count * ((Global.Rand.NextDouble() * 5) + 1));
                        if (IsBetween(bossWeight, 0, 8)) Global.SiegeInfo[fileId].Boss = Global.PreeTheTree;
                        else if (IsBetween(bossWeight, 9, 16)) Global.SiegeInfo[fileId].Boss = Global.HelpMeTheTree;
                        else if (IsBetween(bossWeight, 17, 24)) Global.SiegeInfo[fileId].Boss = Global.HATTMANN;
                        else if (IsBetween(bossWeight, 25, 32)) Global.SiegeInfo[fileId].Boss = Global.Orange;
                        else if (IsBetween(bossWeight, 33, 40)) Global.SiegeInfo[fileId].Boss = Global.Erango;
                        else if (IsBetween(bossWeight, 41, 48)) Global.SiegeInfo[fileId].Boss = Global.Octopheesh;
                        else if (IsBetween(bossWeight, 49, 56)) Global.SiegeInfo[fileId].Boss = Global.Green;
                        else Global.SiegeInfo[fileId].Boss = Global.Destroyer;
                    }
                    var boss = Global.SiegeInfo[fileId].Boss;
                    var hp = ((int)boss.Difficulty + 2) * 5;
                    foreach (var marble in Global.SiegeInfo[fileId].Marbles) {
                        marble.HP = hp;
                        marble.MaxHP = hp;
                    }

                    // Siege Start
                    var cdown = await ReplyAsync("**3**");
                    await Task.Delay(1000);
                    await cdown.ModifyAsync(m => m.Content = "**2**");
                    await Task.Delay(1000);
                    await cdown.ModifyAsync(m => m.Content = "**1**");
                    await Task.Delay(1000);
                    await cdown.ModifyAsync(m => m.Content = "**BEGIN THE SIEGE!**");
                    Global.SiegeInfo[fileId].Actions = Task.Run(async () => { await SiegeBossActionsAsync(fileId); });
                    var marbles = new StringBuilder();
                    var pings = new StringBuilder();
                    foreach (var marble in Global.SiegeInfo[fileId].Marbles) {
                        marbles.AppendLine($"**{marble.Name}** [{Context.Client.GetUser(marble.Id).Username}#{Context.Client.GetUser(marble.Id).Discriminator}]");
                        if (GetUser(Context, marble.Id).SiegePing) pings.Append($"<@{marble.Id}> ");
                    }
                    var bossInfo = new StringBuilder()
                        .AppendLine($"HP: **{boss.HP}**")
                        .AppendLine($"Attacks: **{boss.Attacks.Length}**")
                        .AppendLine($"Difficulty: **{Enum.GetName(typeof(Difficulty), boss.Difficulty)} {(int)boss.Difficulty}**/10")
                        .ToString();
                    builder.WithTitle("The Siege has begun!")
                        .WithDescription("Get ready! Use `mb/siege attack` to attack and `mb/siege grab` to grab power-ups when they appear!")
                        .WithThumbnailUrl(boss.ImageUrl)
                        .AddField($"Marbles: **{Global.SiegeInfo[fileId].Marbles.Count}**", marbles.ToString())
                        .AddField($"Boss: **{boss.Name}**", bossInfo);
                    await ReplyAsync(embed: builder.Build());
                    if (pings.Length != 0) await ReplyAsync(pings.ToString());
                }
            }

            [Command("attack")]
            [Alias("bonk")]
            [Summary("Attacks the boss.")]
            public async Task SiegeAttackCommandAsync()
            {
                await Context.Channel.TriggerTypingAsync();
                ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                EmbedBuilder builder = new EmbedBuilder()
                    .WithColor(GetColor(Context))
                    .WithCurrentTimestamp();

                if (Global.SiegeInfo.ContainsKey(fileId)) {
                    var allIDs = new List<ulong>();
                    foreach (var marble in Global.SiegeInfo[fileId].Marbles) allIDs.Add(marble.Id);
                    if (allIDs.Contains(Context.User.Id)) {
                        var marble = Global.SiegeInfo[fileId].Marbles.Find(m => m.Id == Context.User.Id);
                        if (marble.HP > 0) {
                            if (marble.StatusEffect == MSE.Stun && DateTime.UtcNow.Subtract(marble.LastStun).TotalSeconds > 15) marble.StatusEffect = MSE.None;
                            if (marble.StatusEffect != MSE.Stun) {
                                if (DateTime.UtcNow.Subtract(Global.SiegeInfo[fileId].LastMorale).TotalSeconds > 20 && Global.SiegeInfo[fileId].Morales > 0) {
                                    Global.SiegeInfo[fileId].Morales--;
                                    await ReplyAsync($"The effects of a Morale Boost power-up have worn off! The damage multiplier is now **{Global.SiegeInfo[fileId].DamageMultiplier}**!");
                                }
                                var dmg = Global.Rand.Next(1, 25);
                                if (dmg > 20) dmg = Global.Rand.Next(21, 35);
                                var title = "";
                                var url = "";
                                if (IsBetween(dmg, 1, 7)) {
                                    title = "Slow attack!";
                                    url = "https://cdn.discordapp.com/attachments/296376584238137355/548217423623356418/SiegeAttackSlow.png";
                                } else if (IsBetween(dmg, 8, 14)) {
                                    title = "Fast attack!";
                                    url = "https://cdn.discordapp.com/attachments/296376584238137355/548217417847799808/SiegeAttackFast.png";
                                } else if (IsBetween(dmg, 15, 20)) {
                                    title = "Brutal attack!";
                                    url = "https://cdn.discordapp.com/attachments/296376584238137355/548217407337005067/SiegeAttackBrutal.png";
                                } else if (IsBetween(dmg, 21, 35)) {
                                    title = "CRITICAL attack!";
                                    url = "https://cdn.discordapp.com/attachments/296376584238137355/548217425359798274/SiegeAttackCritical.png";
                                } else title = "Glitch attack!";
                                dmg = marble.StatusEffect == MSE.Chill ? (int)Math.Round(dmg * Global.SiegeInfo[fileId].DamageMultiplier * 0.5) 
                                    : (int)Math.Round(dmg * Global.SiegeInfo[fileId].DamageMultiplier);
                                var clone = false;
                                var userMarble = Global.SiegeInfo[fileId].Marbles.Find(m => m.Id == Context.User.Id);
                                if (marble.Cloned) {
                                    clone = true;
                                    Global.SiegeInfo[fileId].Boss.HP -= dmg * 5;
                                    builder.AddField("Clones attack!", $"Each of the clones dealt **{dmg}** damage to the boss!");
                                    Global.SiegeInfo[fileId].Marbles.Find(m => m.Id == Context.User.Id).Cloned = false;
                                }
                                Global.SiegeInfo[fileId].DealDamage(dmg);
                                userMarble.DamageDealt += dmg;
                                builder.WithTitle(title)
                                    .WithThumbnailUrl(url)
                                    .WithDescription($"**{userMarble.Name}** dealt **{dmg}** damage to **{Global.SiegeInfo[fileId].Boss.Name}**!")
                                    .AddField("Boss HP", $"**{Global.SiegeInfo[fileId].Boss.HP}**/{Global.SiegeInfo[fileId].Boss.MaxHP}");
                                await ReplyAsync(embed: builder.Build());
                                if (clone && marble.Name[marble.Name.Length - 1] != 's')
                                    await ReplyAsync($"{marble.Name}'s clones disappeared!");
                                else if (clone) await ReplyAsync($"{marble.Name}' clones disappeared!");

                                if (Global.SiegeInfo[fileId].Boss.HP < 1) await SiegeVictoryAsync(fileId);

                            } else await ReplyAsync($"**{Context.User.Username}**, you are stunned and cannot attack!");
                        } else await ReplyAsync($"**{Context.User.Username}**, you are out and can no longer attack!");
                    } else await ReplyAsync($"**{Context.User.Username}**, you aren't in this Siege!");
                } else await ReplyAsync("There is no currently ongoing Siege!");
            }

            [Command("grab")]
            [Summary("Has a 1/3 chance of activating the power-up.")]
            public async Task SiegeGrabCommandAsync()
            {
                await Context.Channel.TriggerTypingAsync();
                ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                EmbedBuilder builder = new EmbedBuilder()
                    .WithColor(GetColor(Context))
                    .WithCurrentTimestamp();

                if (Global.SiegeInfo[fileId].Marbles.Any(m => m.Id == Context.User.Id)) {
                    if (Global.SiegeInfo[fileId].PowerUp.IsEmpty()) await ReplyAsync("There is no power-up to grab!");
                    else if (Global.SiegeInfo[fileId].Marbles.Find(m => m.Id == Context.User.Id).HP < 1) await ReplyAsync($"**{Context.User.Username}**, you are out and can no longer grab power-ups!");
                    else {
                        if (Global.Rand.Next(0, 3) == 0) {
                            Global.SiegeInfo[fileId].Marbles.Find(m => m.Id == Context.User.Id).PUHits++;
                            switch (Global.SiegeInfo[fileId].PowerUp) {
                                case "Morale Boost":
                                    Global.SiegeInfo[fileId].Morales++;
                                    builder.WithTitle("POWER-UP ACTIVATED!")
                                        .WithDescription(string.Format("**{0}** activated **Morale Boost**! Damage multiplier increased to **{1}**!", Global.SiegeInfo[fileId].Marbles.Find(m => m.Id == Context.User.Id).Name, Global.SiegeInfo[fileId].DamageMultiplier));
                                    await ReplyAsync(embed: builder.Build());
                                    Global.SiegeInfo[fileId].SetPowerUp("");
                                    Global.SiegeInfo[fileId].LastMorale = DateTime.UtcNow;
                                    break;
                                case "Clone":
                                    Global.SiegeInfo[fileId].Marbles.Find(m => m.Id == Context.User.Id).Cloned = true;
                                    builder.WithTitle("POWER-UP ACTIVATED!")
                                        .WithDescription(string.Format("**{0}** activated **Clone**! Five clones of {0} appeared!", Global.SiegeInfo[fileId].Marbles.Find(m => m.Id == Context.User.Id).Name));
                                    await ReplyAsync(embed: builder.Build());
                                    Global.SiegeInfo[fileId].SetPowerUp("");
                                    break;
                                case "Summon":
                                    var choice = Global.Rand.Next(0, 2);
                                    string ally;
                                    string url;
                                    switch (choice) {
                                        case 0: ally = "Frigidium"; url = "https://cdn.discordapp.com/attachments/296376584238137355/543745898690379816/Frigidium.png"; break;
                                        case 1: ally = "Neptune"; url = "https://cdn.discordapp.com/attachments/296376584238137355/543745899591893012/Neptune.png"; break;
                                        default: ally = "MarbleBot"; url = ""; break;
                                    }
                                    var dmg = Global.Rand.Next(60, 85);
                                    Global.SiegeInfo[fileId].Boss.HP -= (int)Math.Round(dmg * Global.SiegeInfo[fileId].DamageMultiplier);
                                    if (Global.SiegeInfo[fileId].Boss.HP < 0) Global.SiegeInfo[fileId].Boss.HP = 0;
                                    builder.WithTitle("POWER-UP ACTIVATED!")
                                        .WithThumbnailUrl(url)
                                        .AddField("Boss HP", $"**{Global.SiegeInfo[fileId].Boss.HP}**/{Global.SiegeInfo[fileId].Boss.MaxHP}")
                                        .WithDescription($"**{Global.SiegeInfo[fileId].Marbles.Find(m => m.Id == Context.User.Id).Name}** activated **Summon**! **{ally}** came into the arena and dealt **{dmg}** damage to the boss!");
                                    await ReplyAsync(embed: builder.Build());
                                    Global.SiegeInfo[fileId].SetPowerUp("");
                                    break;
                                case "Cure":
                                    var marble = Global.SiegeInfo[fileId].Marbles.Find(m => m.Id == Context.User.Id);
                                    var mse = Enum.GetName(typeof(MSE), marble.StatusEffect);
                                    Global.SiegeInfo[fileId].Marbles.Find(m => m.Id == Context.User.Id).StatusEffect = MSE.None;
                                    builder.WithTitle("Cured!")
                                        .WithDescription($"**{marble.Name}** has been cured of **{mse}**!");
                                    await ReplyAsync(embed: builder.Build());
                                    Global.SiegeInfo[fileId].SetPowerUp("");
                                    break;
                            }
                        } else await ReplyAsync("You failed to grab the power-up!");
                    }
                } else await ReplyAsync($"**{Context.User.Username}**, you aren't in this Siege!");
            }

            [Command("clear")]
            [Summary("Clears the list of contestants.")]
            public async Task SiegeClearCommandAsync()
            {
                ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                if (Context.User.Id == 224267581370925056 || Context.IsPrivate) {
                    using (var marbleList = new StreamWriter(fileId.ToString() + "siege.csv", false)) {
                        await marbleList.WriteAsync("");
                        await ReplyAsync("Contestant list successfully cleared!");
                        marbleList.Close();
                    }
                }
            }

            [Command("checkearn")]
            [Summary("Shows whether you can earn money from Sieges and if not, when.")]
            public async Task SiegeCheckearnCommandAsync()
            {
                var user = GetUser(Context);
                var nextDaily = DateTime.UtcNow.Subtract(user.LastSiegeWin);
                var output = nextDaily.TotalHours < 6 ?
                    $"You can earn money from Sieges in **{GetDateString(user.LastSiegeWin.Subtract(DateTime.UtcNow.AddHours(-6)))}**!"
                    : "You can earn money from Sieges now!";
                await ReplyAsync(embed: new EmbedBuilder()
                    .WithAuthor(Context.User)
                    .WithColor(GetColor(Context))
                    .WithCurrentTimestamp()
                    .WithDescription(output)
                    .Build());
            }

            [Command("contestants")]
            [Alias("marbles", "participants")]
            [Summary("Shows a list of all the contestants in the Siege.")]
            public async Task SiegeContestantsCommandAsync()
            {
                ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                var marbles = new StringBuilder();
                byte cCount = 0;
                using (var marbleList = new StreamReader(fileId.ToString() + "siege.csv")) {
                    var allMarbles = (await marbleList.ReadToEndAsync()).Split('\n');
                    foreach (var marble in allMarbles) {
                        if (marble.Length > 16) {
                            var mSplit = marble.Split(',');
                            var user = Context.Client.GetUser(ulong.Parse(mSplit[1].Trim('\n')));
                            if (Context.IsPrivate) marbles.AppendLine($"**{mSplit[0].Trim('\n')}**");
                            else marbles.AppendLine($"**{mSplit[0].Trim('\n')}** [{user.Username}#{user.Discriminator}]");
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
                    .WithTitle("Marble Siege: Contestants")
                    .Build());
            }

            [Command("remove")]
            [Summary("Removes a contestant from the contestant list.")]
            public async Task SiegeRemoveCommandAsync([Remainder] string marbleToRemove)
            {
                ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                // 0 - Not found, 1 - Found but not yours, 2 - Found & yours, 3 - Found & overridden
                byte state = Context.User.Id == 224267581370925056 ? (byte)3 : (byte)0; 
                var wholeFile = new StringBuilder();
                var id = 0ul;
                using (var marbleList = new StreamReader(fileId.ToString() + "siege.csv")) {
                    while (!marbleList.EndOfStream) {
                        var line = await marbleList.ReadLineAsync();
                        if (line.Split(',')[0] == marbleToRemove) {
                            if (ulong.Parse(line.Split(',')[1]) == Context.User.Id) {
                                id = ulong.Parse(line.Split(',')[1]);
                                state = 2;
                            } else {
                                wholeFile.AppendLine(line);
                                if (!(state == 2)) state = 1;
                            }
                        } else wholeFile.AppendLine(line);
                    }
                }
                switch (state) {
                    case 0: await ReplyAsync("Could not find the requested marble!"); break;
                    case 1: await ReplyAsync("This is not your marble!"); break;
                    case 2: using (var marbleList = new StreamWriter(fileId.ToString() + "siege.csv", false)) {
                            await marbleList.WriteAsync(wholeFile.ToString());
                            await ReplyAsync("Removed contestant **" + marbleToRemove + "**!");
                        }
                        break;
                    case 3: goto case 2;
                }
            }

            [Command("info")]
            [Summary("Shows information about the Siege.")]
            public async Task SiegeInfoCommandAsync()
            {
                ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                var marbles = new StringBuilder();
                var builder = new EmbedBuilder()
                    .WithColor(GetColor(Context))
                    .WithCurrentTimestamp()
                    .WithTitle("Siege Info");
                if (Global.SiegeInfo.ContainsKey(fileId)) {
                    var siege = Global.SiegeInfo[fileId];
                    foreach (var marble in siege.Marbles) 
                        marbles.AppendLine(marble.ToString(Context));
                    var PU = siege.PowerUp.IsEmpty() ? "None" : siege.PowerUp;
                    builder.AddField($"Boss: **{siege.Boss.Name}**", $"\nHP: **{siege.Boss.HP}**/{siege.Boss.MaxHP}\nAttacks: **{siege.Boss.Attacks.Length}**\nDifficulty: **{Enum.GetName(typeof(Difficulty), siege.Boss.Difficulty)}**")
                        .AddField($"Marbles: **{siege.Marbles.Count}**", marbles.ToString())
                        .WithDescription($"Damage Multiplier: **{siege.DamageMultiplier}**\nActive Power-up: **{PU}**")
                        .WithThumbnailUrl(siege.Boss.ImageUrl);
                } else {
                    using (var marbleList = new StreamReader(fileId.ToString() + "siege.csv")) {
                        var allMarbles = (await marbleList.ReadToEndAsync()).Split('\n');
                        if (allMarbles.Length > 1) {
                            foreach (var marble in allMarbles) {
                                if (marble.Length > 16) {
                                    var mSplit = marble.Split(',');
                                    var user = Context.Client.GetUser(ulong.Parse(mSplit[1].Trim('\n')));
                                    marbles.AppendLine($"**{mSplit[0].Trim('\n')}** [{user.Username}#{user.Discriminator}]");
                                }
                            }
                        } else marbles.Append("No contestants have signed up!");
                    }
                    builder.AddField("Marbles", marbles.ToString());
                    builder.WithDescription("Siege not started yet.");
                }
                await ReplyAsync(embed: builder.Build());
            }

            [Command("leaderboard")]
            [Alias("leaderboard mostused")]
            [Summary("Shows a leaderboard of most used marbles in Sieges.")]
            public async Task SiegeLeaderboardCommandAsync()
            {
                var winners = new SortedDictionary<string, int>();
                using (var win = new StreamReader("Resources\\SiegeMostUsed.txt")) {
                    while (!win.EndOfStream) {
                        var racerInfo = await win.ReadLineAsync();
                        if (winners.ContainsKey(racerInfo)) winners[racerInfo]++;
                        else winners.Add(racerInfo, 1);
                    }
                }
                var winList = new List<Tuple<string, int>>();
                foreach (var winner in winners)
                    winList.Add(Tuple.Create(winner.Key, winner.Value));
                winList = (from winner in winList orderby winner.Item2 descending select winner).ToList();
                int i = 1, j = 1;
                var desc = new StringBuilder();
                foreach (var winner in winList) {
                    if (i < 11) {
                        desc.Append($"{i}{i.Ordinal()}: {winner.Item1} {winner.Item2}\n");
                        if (j < winners.Count) if (!(winList[j].Item2 == winner.Item2)) i++;
                        j++;
                    }
                    else break;
                }
                await ReplyAsync(embed: new EmbedBuilder()
                    .WithColor(GetColor(Context))
                    .WithCurrentTimestamp()
                    .WithDescription(desc.ToString())
                    .WithTitle("Siege Leaderboard: Most Used")
                    .Build());
            }

            [Command("boss")]
            [Alias("bossinfo")]
            [Summary("Returns information about a boss.")]
            public async Task SiegeBossCommandAsync([Remainder] string searchTerm)
            {
                var boss = Boss.Empty;
                var state = 1;
                switch (searchTerm.ToLower().RemoveChar(' ')) {
                    case "preethetree": boss = Global.PreeTheTree; break;
                    case "pree": goto case "preethetree";
                    case "hattmann": boss = Global.HATTMANN; break;
                    case "orange": boss = Global.Orange; break;
                    case "green": boss = Global.Green; break;
                    case "destroyer": boss = Global.Destroyer; break;
                    case "helpmethetree": boss = Global.HelpMeTheTree; break;
                    case "helpme": goto case "helpmethetree";
                    case "erango": boss = Global.Erango; break;
                    case "octopheesh": boss = Global.Octopheesh; break;
                    case "frigidium": await ReplyAsync("No."); state = 3; break;
                    case "highwaystickman": goto case "frigidium";
                    case "outcast": goto case "frigidium";
                    case "doon": goto case "frigidium";
                    case "shardberg": goto case "frigidium";
                    case "iceelemental": goto case "frigidium";
                    case "snowjoke": goto case "frigidium";
                    case "pheesh": goto case "frigidium";
                    case "shark": goto case "frigidium";
                    case "pufferfish": goto case "frigidium";
                    case "neptune": goto case "frigidium";
                    case "lavachunk": goto case "frigidium";
                    case "pyromaniac": goto case "frigidium";
                    case "volcano": goto case "frigidium";
                    case "red": goto case "frigidium";
                    case "spaceman": goto case "frigidium";
                    case "rgvzdhjvewvy": goto case "frigidium";
                    case "corruptsoldier": goto case "frigidium";
                    case "corruptpurple": goto case "frigidium";
                    case "chest": goto case "frigidium";
                    case "scaryface": goto case "frigidium";
                    case "marblebot": goto case "frigidium";
                    case "overlord": await ReplyAsync("*Ahahahaha...\n\nYou are sorely mistaken.*"); state = 3; break;
                    case "vinemonster": await ReplyAsync("Excuse me?"); state = 3; break;
                    case "vinemonsters": goto case "vinemonster";
                    case "floatingore": goto case "vinemonster";
                    case "veronica": await ReplyAsync("Woah there! Calm down!"); state = 3; break;
                    case "rockgolem": goto case "veronica";
                    case "minideletion": goto case "veronica";
                    case "alphadeletion": goto case "veronica";
                    case "viii": goto case "veronica";
                    case "triacontapheesh": goto case "veronica";
                    case "hyperguard": goto case "veronica";
                    case "creator": await ReplyAsync("*No...\n\nThis is wrong...*"); state = 3; break;
                    case "doc671": await ReplyAsync("No spoilers here!"); state = 4; break;
                    default: state = 0; break;
                }
                if (state == 1) {
                    var attacks = new StringBuilder();
                    foreach (var attack in boss.Attacks)
                        attacks.AppendLine($"**{attack.Name}** (Accuracy: {attack.Accuracy}%) [Damage: {attack.Damage}] <MSE: {Enum.GetName(typeof(MSE), attack.StatusEffect)}>");
                    var drops = new StringBuilder();
                    foreach (var drop in boss.Drops) {
                        var dropAmount = drop.MinCount == drop.MaxCount ? drop.MinCount.ToString() : $"{drop.MinCount}-{drop.MaxCount}";
                        var idString = drop.ItemId.ToString("000");
                        drops.AppendLine($"`[{idString}]` **{GetItem(idString).Name}**: {dropAmount} ({drop.Chance}%)");
                    }
                    await ReplyAsync(embed: new EmbedBuilder()
                        .AddField("HP", $"**{boss.MaxHP}**")
                        .AddField("Attacks", attacks.ToString())
                        .AddField("Difficulty", $"**{Enum.GetName(typeof(Difficulty), boss.Difficulty)} {(int)boss.Difficulty}**/10")
                        .AddField("Drops", drops.ToString())
                        .WithColor(GetColor(Context))
                        .WithCurrentTimestamp()
                        .WithThumbnailUrl(boss.ImageUrl)
                        .WithTitle(boss.Name)
                        .Build());
                } else if (state == 0) await ReplyAsync("Could not find the requested boss!");
            }

            [Command("bosslist")]
            [Summary("Returns a list of bosses.")]
            public async Task SiegeBosslistCommandAsync()
            {
                var builder = new EmbedBuilder()
                    .WithColor(GetColor(Context))
                    .WithCurrentTimestamp();
                Boss[] playableBosses = { Global.PreeTheTree, Global.HelpMeTheTree, Global.HATTMANN, Global.Orange,
                    Global.Erango, Global.Octopheesh, Global.Green, Global.Destroyer };
                foreach (var boss in playableBosses)
                    builder.AddField($"{boss.Name}", $"Difficulty: **{Enum.GetName(typeof(Difficulty), boss.Difficulty)} {(int)boss.Difficulty}**/10, HP: **{boss.MaxHP}**, Attacks: **{boss.Attacks.Count()}**");
                builder.WithDescription("Use `mb/siege boss <boss name>` for more info!")
                    .WithTitle("Playable MS Bosses");
                await ReplyAsync(embed: builder.Build());
            }

            [Command("powerup")]
            [Alias("power-up", "powerupinfo", "power-upinfo", "puinfo")]
            [Summary("Returns information about a power-up.")]
            public async Task SiegePowerupCommandAsync(string searchTerm)
            {
                var powerup = "";
                var desc = "";
                var url = "";
                switch (searchTerm.ToLower().RemoveChar(' ')) {
                    case "clone": powerup = "Clone";
                        desc = "Spawns five clones of a marble which all attack with the marble then die.";
                        url = "https://cdn.discordapp.com/attachments/296376584238137355/541373091495018496/PUClone.png";
                        break;
                    case "moraleboost": powerup = "Morale Boost";
                        desc = "Doubles the Damage Multiplier for 20 seconds.";
                        url = "https://cdn.discordapp.com/attachments/296376584238137355/541373099090903070/PUMoraleBoost.png";
                        break;
                    case "summon": powerup = "Summon";
                        desc = "Summons an ally to help against the boss.";
                        url = "https://cdn.discordapp.com/attachments/296376584238137355/541373120129531939/PUSummon.png";
                        break;
                    case "cure": powerup = "Cure";
                        desc = "Cures a marble of a status effect.";
                        url = "https://cdn.discordapp.com/attachments/296376584238137355/541373094724501524/PUCure.png";
                        break;
                }
                if (powerup.IsEmpty()) await ReplyAsync("Could not find the requested power-up!");
                else await ReplyAsync(embed: new EmbedBuilder()
                    .WithColor(GetColor(Context))
                    .WithCurrentTimestamp()
                    .WithDescription(desc)
                    .WithThumbnailUrl(url)
                    .WithTitle(powerup)
                    .Build());
            }

            [Command("ping")]
            [Summary("Toggles whether you are pinged when a Siege that you are in starts.")]
            public async Task SiegePingCommandAsync(string option = "")
            {
                var obj = GetUsersObj();
                var user = GetUser(Context, obj);
                switch (option) {
                    case "on": user.SiegePing = true; break;
                    case "off": user.SiegePing = false; break;
                    default: user.SiegePing = !user.SiegePing; break;
                }
                obj.Remove(Context.User.Id.ToString());
                obj.Add(new JProperty(Context.User.Id.ToString(), JObject.FromObject(user)));
                WriteUsers(obj);
                if (user.SiegePing) await ReplyAsync($"**{Context.User.Username}**, you will now be pinged when a Siege that you are in starts.\n(type `mb/siege ping` to turn off)");
                else await ReplyAsync($"**{Context.User.Username}**, you will no longer be pinged when a Siege that you are in starts.\n(type `mb/siege ping` to turn on)");
            }

            // Separate task dealing with time-based boss responses
            public async Task SiegeBossActionsAsync(ulong id) {
                var startTime = DateTime.UtcNow;
                var timeout = false;
                var currentSiege = Global.SiegeInfo[id];
                do {
                    await Task.Delay(currentSiege.AttackTime);
                    if (currentSiege.Boss.HP < 1) {
                        await SiegeVictoryAsync(id);
                        break;
                    } else if (DateTime.UtcNow.Subtract(startTime).TotalMinutes >= 10) {
                        timeout = true;
                        break;
                    } else {
                        // Attack marbles
                        var rand = Global.Rand.Next(0, currentSiege.Boss.Attacks.Length);
                        var atk = currentSiege.Boss.Attacks[rand];
                        var builder = new EmbedBuilder()
                            .WithColor(GetColor(Context))
                            .WithCurrentTimestamp()
                            .WithDescription($"**{currentSiege.Boss.Name}** used **{atk.Name}**!")
                            .WithThumbnailUrl(currentSiege.Boss.ImageUrl)
                            .WithTitle($"WARNING: {atk.Name.ToUpper()} INBOUND!");
                        var hits = 0;
                        foreach (var marble in currentSiege.Marbles) {
                            if (marble.HP > 0) {
                                var likelihood = Global.Rand.Next(0, 100);
                                if (!(likelihood > atk.Accuracy)) {
                                    marble.HP -= atk.Damage;
                                    hits++;
                                    if (marble.HP < 1) {
                                        marble.HP = 0;
                                        builder.AddField($"**{marble.Name}** has been killed!", $"HP: **0**/{marble.MaxHP}\nDamage Multiplier: **{currentSiege.DamageMultiplier}**");
                                    } else {
                                        switch (atk.StatusEffect) {
                                            case MSE.Chill:
                                                marble.StatusEffect = MSE.Chill;
                                                builder.AddField($"**{marble.Name}** has been chilled! All attacks will deal half damage unless cured!", $"HP: **{marble.HP}**/{marble.MaxHP}\nStatus Effect: **Chill**");
                                                break;
                                            case MSE.Doom:
                                                marble.StatusEffect = MSE.Doom;
                                                builder.AddField($"**{marble.Name}** has been doomed and will die in ~45 seconds if not cured!", $"HP: **{marble.HP}**/{marble.MaxHP}\nStatus Effect: **Doom**");
                                                marble.DoomStart = DateTime.UtcNow;
                                                break;
                                            case MSE.Poison:
                                                marble.StatusEffect = MSE.Poison;
                                                builder.AddField($"**{marble.Name}** has been poisoned and will lose HP every ~15 seconds until cured/at 1 HP!", $"HP: **{marble.HP}**/{marble.MaxHP}\nStatus Effect: **Poison**");
                                                break;
                                            case MSE.Stun:
                                                marble.StatusEffect = MSE.Stun;
                                                builder.AddField($"**{marble.Name}** has been stunned and cannot attack for the next ~15 seconds!", $"HP: **{marble.HP}**/{marble.MaxHP}\nStatus Effect: **Stun**");
                                                marble.LastStun = DateTime.UtcNow;
                                                break;
                                            default: builder.AddField($"**{marble.Name}** has been damaged!", $"HP: **{marble.HP}**/{marble.MaxHP}"); break;
                                        }
                                    }
                                }

                                // Perform status effects
                                switch (marble.StatusEffect) {
                                    case MSE.Doom:
                                        if (DateTime.UtcNow.Subtract(marble.DoomStart).TotalSeconds > 45) {
                                            marble.HP = 0;
                                            builder.AddField($"**{marble.Name}** has died of Doom!", $"HP: **0**/{marble.MaxHP}\nDamage Multiplier: **{currentSiege.DamageMultiplier}**");
                                        }
                                        break;
                                    case MSE.Poison:
                                        if (DateTime.UtcNow.Subtract(marble.LastPoisonTick).TotalSeconds > 15) {
                                            if (marble.HP < 1) break;
                                            marble.HP -= (int)Math.Round((double)marble.MaxHP / 10);
                                            marble.LastPoisonTick = DateTime.UtcNow;
                                            if (marble.HP < 2) {
                                                marble.HP = 1;
                                                marble.StatusEffect = MSE.None;
                                            }
                                            builder.AddField($"**{marble.Name}** has taken Poison damage!", $"HP: **{marble.HP}**/{marble.MaxHP}");
                                        }
                                        marble.LastPoisonTick = DateTime.UtcNow;
                                        break;
                                }
                            }
                        }
                        if (hits < 1) builder.AddField("Missed!", "No-one got hurt!");
                        await ReplyAsync(embed: builder.Build());

                        // Wear off Morale Boost
                        if (DateTime.UtcNow.Subtract(currentSiege.LastMorale).TotalSeconds > 20 && currentSiege.Morales > 0) {
                            currentSiege.Morales--;
                            await ReplyAsync($"The effects of a Morale Boost power-up have worn off! The damage multiplier is now **{currentSiege.DamageMultiplier}**!");
                        }

                        // Siege failure
                        if (currentSiege.Marbles.Aggregate(0, (agg, m) => { agg += m.HP; return agg; }) < 1) {
                            var marbles = new StringBuilder();
                            foreach (var marble in currentSiege.Marbles)
                                marbles.AppendLine(marble.ToString(Context));
                            await ReplyAsync(embed: new EmbedBuilder()
                                .WithColor(GetColor(Context))
                                .WithCurrentTimestamp()
                                .WithDescription($"All the marbles died!\n**{currentSiege.Boss.Name}** won!\nFinal HP: **{currentSiege.Boss.HP}**/{currentSiege.Boss.MaxHP}")
                                .AddField($"Fallen Marbles: **{currentSiege.Marbles.Count}**", marbles.ToString())
                                .WithThumbnailUrl(currentSiege.Boss.ImageUrl)
                                .WithTitle("Siege Failure!")
                                .Build());
                            break;
                        }
                    
                        // Cause new power-up to appear
                        if (currentSiege.PowerUp == "") {
                            rand = Global.Rand.Next(0, 8);
                            switch (rand) {
                                case 0:
                                    currentSiege.SetPowerUp("Morale Boost");
                                    await ReplyAsync(embed: new EmbedBuilder()
                                        .WithColor(GetColor(Context))
                                        .WithCurrentTimestamp()
                                        .WithDescription("A **Morale Boost** power-up has spawned in the arena! Use `mb/siege grab` to try and activate it!")
                                        .WithThumbnailUrl(currentSiege.PUImageUrl)
                                        .WithTitle("Power-up spawned!")
                                        .Build());
                                    break;
                                case 1:
                                    currentSiege.SetPowerUp("Clone");
                                    await ReplyAsync(embed: new EmbedBuilder()
                                        .WithColor(GetColor(Context))
                                        .WithCurrentTimestamp()
                                        .WithDescription("A **Clone** power-up has spawned in the arena! Use `mb/siege grab` to try and activate it!")
                                        .WithThumbnailUrl(currentSiege.PUImageUrl)
                                        .WithTitle("Power-up spawned!").Build());
                                    break;
                                case 2:
                                    currentSiege.SetPowerUp("Summon");
                                    await ReplyAsync(embed: new EmbedBuilder()
                                        .WithColor(GetColor(Context))
                                        .WithCurrentTimestamp()
                                        .WithDescription("A **Summon** power-up has spawned in the arena! Use `mb/siege grab` to try and activate it!")
                                        .WithThumbnailUrl(currentSiege.PUImageUrl)
                                        .WithTitle("Power-up spawned!")
                                        .Build());
                                    break;
                                case 3:
                                    if (currentSiege.Marbles.Any(m => m.StatusEffect != MSE.None)) {
                                        currentSiege.SetPowerUp("Cure");
                                         await ReplyAsync(embed: new EmbedBuilder()
                                            .WithColor(GetColor(Context))
                                            .WithCurrentTimestamp()
                                            .WithDescription("A **Cure** power-up has spawned in the arena! Use `mb/siege grab` to try and activate it!")
                                            .WithThumbnailUrl(currentSiege.PUImageUrl)
                                            .WithTitle("Power-up spawned!")
                                            .Build());
                                    }
                                    break;
                            }
                        }
                    }
                } while (currentSiege.Boss.HP > 0 || !timeout || currentSiege.Marbles.Aggregate(0, (agg, m) => { agg += m.HP; return agg; }) > 0);
                if (timeout || currentSiege.Marbles.Aggregate(0, (agg, m) => { agg += m.HP; return agg; }) < 1) {
                    currentSiege.Boss.ResetHP();
                    Global.SiegeInfo.Remove(id);
                }
                using (var marbleList = new StreamWriter(id + "siege.csv", false)) {
                    await marbleList.WriteAsync("");
                    marbleList.Close();
                }
                if (timeout) await ReplyAsync("10 minute timeout reached! Siege aborted!");
            }

            public async Task SiegeVictoryAsync(ulong id) {
                var siege = Global.SiegeInfo[id];
                var builder = new EmbedBuilder()
                    .WithColor(GetColor(Context))
                    .WithCurrentTimestamp()
                    .WithTitle("Siege Victory!")
                    .WithDescription($"**{siege.Boss.Name}** has been defeated!");
                for (int i = 0; i < siege.Marbles.Count; i++) {
                    var marble = siege.Marbles[i];
                    var obj = GetUsersObj();
                    var user = GetUser(Context, obj, marble.Id);
                    int earnings = marble.DamageDealt + (marble.PUHits * 50);
                    if (DateTime.UtcNow.Subtract(user.LastSiegeWin).TotalHours > 6) {
                        var output = new StringBuilder();
                        var didNothing = true;
                        if (marble.DamageDealt > 0) {
                            output.AppendLine($"Damage dealt: {Global.UoM}**{marble.DamageDealt:n}**");
                            didNothing = false;
                        }
                        if (marble.PUHits > 0) {
                            output.AppendLine($"Power-ups grabbed (x50): {Global.UoM}**{marble.PUHits * 50:n}**");
                            didNothing = false;
                        }
                        if (marble.HP > 0) {
                            earnings += 200;
                            output.AppendLine($"Alive bonus: {Global.UoM}**{200:n}**");
                            user.SiegeWins++;
                        }
                        if (output.Length > 0 && !didNothing) {
                            if (marble.HP > 0) user.LastSiegeWin = DateTime.UtcNow;
                            if (siege.Boss.Drops.Length > 0) output.AppendLine("**Item Drops:**");
                            byte drops = 0;
                            foreach (var itemDrops in siege.Boss.Drops) {
                                if (Global.Rand.Next(0, 100) < itemDrops.Chance) {
                                    ushort amount;
                                    if (itemDrops.MinCount == itemDrops.MaxCount) amount = itemDrops.MinCount;
                                    else amount = (ushort)Global.Rand.Next(itemDrops.MinCount, itemDrops.MaxCount + 1);
                                    if (user.Items.ContainsKey(itemDrops.ItemId)) user.Items[itemDrops.ItemId] +=amount;
                                    else user.Items.Add(itemDrops.ItemId, amount);
                                    var item = GetItem(itemDrops.ItemId.ToString("000"));
                                    user.NetWorth += item.Price * amount;
                                    drops++;
                                    output.AppendLine($"`[{itemDrops.ItemId.ToString("000")}]` {GetItem(itemDrops.ItemId.ToString()).Name} x{amount}");
                                }
                            }
                            if (drops == 0) output.AppendLine("None");
                            output.AppendLine($"__**Total: {Global.UoM}{earnings:n}**__");
                            user.Balance += earnings;
                            user.NetWorth += earnings;
                            builder.AddField($"**{Context.Client.GetUser(marble.Id).Username}**'s earnings", output.ToString());
                        }
                    }
                    obj.Remove(marble.Id.ToString());
                    obj.Add(new JProperty(marble.Id.ToString(), JObject.FromObject(user)));
                    WriteUsers(obj);
                }
                await ReplyAsync(embed: builder.Build());
                var boss = Global.SiegeInfo[id].Boss;
                Global.SiegeInfo.Remove(id);
                boss.ResetHP();
                using (var marbleList = new StreamWriter(id.ToString() + "siege.csv", false)) {
                    await marbleList.WriteAsync("");
                    marbleList.Close();
                }
            }
        }
    }
}