using Discord;
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
        [Group("siege")]
        [Summary("Participate in a Marble Siege boss battle!")]
        [Remarks("Requires a channel in which slowmode is enabled.")]
        public class SiegeCommand : MarbleBotModule
        {
            private static bool IsBetween(int no, int lower, int upper) => lower <= no && no <= upper;

            [Command("signup")]
            [Alias("join")]
            [Summary("Sign up to the Marble Siege!")]
            [RequireSlowmode]
            public async Task SiegeSignupCommandAsync([Remainder] string marbleName = "")
            {
                await Context.Channel.TriggerTypingAsync();
                ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                EmbedBuilder builder = new EmbedBuilder()
                    .WithColor(GetColor(Context))
                    .WithCurrentTimestamp();

                if (marbleName.IsEmpty() || marbleName.Contains("@")) marbleName = Context.User.Username;
                else if (marbleName.Length > 100)
                {
                    await ReplyAsync($"**{Context.User.Username}**, your entry exceeds the 100 character limit.");
                    return;
                }
                else if (SiegeInfo.ContainsKey(fileId) && SiegeInfo[fileId].Active)
                {
                    await ReplyAsync($"**{Context.User.Username}**, a battle is currently ongoing!");
                    return;
                }
                if (!File.Exists($"Data{Path.DirectorySeparatorChar}{fileId}siege.csv")) File.Create($"Data{Path.DirectorySeparatorChar}{fileId}siege.csv").Close();
                using (var marbleList = new StreamReader($"Data{Path.DirectorySeparatorChar}{fileId}siege.csv"))
                {
                    if ((await marbleList.ReadToEndAsync()).Contains(Context.User.Id.ToString()))
                    {
                        await ReplyAsync("You've already joined!");
                        return;
                    }
                }
                marbleName = marbleName.Replace("\n", " ").Replace(",", ";");
                builder.AddField("Marble Siege: Signed up!", $"**{Context.User.Username}** has successfully signed up as **{marbleName}**!");
                using (var siegers = new StreamWriter("SiegeMostUsed.txt", true))
                    await siegers.WriteLineAsync(marbleName);
                using (var marbleList = new StreamWriter($"Data{Path.DirectorySeparatorChar}{fileId}siege.csv", true))
                    await marbleList.WriteLineAsync($"{marbleName},{Context.User.Id}");
                int alive;
                using (var marbleList = new StreamReader($"Data{Path.DirectorySeparatorChar}{fileId}siege.csv"))
                    alive = (await marbleList.ReadToEndAsync()).Split('\n').Length;
                await ReplyAsync(embed: builder.Build());
                if (alive > 20)
                {
                    await ReplyAsync("The limit of 20 contestants has been reached!");
                    await SiegeStartCommandAsync();
                }
            }

            [Command("start")]
            [Alias("begin")]
            [Summary("Starts the Marble Siege Battle.")]
            [RequireSlowmode]
            public async Task SiegeStartCommandAsync([Remainder] string over = "")
            {
                await Context.Channel.TriggerTypingAsync();
                ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;

                if (SiegeInfo.ContainsKey(fileId) && SiegeInfo[fileId].Active)
                {
                    await ReplyAsync("A battle is currently ongoing!");
                    return;
                }
                // Get marbles
                byte marbleCount = 0;
                using (var marbleList = new StreamReader($"Data{Path.DirectorySeparatorChar}{fileId}siege.csv"))
                {
                    while (!marbleList.EndOfStream)
                    {
                        var line = (await marbleList.ReadLineAsync()).RemoveChar('\n');
                        if (!line.IsEmpty()) marbleCount++;
                        var sLine = line.Split(',');
                        var marble = new SiegeMarble()
                        {
                            Id = ulong.Parse(sLine[1]),
                            Name = sLine[0]
                        };
                        var user = GetUser(Context, marble.Id);
                        if (user.Items.ContainsKey(63) && user.Items[63] >= 1)
                            marble.Shield = GetItem("063");
                        if (user.Items.ContainsKey(80)) marble.DamageIncrease = 110;
                        else if (user.Items.ContainsKey(74)) marble.DamageIncrease = 95;
                        else if (user.Items.ContainsKey(71)) marble.DamageIncrease = 60;
                        else if (user.Items.ContainsKey(66)) marble.DamageIncrease = 40;
                        if (SiegeInfo.ContainsKey(fileId) && !SiegeInfo[fileId].Marbles.Contains(marble))
                            SiegeInfo[fileId].Marbles.Add(marble);
                        else SiegeInfo.Add(fileId, new Siege(Context, new SiegeMarble[] { marble }));
                    }
                }
                if (marbleCount == 0) await ReplyAsync("It doesn't look like anyone has signed up!");
                else
                {
                    var currentSiege = SiegeInfo[fileId];
                    currentSiege.Active = true;
                    // Pick boss & set battle stats based on boss
                    if (over.Contains("override") && (Context.User.Id == 224267581370925056 || Context.IsPrivate))
                        currentSiege.Boss = Siege.GetBoss(over.Split(' ')[1].RemoveChar(' '));
                    else if (string.Compare(currentSiege.Boss.Name, "", true) == 0)
                    {
                        byte stageTotal = 0;
                        foreach (var marble in currentSiege.Marbles)
                        {
                            var user = GetUser(Context, marble.Id);
                            stageTotal += user.Stage;
                        }
                        float stage = stageTotal / (float)currentSiege.Marbles.Count;
                        if (stage == 1f) StageOneBossChooser(currentSiege);
                        else if (stage == 2f) StageTwoBossChooser(currentSiege);
                        else
                        {
                            stage--;
                            if (Rand.NextDouble() < stage) StageTwoBossChooser(currentSiege);
                            else StageOneBossChooser(currentSiege);
                        }
                    }
                    var hp = ((int)currentSiege.Boss.Difficulty + 2) * 5;
                    foreach (var marble in currentSiege.Marbles)
                        marble.SetHP(hp);

                    // Siege Start
                    var cdown = await ReplyAsync("**3**");
                    await Task.Delay(1000);
                    await cdown.ModifyAsync(m => m.Content = "**2**");
                    await Task.Delay(1000);
                    await cdown.ModifyAsync(m => m.Content = "**1**");
                    await Task.Delay(1000);
                    await cdown.ModifyAsync(m => m.Content = "**BEGIN THE SIEGE!**");
                    currentSiege.Actions = Task.Run(async () => { await currentSiege.SiegeBossActionsAsync(Context); });
                    var marbles = new StringBuilder();
                    var pings = new StringBuilder();
                    foreach (var marble in currentSiege.Marbles)
                    {
                        marbles.AppendLine($"**{marble.Name}** [{Context.Client.GetUser(marble.Id).Username}#{Context.Client.GetUser(marble.Id).Discriminator}]");
                        if (GetUser(Context, marble.Id).SiegePing) pings.Append($"<@{marble.Id}> ");
                    }
                    await ReplyAsync(embed: new EmbedBuilder()
                        .WithColor(GetColor(Context))
                        .WithCurrentTimestamp()
                        .WithDescription("Get ready! Use `mb/siege attack` to attack and `mb/siege grab` to grab power-ups when they appear!")
                        .WithTitle("The Siege has begun!")
                        .WithThumbnailUrl(currentSiege.Boss.ImageUrl)
                        .AddField($"Marbles: **{currentSiege.Marbles.Count}**", marbles.ToString())
                        .AddField($"Boss: **{currentSiege.Boss.Name}**", new StringBuilder()
                            .AppendLine($"HP: **{currentSiege.Boss.HP}**")
                            .AppendLine($"Attacks: **{currentSiege.Boss.Attacks.Length}**")
                            .AppendLine($"Difficulty: **{Enum.GetName(typeof(Difficulty), currentSiege.Boss.Difficulty)} {(int)currentSiege.Boss.Difficulty}**/10")
                            .ToString())
                        .Build());
                    if (pings.Length != 0 || (Context.User.Id == 224267581370925056 && !over.Contains("noping")))
                        await ReplyAsync(pings.ToString());
                }
            }

            [Command("stop")]
            [RequireOwner]
            public async Task SiegeStopCommandAsync()
            {
                SiegeInfo[Context.IsPrivate ? Context.User.Id : Context.Guild.Id].Dispose();
                await ReplyAsync("Siege successfully stopped.");
            }

            [Command("attack")]
            [Alias("bonk")]
            [Summary("Attacks the boss.")]
            [RequireSlowmode]
            public async Task SiegeAttackCommandAsync()
            {
                await Context.Channel.TriggerTypingAsync();
                ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                EmbedBuilder builder = new EmbedBuilder()
                    .WithColor(GetColor(Context))
                    .WithCurrentTimestamp();

                if (!SiegeInfo.ContainsKey(fileId) || !SiegeInfo[fileId].Active)
                {
                    await ReplyAsync("There is no currently ongoing Siege!");
                    return;
                }
                var allIDs = new List<ulong>();
                foreach (var marble in SiegeInfo[fileId].Marbles) allIDs.Add(marble.Id);
                if (allIDs.Contains(Context.User.Id))
                {
                    var marble = SiegeInfo[fileId].Marbles.Find(m => m.Id == Context.User.Id);
                    if (marble.HP > 0)
                    {
                        if (marble.StatusEffect == MSE.Stun && DateTime.UtcNow.Subtract(marble.LastStun).TotalSeconds > 15) marble.StatusEffect = MSE.None;
                        if (marble.StatusEffect != MSE.Stun)
                        {
                            if (DateTime.UtcNow.Subtract(SiegeInfo[fileId].LastMorale).TotalSeconds > 20 && SiegeInfo[fileId].Morales > 0)
                            {
                                SiegeInfo[fileId].Morales--;
                                await ReplyAsync($"The effects of a Morale Boost power-up have worn off! The damage multiplier is now **{SiegeInfo[fileId].DamageMultiplier}**!");
                            }
                            var dmg = Rand.Next(1, 25);
                            if (dmg > 20) dmg = Rand.Next(21, 35);
                            string title;
                            string url;
                            if (IsBetween(dmg, 1, 7))
                            {
                                title = "Slow attack!";
                                url = "https://cdn.discordapp.com/attachments/296376584238137355/548217423623356418/SiegeAttackSlow.png";
                            }
                            else if (IsBetween(dmg, 8, 14))
                            {
                                title = "Fast attack!";
                                url = "https://cdn.discordapp.com/attachments/296376584238137355/548217417847799808/SiegeAttackFast.png";
                            }
                            else if (IsBetween(dmg, 15, 20))
                            {
                                title = "Brutal attack!";
                                url = "https://cdn.discordapp.com/attachments/296376584238137355/548217407337005067/SiegeAttackBrutal.png";
                            }
                            else
                            {
                                title = "CRITICAL attack!";
                                url = "https://cdn.discordapp.com/attachments/296376584238137355/548217425359798274/SiegeAttackCritical.png";
                            }
                            dmg = marble.StatusEffect == MSE.Chill ? (int)Math.Round(dmg * SiegeInfo[fileId].DamageMultiplier * 0.5 * (marble.DamageIncrease / 100.0 + 1))
                                : (int)Math.Round(dmg * SiegeInfo[fileId].DamageMultiplier * (marble.DamageIncrease / 100.0 + 1));
                            var clone = false;
                            var userMarble = SiegeInfo[fileId].Marbles.Find(m => m.Id == Context.User.Id);
                            if (marble.Cloned)
                            {
                                clone = true;
                                await SiegeInfo[fileId].DealDamageAsync(Context, dmg * 5);
                                builder.AddField("Clones attack!", $"Each of the clones dealt **{dmg}** damage to the boss!");
                                userMarble.Cloned = false;
                            }
                            await SiegeInfo[fileId].DealDamageAsync(Context, dmg);
                            userMarble.DamageDealt += dmg;
                            builder.WithTitle(title)
                                .WithThumbnailUrl(url)
                                .WithDescription($"**{userMarble.Name}** dealt **{dmg}** damage to **{SiegeInfo[fileId].Boss.Name}**!")
                                .AddField("Boss HP", $"**{SiegeInfo[fileId].Boss.HP}**/{SiegeInfo[fileId].Boss.MaxHP}");
                            await ReplyAsync(embed: builder.Build());
                            if (clone && marble.Name.Last() != 's')
                                await ReplyAsync($"{marble.Name}'s clones disappeared!");
                            else if (clone) await ReplyAsync($"{marble.Name}' clones disappeared!");

                            if (SiegeInfo[fileId].Boss.HP < 1) await SiegeInfo[fileId].SiegeVictoryAsync(Context);

                        }
                        else await ReplyAsync($"**{Context.User.Username}**, you are stunned and cannot attack!");
                    }
                    else await ReplyAsync($"**{Context.User.Username}**, you are out and can no longer attack!");
                }
                else await ReplyAsync($"**{Context.User.Username}**, you aren't in this Siege!");
            }

            [Command("grab")]
            [Summary("Has a 1/3 chance of activating the power-up.")]
            [RequireSlowmode]
            public async Task SiegeGrabCommandAsync()
            {
                await Context.Channel.TriggerTypingAsync();
                ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                EmbedBuilder builder = new EmbedBuilder()
                    .WithColor(GetColor(Context))
                    .WithCurrentTimestamp();

                if (!SiegeInfo.ContainsKey(fileId) && !SiegeInfo[fileId].Active)
                {
                    await ReplyAsync("There is no currently ongoing Siege!");
                    return;
                }

                var currentSiege = SiegeInfo[fileId];
                if (currentSiege.Marbles.Any(m => m.Id == Context.User.Id))
                {
                    if (currentSiege.PowerUp.IsEmpty()) await ReplyAsync("There is no power-up to grab!");
                    else if (currentSiege.Marbles.Find(m => m.Id == Context.User.Id).HP < 1) await ReplyAsync($"**{Context.User.Username}**, you are out and can no longer grab power-ups!");
                    else
                    {
                        if (Rand.Next(0, 3) == 0)
                        {
                            currentSiege.Marbles.Find(m => m.Id == Context.User.Id).PowerUpHits++;
                            switch (currentSiege.PowerUp)
                            {
                                case "Morale Boost":
                                    currentSiege.Morales++;
                                    builder.WithTitle("POWER-UP ACTIVATED!")
                                        .WithDescription($"**{currentSiege.Marbles.Find(m => m.Id == Context.User.Id).Name}** activated **Morale Boost**! Damage multiplier increased to **{currentSiege.DamageMultiplier}**!");
                                    await ReplyAsync(embed: builder.Build());
                                    currentSiege.SetPowerUp("");
                                    currentSiege.LastMorale = DateTime.UtcNow;
                                    break;
                                case "Clone":
                                    currentSiege.Marbles.Find(m => m.Id == Context.User.Id).Cloned = true;
                                    builder.WithTitle("POWER-UP ACTIVATED!")
                                        .WithDescription($"**{currentSiege.Marbles.Find(m => m.Id == Context.User.Id).Name}** activated **Clone**! Five clones of {currentSiege.Marbles.Find(m => m.Id == Context.User.Id).Name} appeared!");
                                    await ReplyAsync(embed: builder.Build());
                                    currentSiege.SetPowerUp("");
                                    break;
                                case "Summon":
                                    var choice = Rand.Next(0, 2);
                                    string ally;
                                    string url;
                                    switch (choice)
                                    {
                                        case 0: ally = "Frigidium"; url = "https://cdn.discordapp.com/attachments/296376584238137355/543745898690379816/Frigidium.png"; break;
                                        case 1: ally = "Neptune"; url = "https://cdn.discordapp.com/attachments/296376584238137355/543745899591893012/Neptune.png"; break;
                                        default: ally = "MarbleBot"; url = ""; break;
                                    }
                                    var dmg = Rand.Next(25, 30) * currentSiege.Boss.Stage * ((int)currentSiege.Boss.Difficulty >> 1);
                                    currentSiege.Boss.HP -= (int)Math.Round(dmg * currentSiege.DamageMultiplier);
                                    if (currentSiege.Boss.HP < 0) currentSiege.Boss.HP = 0;
                                    builder.WithTitle("POWER-UP ACTIVATED!")
                                        .WithThumbnailUrl(url)
                                        .AddField("Boss HP", $"**{currentSiege.Boss.HP}**/{currentSiege.Boss.MaxHP}")
                                        .WithDescription($"**{currentSiege.Marbles.Find(m => m.Id == Context.User.Id).Name}** activated **Summon**! **{ally}** came into the arena and dealt **{dmg}** damage to the boss!");
                                    await ReplyAsync(embed: builder.Build());
                                    currentSiege.SetPowerUp("");
                                    break;
                                case "Cure":
                                    var marble = currentSiege.Marbles.Find(m => m.Id == Context.User.Id);
                                    var mse = Enum.GetName(typeof(MSE), marble.StatusEffect);
                                    builder.WithTitle("Cured!")
                                        .WithDescription($"**{marble.Name}** has been cured of **{mse}**!");
                                    marble.StatusEffect = MSE.None;
                                    await ReplyAsync(embed: builder.Build());
                                    currentSiege.SetPowerUp("");
                                    break;
                            }
                        }
                        else await ReplyAsync("You failed to grab the power-up!");
                    }
                }
                else await ReplyAsync($"**{Context.User.Username}**, you aren't in this Siege!");
            }

            [Command("clear")]
            [Summary("Clears the list of contestants.")]
            public async Task SiegeClearCommandAsync()
            {
                ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                if (Context.User.Id == 224267581370925056 || Context.IsPrivate)
                {
                    using (var marbleList = new StreamWriter($"Data{Path.DirectorySeparatorChar}{fileId}siege.csv", false))
                    {
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
            [RequireSlowmode]
            public async Task SiegeContestantsCommandAsync()
            {
                ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                var marbles = new StringBuilder();
                byte cCount = 0;
                using (var marbleList = new StreamReader($"Data{Path.DirectorySeparatorChar}{fileId}siege.csv"))
                {
                    var allMarbles = (await marbleList.ReadToEndAsync()).Split('\n');
                    foreach (var marble in allMarbles)
                    {
                        if (marble.Length > 16)
                        {
                            var mSplit = marble.Split(',');
                            var user = Context.Client.GetUser(ulong.Parse(mSplit[1]));
                            if (Context.IsPrivate) marbles.AppendLine($"**{mSplit[0]}**");
                            else marbles.AppendLine($"**{mSplit[0]}** [{user.Username}#{user.Discriminator}]");
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
            [RequireSlowmode]
            public async Task SiegeRemoveCommandAsync([Remainder] string marbleToRemove)
            {
                ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                // 0 - Not found, 1 - Found but not yours, 2 - Found & yours, 3 - Found & overridden
                byte state = Context.User.Id == 224267581370925056 ? (byte)3 : (byte)0;
                var wholeFile = new StringBuilder();
                using (var marbleList = new StreamReader($"Data{Path.DirectorySeparatorChar}{fileId}siege.csv"))
                {
                    while (!marbleList.EndOfStream)
                    {
                        var line = await marbleList.ReadLineAsync();
                        if (string.Compare(line.Split(',')[0], marbleToRemove, true) == 0)
                        {
                            if (ulong.Parse(line.Split(',')[1]) == Context.User.Id)
                                state = 2;
                            else
                            {
                                wholeFile.AppendLine(line);
                                if (!(state == 2)) state = 1;
                            }
                        }
                        else wholeFile.AppendLine(line);
                    }
                }
                switch (state)
                {
                    case 0: await ReplyAsync("Could not find the requested marble!"); break;
                    case 1: await ReplyAsync("This is not your marble!"); break;
                    case 2:
                        using (var marbleList = new StreamWriter($"Data{Path.DirectorySeparatorChar}{fileId}siege.csv", false))
                        {
                            await marbleList.WriteAsync(wholeFile.ToString());
                            await ReplyAsync($"Removed contestant **{marbleToRemove}**!");
                        }
                        break;
                    case 3: goto case 2;
                }
            }

            [Command("info")]
            [Summary("Shows information about the Siege.")]
            [RequireSlowmode]
            public async Task SiegeInfoCommandAsync()
            {
                ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                var marbles = new StringBuilder();
                var builder = new EmbedBuilder()
                    .WithColor(GetColor(Context))
                    .WithCurrentTimestamp()
                    .WithTitle("Siege Info");
                if (SiegeInfo.ContainsKey(fileId) && SiegeInfo[fileId].Active)
                {
                    var siege = SiegeInfo[fileId];
                    foreach (var marble in siege.Marbles)
                        marbles.AppendLine(marble.ToString(Context));
                    var PU = siege.PowerUp.IsEmpty() ? "None" : siege.PowerUp;
                    builder.AddField($"Boss: **{siege.Boss.Name}**", $"\nHP: **{siege.Boss.HP}**/{siege.Boss.MaxHP}\nAttacks: **{siege.Boss.Attacks.Length}**\nDifficulty: **{Enum.GetName(typeof(Difficulty), siege.Boss.Difficulty)}**");
                    if (marbles.Length > 1024) builder.AddField($"Marbles: **{siege.Marbles.Count}**", string.Concat(marbles.ToString().Take(1024)))
                        .AddField("Marbles (cont.)", string.Concat(marbles.ToString().Skip(1024)));
                    else builder.AddField($"Marbles: **{siege.Marbles.Count}**", marbles.ToString());
                    builder.WithDescription($"Damage Multiplier: **{siege.DamageMultiplier}**\nActive Power-up: **{PU}**")
                         .WithThumbnailUrl(siege.Boss.ImageUrl);
                }
                else
                {
                    using (var marbleList = new StreamReader($"Data{Path.DirectorySeparatorChar}{fileId}siege.csv"))
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
                                    marbles.AppendLine($"**{mSplit[0]}** [{user.Username}#{user.Discriminator}]");
                                }
                            }
                        }
                        else marbles.Append("No contestants have signed up!");
                    }
                    builder.AddField("Marbles", marbles.ToString());
                    builder.WithDescription("Siege not started yet.");
                }
                await ReplyAsync(embed: builder.Build());
            }

            [Command("leaderboard")]
            [Alias("leaderboard mostused")]
            [Summary("Shows a leaderboard of most used marbles in Sieges.")]
            public async Task SiegeLeaderboardCommandAsync(string rawNo = "1")
            {
                if (int.TryParse(rawNo, out int no))
                {
                    var winners = new SortedDictionary<string, int>();
                    using (var win = new StreamReader("SiegeMostUsed.txt"))
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
                        .WithTitle("Siege Leaderboard: Most Used")
                        .Build());
                }
                else await ReplyAsync("This is not a valid number! Format: `mb/siege leaderboard <optional number>`");
            }

            [Command("boss")]
            [Alias("bossinfo")]
            [Summary("Returns information about a boss.")]
            public async Task SiegeBossCommandAsync([Remainder] string searchTerm)
            {
                var boss = Boss.Empty;
                var state = 1;
                switch (searchTerm.ToLower().RemoveChar(' '))
                {
                    case "pree":
                    case "preethetree": boss = Siege.GetBoss("PreeTheTree"); break;
                    case "hattmann": boss = Siege.GetBoss("HattMann"); break;
                    case "orange": boss = Siege.GetBoss("Orange"); break;
                    case "green": boss = Siege.GetBoss("Green"); break;
                    case "destroyer": boss = Siege.GetBoss("Destroyer"); break;
                    case "helpme":
                    case "helpmethetree": boss = Siege.GetBoss("HelpMeTheTree"); break;
                    case "erango": boss = Siege.GetBoss("Erango"); break;
                    case "octopheesh": boss = Siege.GetBoss("Octopheesh"); break;
                    case "frigidium":
                    case "highwaystickman":
                    case "outcast":
                    case "doon":
                    case "shardberg":
                    case "iceelemental":
                    case "snowjoke":
                    case "pheesh":
                    case "shark":
                    case "pufferfish":
                    case "neptune":
                    case "lavachunk":
                    case "pyromaniac":
                    case "volcano": await ReplyAsync("No."); state = 3; break;
                    case "red": boss = Siege.GetBoss("Red"); break;
                    case "spaceman":
                    case "rgvzdhjvewvy":
                    case "corruptsoldier": goto case "volcano";
                    case "corruptpurple": boss = Siege.GetBoss("CorruptPurple"); break;
                    case "chest": boss = Siege.GetBoss("Chest"); break;
                    case "scaryface": boss = Siege.GetBoss("ScaryFace"); break;
                    case "marblebot":
                    case "marblebotprototype": boss = Siege.GetBoss("MarbleBotPrototype"); break;
                    case "overlord": boss = Siege.GetBoss("Overlord"); break;
                    case "vinemonster":
                    case "vinemonsters":
                    case "floatingore": await ReplyAsync("Excuse me?"); state = 3; break;
                    case "doc671": await ReplyAsync("No spoilers here!"); state = 4; break;
                    default: state = 0; break;
                }
                if (state == 1)
                {
                    if (boss.Stage > GetUser(Context).Stage) await ReplyAsync(embed: new EmbedBuilder()
                        .WithColor(GetColor(Context))
                        .WithCurrentTimestamp()
                        .WithDescription(StageTooHighString())
                        .WithThumbnailUrl(boss.ImageUrl)
                        .WithTitle(boss.Name)
                        .Build());
                    else
                    {
                        var attacks = new StringBuilder();
                        foreach (var attack in boss.Attacks)
                            attacks.AppendLine($"**{attack.Name}** (Accuracy: {attack.Accuracy}%) [Damage: {attack.Damage}] <MSE: {Enum.GetName(typeof(MSE), attack.StatusEffect)}>");
                        var drops = new StringBuilder();
                        foreach (var drop in boss.Drops)
                        {
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
                    }
                }
                else if (state == 0) await ReplyAsync("Could not find the requested boss!");
            }

            [Command("bosschance")]
            [Alias("spawnchance", "boss chance", "spawn chance", "chance")]
            [Summary("Displays the spawn chances of each boss.")]
            public async Task SiegeBossChanceCommandAsync([Remainder] string option)
            {
                var builder = new EmbedBuilder()
                    .WithColor(GetColor(Context))
                    .WithCurrentTimestamp();
                switch (option.ToLower().RemoveChar(' '))
                {
                    case "graph1":
                    case "1":
                        await ReplyAsync(embed: builder
                            .WithImageUrl("https://cdn.discordapp.com/attachments/229280519697727488/572121299485327360/unknown.png")
                            .WithTitle("Siege Boss Spawn Chances: Stage I (Graph)")
                            .Build());
                        break;
                    case "graph2":
                    case "2":
                        await ReplyAsync(embed: builder
                            .WithImageUrl("https://cdn.discordapp.com/attachments/296376584238137355/574317268767604757/unknown.png")
                            .WithTitle("Siege Boss Spawn Chances: Stage II (Graph)")
                            .Build());
                        break;
                    case "raw1":
                        await ReplyAsync(embed: builder
                            .WithImageUrl("https://cdn.discordapp.com/attachments/229280519697727488/572121173828173844/unknown.png")
                            .WithTitle("Siege Boss Spawn Chances: Stage I (Raw)")
                            .Build());
                        break;
                    case "raw2":
                        await ReplyAsync(embed: builder
                            .WithImageUrl("https://cdn.discordapp.com/attachments/296376584238137355/574317022679138320/unknown.png")
                            .WithTitle("Siege Boss Spawn Chances: Stage II (Raw)")
                            .Build());
                        break;
                }
            }

            [Command("bosslist")]
            [Alias("bosses")]
            [Summary("Returns a list of bosses.")]
            public async Task SiegeBosslistCommandAsync(string rawStage = "1")
            {
                if (int.TryParse(rawStage, out int stage) && (stage == 1 || stage == 2))
                {
                    var builder = new EmbedBuilder()
                        .WithColor(GetColor(Context))
                        .WithCurrentTimestamp();
                    string json;
                    using (var bosses = new StreamReader($"Resources{Path.DirectorySeparatorChar}Bosses.json")) json = bosses.ReadToEnd();
                    var playableBosses = JObject.Parse(json).ToObject<Dictionary<string, Boss>>();
                    var userStage = GetUser(Context).Stage;
                    foreach (var bossPair in playableBosses)
                    {
                        if (bossPair.Value.Stage == stage)
                        {
                            var difficulty = bossPair.Value.Stage > userStage ? StageTooHighString() :
                                $"Difficulty: **{Enum.GetName(typeof(Difficulty), bossPair.Value.Difficulty)} {(int)bossPair.Value.Difficulty}**/10, HP: **{bossPair.Value.MaxHP}**, Attacks: **{bossPair.Value.Attacks.Count()}**";
                            builder.AddField($"{bossPair.Value.Name}", difficulty);
                        }
                    }
                    builder.WithDescription("Use `mb/siege boss <boss name>` for more info!")
                        .WithTitle($"Playable MS Bosses: Stage {rawStage}");
                    await ReplyAsync(embed: builder.Build());
                }
                else await ReplyAsync("Invalid stage number!");
            }

            [Command("powerup")]
            [Alias("power-up", "powerupinfo", "power-upinfo", "puinfo")]
            [Summary("Returns information about a power-up.")]
            public async Task SiegePowerupCommandAsync(string searchTerm)
            {
                var powerup = "";
                var desc = "";
                var url = "";
                switch (searchTerm.ToLower().RemoveChar(' '))
                {
                    case "clone":
                        powerup = "Clone";
                        desc = "Spawns five clones of a marble which all attack with the marble then die.";
                        url = "https://cdn.discordapp.com/attachments/296376584238137355/541373091495018496/PUClone.png";
                        break;
                    case "moraleboost":
                        powerup = "Morale Boost";
                        desc = "Doubles the Damage Multiplier for 20 seconds.";
                        url = "https://cdn.discordapp.com/attachments/296376584238137355/541373099090903070/PUMoraleBoost.png";
                        break;
                    case "summon":
                        powerup = "Summon";
                        desc = "Summons an ally to help against the boss.";
                        url = "https://cdn.discordapp.com/attachments/296376584238137355/541373120129531939/PUSummon.png";
                        break;
                    case "cure":
                        powerup = "Cure";
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
                switch (option)
                {
                    case "enable":
                    case "true":
                    case "on": user.SiegePing = true; break;
                    case "disable":
                    case "false":
                    case "off": user.SiegePing = false; break;
                    default: user.SiegePing = !user.SiegePing; break;
                }
                obj.Remove(Context.User.Id.ToString());
                obj.Add(new JProperty(Context.User.Id.ToString(), JObject.FromObject(user)));
                WriteUsers(obj);
                if (user.SiegePing) await ReplyAsync($"**{Context.User.Username}**, you will now be pinged when a Siege that you are in starts.\n(type `mb/siege ping` to turn off)");
                else await ReplyAsync($"**{Context.User.Username}**, you will no longer be pinged when a Siege that you are in starts.\n(type `mb/siege ping` to turn on)");
            }

            public void StageOneBossChooser(Siege currentSiege)
            {
                var bossWeight = (int)Math.Round(currentSiege.Marbles.Count * ((Rand.NextDouble() * 5) + 1));
                if (IsBetween(bossWeight, 0, 6)) currentSiege.Boss = Siege.GetBoss("PreeTheTree");
                else if (IsBetween(bossWeight, 7, 13)) currentSiege.Boss = Siege.GetBoss("HelpMeTheTree");
                else if (IsBetween(bossWeight, 14, 21)) currentSiege.Boss = Siege.GetBoss("HattMann");
                else if (IsBetween(bossWeight, 22, 29)) currentSiege.Boss = Siege.GetBoss("Orange");
                else if (IsBetween(bossWeight, 30, 37)) currentSiege.Boss = Siege.GetBoss("Erango");
                else if (IsBetween(bossWeight, 38, 45)) currentSiege.Boss = Siege.GetBoss("Octopheesh");
                else if (IsBetween(bossWeight, 46, 53)) currentSiege.Boss = Siege.GetBoss("Green");
                else currentSiege.Boss = Siege.GetBoss("Destroyer");
            }

            public void StageTwoBossChooser(Siege currentSiege)
            {
                var bossWeight = (int)Math.Round(currentSiege.Marbles.Count * ((Rand.NextDouble() * 5) + 1));
                if (IsBetween(bossWeight, 0, 9)) currentSiege.Boss = Siege.GetBoss("Chest");
                else if (IsBetween(bossWeight, 10, 18)) currentSiege.Boss = Siege.GetBoss("ScaryFace");
                else if (IsBetween(bossWeight, 19, 27)) currentSiege.Boss = Siege.GetBoss("Red");
                else if (IsBetween(bossWeight, 28, 36)) currentSiege.Boss = Siege.GetBoss("CorruptPurple");
                else if (IsBetween(bossWeight, 37, 45)) currentSiege.Boss = Siege.GetBoss("MarbleBotPrototype");
                else currentSiege.Boss = Siege.GetBoss("Overlord");
            }

            [Command("")]
            [Alias("help")]
            [Priority(-1)]
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
                        .AppendLine("There are a few differences between this and normal Marble Sieges:")
                        .AppendLine("- **HP Scaling**: Marble HP scales with difficulty ((difficulty + 2) * 5).")
                        .AppendLine("- **Vengeance**: When a marble dies, the damage multiplier goes up by 0.2 (0.4 if Morale Boost is active).")
                        .ToString())
                    .AddField("More info", "For more information, visit https://github.com/doc671/MarbleBot/wiki/Marble-Siege.")
                    .WithColor(GetColor(Context))
                    .WithCurrentTimestamp()
                    .WithTitle("Marble Siege!")
                    .Build());
        }
    }
}
