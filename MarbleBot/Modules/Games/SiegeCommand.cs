using Discord;
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
                        .AppendLine("There are a few differences between this and normal Marble Sieges.\n")
                        .AppendLine("**HP Scaling**\nMarble HP scales with difficulty ((difficulty + 2) * 5).\n")
                        .AppendLine("**Vengeance**\nWhen a marble dies, the damage multiplier goes up by 0.2 (0.4 if Morale Boost is active).")
                        .ToString())
                    .WithColor(GetColor(Context))
                    .WithCurrentTimestamp()
                    .WithTitle("Marble Siege!")
                    .Build());

            [Command("signup")]
            [Alias("join")]
            [Summary("Sign up to the Marble Siege!")]
            public async Task SiegeSignupCommandAsync([Remainder] string marbleName = "")
            {
                await Context.Channel.TriggerTypingAsync();
                ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
                EmbedBuilder builder = new EmbedBuilder()
                    .WithColor(GetColor(Context))
                    .WithCurrentTimestamp();

                string name;
                if (marbleName.IsEmpty() || marbleName.Contains("@")) name = Context.User.Username;
                else if (marbleName.Length > 100){
                    await ReplyAsync("Your entry exceeds the 100 character limit.");
                    return;
                } else if (Global.SiegeInfo.ContainsKey(fileId)) {
                    if (Global.SiegeInfo[fileId].Active) {
                        await ReplyAsync("A battle is currently ongoing!");
                        return;
                    }
                }
                if (!File.Exists($"{fileId}siege.csv")) File.Create($"{fileId}siege.csv").Close();
                var found = false;
                using (var marbleList = new StreamReader($"{fileId}siege.csv")) {
                    while (!marbleList.EndOfStream) {
                        var line = await marbleList.ReadLineAsync();
                        found = line.Contains(Context.User.Id.ToString());
                    }
                }
                if (found) {
                    await ReplyAsync("You've already joined!");
                    return;
                }
                marbleName = marbleName.Replace("\n", " ").Replace(",", ";");
                name = marbleName;
                builder.AddField("Marble Siege: Signed up!", "**" + Context.User.Username + "** has successfully signed up as **" + name + "**!");
                using (var siegers = new StreamWriter("SiegeMostUsed.txt", true)) await siegers.WriteLineAsync(name);
                using (var marbleList = new StreamWriter($"{fileId}siege.csv", true)) {
                    await marbleList.WriteLineAsync($"{name},{Context.User.Id}");
                    marbleList.Close();
                }
                byte alive = 0;
                using (var marbleList = new StreamReader($"{fileId}siege.csv")) {
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

                if (Global.SiegeInfo.ContainsKey(fileId)) {
                    if (Global.SiegeInfo[fileId].Active)
                        await ReplyAsync("A battle is currently ongoing!");
                } else {
                    // Get marbles
                    byte marbleCount = 0;
                    using (var marbleList = new StreamReader($"{fileId}siege.csv")) {
                        while (!marbleList.EndOfStream) {
                            var line = await marbleList.ReadLineAsync();
                            if (!line.IsEmpty()) marbleCount++;
                            var sLine = line.Split(',');
                            var marble = new Marble() {
                                Id = ulong.Parse(sLine[1]),
                                Name = sLine[0]
                            };
                            var user = GetUser(Context, marble.Id);
                            if (user.Items.ContainsKey(063) && user.Items[063] >= 1)
                                marble.Shield = GetItem("063");
                            if (user.Items.ContainsKey(074)) marble.DamageIncrease = 25;
                            else if (user.Items.ContainsKey(071)) marble.DamageIncrease = 15;
                            else if (user.Items.ContainsKey(066)) marble.DamageIncrease = 10;
                            if (Global.SiegeInfo.ContainsKey(fileId)) {
                                if (!Global.SiegeInfo[fileId].Marbles.Contains(marble)) Global.SiegeInfo[fileId].Marbles.Add(marble);
                            }
                            else Global.SiegeInfo.Add(fileId, new Siege(new Marble[] { marble }));
                        }
                    }
                    if (marbleCount == 0) await ReplyAsync("It doesn't look like anyone has signed up!");
                    else {
                        var currentSiege = Global.SiegeInfo[fileId];
                        currentSiege.Active = true;
                        // Pick boss & set battle stats based on boss
                        var boss = currentSiege.Boss;
                        if (over.Contains("override") && (Context.User.Id == 224267581370925056 || Context.IsPrivate)) {
                            boss = Siege.GetBoss(over.Split(' ')[1]);
                        } else {
                            byte stageTotal = 0;
                            foreach (var marble in currentSiege.Marbles) {
                                var user = GetUser(Context, marble.Id);
                                stageTotal += user.Stage;
                            }
                            float stage = stageTotal / currentSiege.Marbles.Count;
                            if (stage == 1f) StageOneBossChooser(currentSiege);
                            else if (stage == 2f) StageTwoBossChooser(currentSiege);
                            else {
                                stage--;
                                if (Global.Rand.NextDouble() > stage) StageTwoBossChooser(currentSiege);
                                else StageOneBossChooser(currentSiege);
                            }
                        }
                        var hp = ((int)boss.Difficulty + 2) * 5;
                        foreach (var marble in currentSiege.Marbles) {
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
                        currentSiege.Actions = Task.Run(async () => { await currentSiege.SiegeBossActionsAsync(Context, fileId); });
                        var marbles = new StringBuilder();
                        var pings = new StringBuilder();
                        foreach (var marble in currentSiege.Marbles) {
                            marbles.AppendLine($"**{marble.Name}** [{Context.Client.GetUser(marble.Id).Username}#{Context.Client.GetUser(marble.Id).Discriminator}]");
                            if (GetUser(Context, marble.Id).SiegePing) pings.Append($"<@{marble.Id}> ");
                        }
                        await ReplyAsync(embed: new EmbedBuilder()
                            .WithColor(GetColor(Context))
                            .WithCurrentTimestamp()
                            .WithDescription("Get ready! Use `mb/siege attack` to attack and `mb/siege grab` to grab power-ups when they appear!")
                            .WithTitle("The Siege has begun!")
                            .WithThumbnailUrl(boss.ImageUrl)
                            .AddField($"Marbles: **{currentSiege.Marbles.Count}**", marbles.ToString())
                            .AddField($"Boss: **{boss.Name}**", new StringBuilder()
                                .AppendLine($"HP: **{boss.HP}**")
                                .AppendLine($"Attacks: **{boss.Attacks.Length}**")
                                .AppendLine($"Difficulty: **{Enum.GetName(typeof(Difficulty), boss.Difficulty)} {(int)boss.Difficulty}**/10")
                                .ToString())
                            .Build());
                        if (pings.Length != 0) await ReplyAsync(pings.ToString());
                    }
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
                                dmg = marble.StatusEffect == MSE.Chill ? (int)Math.Round(dmg * Global.SiegeInfo[fileId].DamageMultiplier * 0.5 * (marble.DamageIncrease / 100.0 + 1))
                                    : (int)Math.Round(dmg * Global.SiegeInfo[fileId].DamageMultiplier * (marble.DamageIncrease / 100.0 + 1));
                                var clone = false;
                                var userMarble = Global.SiegeInfo[fileId].Marbles.Find(m => m.Id == Context.User.Id);
                                if (marble.Cloned) {
                                    clone = true;
                                    await Global.SiegeInfo[fileId].DealDamageAsync(Context, dmg * 5);
                                    builder.AddField("Clones attack!", $"Each of the clones dealt **{dmg}** damage to the boss!");
                                    userMarble.Cloned = false;
                                }
                                await Global.SiegeInfo[fileId].DealDamageAsync(Context, dmg);
                                userMarble.DamageDealt += dmg;
                                builder.WithTitle(title)
                                    .WithThumbnailUrl(url)
                                    .WithDescription($"**{userMarble.Name}** dealt **{dmg}** damage to **{Global.SiegeInfo[fileId].Boss.Name}**!")
                                    .AddField("Boss HP", $"**{Global.SiegeInfo[fileId].Boss.HP}**/{Global.SiegeInfo[fileId].Boss.MaxHP}");
                                await ReplyAsync(embed: builder.Build());
                                if (clone && marble.Name[marble.Name.Length - 1] != 's')
                                    await ReplyAsync($"{marble.Name}'s clones disappeared!");
                                else if (clone) await ReplyAsync($"{marble.Name}' clones disappeared!");

                                if (Global.SiegeInfo[fileId].Boss.HP < 1) await Global.SiegeInfo[fileId].SiegeVictoryAsync(Context, fileId);

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
                if (Context.User.Id == 224267581370925056 || Context.IsPrivate) {
                    using (var marbleList = new StreamWriter($"{fileId}siege.csv", false)) {
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
                using (var marbleList = new StreamReader($"{fileId}siege.csv")) {
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
                using (var marbleList = new StreamReader($"{fileId}siege.csv")) {
                    while (!marbleList.EndOfStream) {
                        var line = await marbleList.ReadLineAsync();
                        if (line.Split(',')[0] == marbleToRemove) {
                            if (ulong.Parse(line.Split(',')[1]) == Context.User.Id) {
                                state = 2;
                            } else {
                                wholeFile.AppendLine(line);
                                if (!(state == 2)) state = 1;
                            }
                        }
                        else wholeFile.AppendLine(line);
                    }
                }
                switch (state) {
                    case 0: await ReplyAsync("Could not find the requested marble!"); break;
                    case 1: await ReplyAsync("This is not your marble!"); break;
                    case 2:
                        using (var marbleList = new StreamWriter($"{fileId}siege.csv", false)) {
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
                    using (var marbleList = new StreamReader($"{fileId}siege.csv")){
                        var allMarbles = (await marbleList.ReadToEndAsync()).Split('\n');
                        if (allMarbles.Length > 1) {
                            foreach (var marble in allMarbles) {
                                if (marble.Length > 16) {
                                    var mSplit = marble.Split(',');
                                    var user = Context.Client.GetUser(ulong.Parse(mSplit[1].Trim('\n')));
                                    marbles.AppendLine($"**{mSplit[0].Trim('\n')}** [{user.Username}#{user.Discriminator}]");
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
                    using (var win = new StreamReader("SiegeMostUsed.txt")) {
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
                    await ReplyAsync(embed: new EmbedBuilder()
                        .WithColor(GetColor(Context))
                        .WithCurrentTimestamp()
                        .WithDescription(Global.Leaderboard(winList, no))
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
                switch (searchTerm.ToLower().RemoveChar(' ')) {
                    case "pree":
                    case "preethetree": boss = Siege.GetBoss("PreeTheTree"); break;
                    case "hattmann": boss = Siege.GetBoss("HattMann");  break;
                    case "orange": boss = Siege.GetBoss("Orange"); break;
                    case "green": boss = Siege.GetBoss("Green"); break;
                    case "destroyer": boss = Siege.GetBoss("Destroyer"); break;
                    case "helpme":
                    case "helpmethetree": boss = Siege.GetBoss("HelpMeTheTree"); break;
                    case "erango": boss = Siege.GetBoss("Erango"); break;
                    case "octopheesh": boss = Siege.GetBoss("Octopheesh"); break;
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
                    case "red": Siege.GetBoss("Red"); break;
                    case "spaceman": goto case "frigidium";
                    case "rgvzdhjvewvy": goto case "frigidium";
                    case "corruptsoldier": goto case "frigidium";
                    case "corruptpurple": Siege.GetBoss("CorruptPurple"); break;
                    case "chest": goto case "frigidium";
                    case "scaryface": goto case "frigidium";
                    case "marblebot": goto case "frigidium";
                    case "overlord": Siege.GetBoss("Overlord"); break;
                    case "vinemonster": await ReplyAsync("Excuse me?"); state = 3; break;
                    case "vinemonsters": goto case "vinemonster";
                    case "floatingore": goto case "vinemonster";
                    case "doc671": await ReplyAsync("No spoilers here!"); state = 4; break;
                    default: state = 0; break;
                }
                if (state == 1) {
                    if (boss.Stage > GetUser(Context).Stage) await ReplyAsync(embed: new EmbedBuilder()
                        .WithColor(GetColor(Context))
                        .WithCurrentTimestamp()
                        .WithDescription(StageTooHighString())
                        .WithThumbnailUrl(boss.ImageUrl)
                        .WithTitle(boss.Name)
                        .Build());
                    else {  
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
                    }
                }
                else if (state == 0) await ReplyAsync("Could not find the requested boss!");
            }
                
            [Command("bosslist")]
            [Summary("Returns a list of bosses.")]
            public async Task SiegeBosslistCommandAsync()
            {
                var builder = new EmbedBuilder()
                    .WithColor(GetColor(Context))
                    .WithCurrentTimestamp();
                string json;
                using (var bosses = new StreamReader("Resources\\Bosses.json")) json = bosses.ReadToEnd();
                var playableBosses = JObject.Parse(json).ToObject<Dictionary<string, Boss>>();
                var stage = GetUser(Context).Stage;
                foreach (var bossPair in playableBosses) {
                    var difficulty = bossPair.Value.Stage > stage ? StageTooHighString() : 
                        $"Difficulty: **{Enum.GetName(typeof(Difficulty), bossPair.Value.Difficulty)} {(int)bossPair.Value.Difficulty}**/10, HP: **{bossPair.Value.MaxHP}**, Attacks: **{bossPair.Value.Attacks.Count()}**";
                    builder.AddField($"{bossPair.Value.Name}", difficulty);
                }
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

            public void StageOneBossChooser(Siege currentSiege) {
                var bossWeight = (int)Math.Round(currentSiege.Marbles.Count * ((Global.Rand.NextDouble() * 5) + 1));
                if (IsBetween(bossWeight, 0, 5)) currentSiege.Boss = Siege.GetBoss("PreeTheTree");
                else if (IsBetween(bossWeight, 6, 13)) currentSiege.Boss = Siege.GetBoss("HelpMeTheTree");
                else if (IsBetween(bossWeight, 14, 21)) currentSiege.Boss = Siege.GetBoss("HattMann");
                else if (IsBetween(bossWeight, 22, 29)) currentSiege.Boss = Siege.GetBoss("Orange");
                else if (IsBetween(bossWeight, 30, 37)) currentSiege.Boss = Siege.GetBoss("Erango");
                else if (IsBetween(bossWeight, 38, 45)) currentSiege.Boss = Siege.GetBoss("Octopheesh");
                else if (IsBetween(bossWeight, 46, 53)) currentSiege.Boss = Siege.GetBoss("Green");
                else currentSiege.Boss = Siege.GetBoss("Destroyer");
            }

            public void StageTwoBossChooser(Siege currentSiege) {
                var bossWeight = (int)Math.Round(currentSiege.Marbles.Count * ((Global.Rand.NextDouble() * 5) + 1));
                if (IsBetween(bossWeight, 0, 25)) currentSiege.Boss = Siege.GetBoss("Red");
                else if (IsBetween(bossWeight, 25, 50)) currentSiege.Boss = Siege.GetBoss("CorruptPurple");
                else currentSiege.Boss = Siege.GetBoss("Overlord");
            }
        }
    }
}
