﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MarbleBot.Common;
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
    [Group("siege")]
    [Summary("Participate in a Marble Siege boss battle!")]
    [Remarks("Requires a channel in which slowmode is enabled.")]
    public class SiegeCommand : GameModule
    {
        private const GameType Type = GameType.Siege;

        public SiegeCommand(BotCredentials botCredentials, GamesService gamesService, RandomService randomService) : base(botCredentials, gamesService, randomService)
        {
        }

        [Command("signup")]
        [Alias("join")]
        [Summary("Sign up to the Marble Siege!")]
        public async Task SiegeSignupCommand([Remainder] string marbleName = "")
        => await Signup(Type, marbleName, 20, async () => { await SiegeStartCommand(); });

        [Command("start")]
        [Alias("begin")]
        [Summary("Starts the Marble Siege Battle.")]
        public async Task SiegeStartCommand([Remainder] string overrideString = "")
        {
            ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;

            if (_gamesService.Sieges.ContainsKey(fileId) && _gamesService.Sieges[fileId].Active)
            {
                await SendErrorAsync("A battle is currently ongoing!");
                return;
            }

            if (!File.Exists($"Data{Path.DirectorySeparatorChar}{fileId}.siege"))
            {
                await SendErrorAsync($"**{Context.User.Username}**, no-one is signed up!");
                return;
            }

            // Get marbles
            int marbleCount = 0;
            var rawMarbles = new List<(ulong id, string name)>();
            var formatter = new BinaryFormatter();
            using (var marbleList = new StreamReader($"Data{Path.DirectorySeparatorChar}{fileId}.siege"))
            {
                if (marbleList.BaseStream.Length == 0)
                {
                    await SendErrorAsync($"**{Context.User.Username}**, no-one is signed up!");
                    return;
                }
                rawMarbles = (List<(ulong id, string name)>)formatter.Deserialize(marbleList.BaseStream);
                marbleCount = rawMarbles.Count;
            }

            if (marbleCount == 0)
            {
                await SendErrorAsync($"**{Context.User.Username}**, no-one is signed up!");
                return;
            }

            MarbleBotUser user;
            var marbles = new List<SiegeMarble>();
            int stageTotal = 0;
            foreach (var (id, name) in rawMarbles)
            {
                user = MarbleBotUser.Find(Context, id);
                stageTotal += user.Stage;

                marbles.Add(new SiegeMarble(id, name, 0)
                {
                    Shield = user.GetShield(),
                    Spikes = user.GetSpikes()
                });
            }

            // Pick boss & set battle stats based on boss
            Boss boss;
            if (overrideString.Contains("override") && (_botCredentials.AdminIds.Any(id => id == Context.User.Id) || Context.IsPrivate))
            {
                boss = Boss.GetBoss(overrideString.Split(' ')[1].RemoveChar(' '));
            }
            else
            {
                // Choose a stage 1 or stage 2 boss depending on the stage of each participant
                float stage = stageTotal / (float)marbleCount;
                if (stage == 1f)
                {
                    boss = ChooseStageOneBoss(marbles);
                }
                else if (stage == 2f)
                {
                    boss = ChooseStageTwoBoss(marbles);
                }
                else
                {
                    stage--;
                    if (_randomService.Rand.NextDouble() < stage)
                    {
                        boss = ChooseStageTwoBoss(marbles);
                    }
                    else
                    {
                        boss = ChooseStageOneBoss(marbles);
                    }
                }
            }

            int marbleHealth = ((int)boss.Difficulty + 2) * 5;
            var marbleOutput = new StringBuilder();
            var mentionOutput = new StringBuilder();
            SocketUser socketUser;
            foreach (SiegeMarble marble in marbles)
            {
                marble.MaxHealth = marbleHealth;
                socketUser = Context.Client.GetUser(marble.Id);
                marbleOutput.AppendLine($"**{marble.Name}** [{socketUser.Username}#{socketUser.Discriminator}]");
                if (MarbleBotUser.Find(Context, marble.Id).SiegePing)
                {
                    mentionOutput.Append($"<@{marble.Id}> ");
                }
            }

            Siege currentSiege = _gamesService.Sieges.GetOrAdd(fileId, new Siege(Context, _gamesService, _randomService, marbles));
            currentSiege.Boss = boss;

            var builder = new EmbedBuilder()
                .WithColor(GetColor(Context))
                .WithCurrentTimestamp()
                .WithDescription("Get ready! Use `mb/siege attack` to attack and `mb/siege grab` to grab power-ups when they appear!")
                .WithTitle("The Siege has begun! :crossed_swords:")
                .WithThumbnailUrl(boss.ImageUrl)
                .AddField($"Marbles: **{marbles.Count}**", marbleOutput.ToString())
                .AddField($"Boss: **{boss.Name}**", new StringBuilder()
                    .AppendLine($"Health: **{boss.Health}**")
                    .AppendLine($"Attacks: **{boss.Attacks.Length}**")
                    .AppendLine($"Difficulty: **{Enum.GetName(typeof(Difficulty), boss.Difficulty)} {(int)boss.Difficulty}**/10")
                    .ToString());

            // Siege Start
            var countdownMessage = await ReplyAsync("**3**");
            await Task.Delay(1000);
            await countdownMessage.ModifyAsync(m => m.Content = "**2**");
            await Task.Delay(1000);
            await countdownMessage.ModifyAsync(m => m.Content = "**1**");
            await Task.Delay(1000);
            await countdownMessage.ModifyAsync(m => m.Content = "**BEGIN THE SIEGE!**");

            await ReplyAsync(embed: builder.Build());

            currentSiege.Actions = Task.Run(async () => { await currentSiege.BossActions(); });

            if (mentionOutput.Length != 0 && _botCredentials.AdminIds.Any(id => id == Context.User.Id) && !overrideString.Contains("noping"))
            {
                await ReplyAsync(mentionOutput.ToString());
            }
        }

        [Command("stop")]
        [RequireOwner]
        public async Task SiegeStopCommand()
        {
            _gamesService.Sieges[Context.IsPrivate ? Context.User.Id : Context.Guild.Id].Dispose();
            await ReplyAsync("Siege successfully stopped.");
        }

        [Command("attack", RunMode = RunMode.Async)]
        [Alias("bonk")]
        [Summary("Attacks the boss.")]
        public async Task SiegeAttackCommand()
        {
            ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
            if (!_gamesService.Sieges.ContainsKey(fileId) || !_gamesService.Sieges[fileId].Active)
            {
                await SendErrorAsync("There is no currently ongoing Siege!");
                return;
            }

            var currentSiege = _gamesService.Sieges[fileId];
            var currentMarble = currentSiege.Marbles.Find(m => m.Id == Context.User.Id);
            if (currentMarble == null)
            {
                await SendErrorAsync($"**{Context.User.Username}**, you aren't in this Siege!");
                return;
            }

            if (currentMarble.Health == 0)
            {
                await SendErrorAsync($"**{Context.User.Username}**, you are out and can no longer attack!");
                return;

            }

            double totalSeconds = DateTime.UtcNow.Subtract(currentMarble.LastMoveUsed).TotalSeconds;
            if (totalSeconds < 5)
            {
                await SendErrorAsync($"**{Context.User.Username}**, you must wait for {GetDateString(5 - totalSeconds)} until you can act again!");
                return;
            }

            if (currentMarble.StatusEffect == StatusEffect.Stun)
            {
                if (DateTime.UtcNow.Subtract(currentMarble.LastStun).TotalSeconds > 15)
                {
                    currentMarble.StatusEffect = StatusEffect.None;
                }
                else
                {
                    await SendErrorAsync($"**{Context.User.Username}**, you are stunned and cannot attack!");
                    return;
                }
            }

            currentMarble.LastMoveUsed = DateTime.UtcNow;

            var builder = new EmbedBuilder()
                .WithColor(GetColor(Context))
                .WithCurrentTimestamp();

            if (currentSiege.Morales > 0 && DateTime.UtcNow.Subtract(currentSiege.LastMorale).TotalSeconds > 20)
            {
                currentSiege.Morales--;
                builder.AddField("Morale Boost has worn off!",
                    $"The effects of a Morale Boost power-up have worn off! The damage multiplier is now **{currentSiege.DamageMultiplier}**!");
            }

            int marbleDamage = _randomService.Rand.Next(1, 25);
            if (marbleDamage > 20)
            {
                marbleDamage = (int)MathF.Round(marbleDamage * 1.5f); // Critical attack
            }

            string title;
            string url;
            if (marbleDamage < 8)
            {
                title = "Slow attack! :boom:";
                url = "https://cdn.discordapp.com/attachments/296376584238137355/548217423623356418/SiegeAttackSlow.png";
            }
            else if (marbleDamage < 15)
            {
                title = "Fast attack! :boom:";
                url = "https://cdn.discordapp.com/attachments/296376584238137355/548217417847799808/SiegeAttackFast.png";
            }
            else if (marbleDamage < 21)
            {
                title = "Brutal attack! :boom:";
                url = "https://cdn.discordapp.com/attachments/296376584238137355/548217407337005067/SiegeAttackBrutal.png";
            }
            else
            {
                title = "CRITICAL attack! :boom:";
                url = "https://cdn.discordapp.com/attachments/296376584238137355/548217425359798274/SiegeAttackCritical.png";
            }
            int totalDamage = marbleDamage = (int)MathF.Round(marbleDamage * currentSiege.DamageMultiplier
                * (currentMarble.StatusEffect == StatusEffect.Chill ? 0.5f : 1f)
                * (currentMarble.DamageBoost / 100f + 1f));

            if (currentMarble.Cloned)
            {
                builder.AddField("Clones attack!", $"Each of the clones dealt **{marbleDamage}** damage to the boss! The clones then disappeared!");
                currentMarble.Cloned = false;
                totalDamage += marbleDamage * 5;
            }

            builder.WithTitle(title)
                .WithThumbnailUrl(url)
                .WithDescription($"**{currentMarble.Name}** dealt **{marbleDamage}** damage to **{currentSiege.Boss.Name}**!")
                .AddField("Boss Health", $"**{Math.Max(currentSiege.Boss.Health - totalDamage, 0)}**/{currentSiege.Boss.MaxHealth}");

            await ReplyAsync(embed: builder.Build());
            currentMarble.DamageDealt += marbleDamage;
            await currentSiege.DealDamageToBoss(totalDamage);
        }

        [Command("grab", RunMode = RunMode.Async)]
        [Summary("Has a 1/3 chance of activating the power-up.")]
        public async Task SiegeGrabCommand()
        {
            ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;

            if (!_gamesService.Sieges.ContainsKey(fileId) || !_gamesService.Sieges[fileId].Active)
            {
                await SendErrorAsync($"**{Context.User.Username}**, there is no currently ongoing Siege!");
                return;
            }

            var currentSiege = _gamesService.Sieges[fileId];
            if (currentSiege.PowerUp == PowerUp.None)
            {
                await SendErrorAsync($"**{Context.User.Username}**, there is no power-up to grab!");
                return;
            }

            var currentMarble = currentSiege.Marbles.Find(m => m.Id == Context.User.Id);
            if (currentMarble == null)
            {
                await SendErrorAsync($"**{Context.User.Username}**, you aren't in this Siege!");
                return;
            }

            if (currentMarble.Health == 0)
            {
                await SendErrorAsync($"**{Context.User.Username}**, you are out and can no longer grab power-ups!");
                return;
            }

            double totalSeconds = DateTime.UtcNow.Subtract(currentMarble.LastMoveUsed).TotalSeconds;
            if (totalSeconds < 5)
            {
                await SendErrorAsync($"**{Context.User.Username}**, you must wait for {GetDateString(5 - totalSeconds):n2} until you can act again!");
                return;
            }

            currentMarble.LastMoveUsed = DateTime.UtcNow;

            if (_randomService.Rand.Next(0, 3) == 0)
            {
                EmbedBuilder builder = new EmbedBuilder()
                    .WithColor(GetColor(Context))
                    .WithCurrentTimestamp()
                    .WithThumbnailUrl(Siege.GetPowerUpImageUrl(currentSiege.PowerUp));

                currentMarble.PowerUpHits++;
                switch (currentSiege.PowerUp)
                {
                    case PowerUp.Clone:
                        currentMarble.Cloned = true;
                        builder.WithDescription($"**{currentMarble.Name}** activated **Clone**! Five clones of {currentMarble.Name} appeared!");
                        break;
                    case PowerUp.Cure:
                        builder.WithTitle("Cured!")
                            .WithDescription($"**{currentMarble.Name}** has been cured of **{Enum.GetName(typeof(StatusEffect), currentMarble.StatusEffect)}**!");
                        currentMarble.StatusEffect = StatusEffect.None;
                        break;
                    case PowerUp.MoraleBoost:
                        currentSiege.Morales++;
                        builder.WithDescription($"**{currentMarble.Name}** activated **Morale Boost**! Damage multiplier increased to **{currentSiege.DamageMultiplier}**!");
                        currentSiege.LastMorale = DateTime.UtcNow;
                        break;
                    case PowerUp.Summon:
                        int choice = _randomService.Rand.Next(0, 2);
                        string ally;
                        string url;
                        switch (choice)
                        {
                            case 0: ally = "Frigidium"; url = "https://cdn.discordapp.com/attachments/296376584238137355/543745898690379816/Frigidium.png"; break;
                            case 1: ally = "Neptune"; url = "https://cdn.discordapp.com/attachments/296376584238137355/543745899591893012/Neptune.png"; break;
                            default: ally = "MarbleBot"; url = ""; break;
                        }
                        int damage = (int)MathF.Round(_randomService.Rand.Next(25, 30) * currentSiege.Boss.Stage * ((int)currentSiege.Boss.Difficulty / 2) * currentSiege.DamageMultiplier);
                        await currentSiege.DealDamageToBoss(damage);

                        builder.WithThumbnailUrl(url)
                            .AddField("Boss Health", $"**{currentSiege.Boss.Health}**/{currentSiege.Boss.MaxHealth}")
                            .WithDescription($"**{currentMarble.Name}** activated **Summon**! **{ally}** came into the arena and dealt **{damage}** damage to the boss!");
                        break;
                }
                currentSiege.PowerUp = PowerUp.None;
                await ReplyAsync(embed: builder.WithTitle("POWER-UP ACTIVATED! :arrow_double_up:")
                    .Build());
            }
            else
            {
                await SendErrorAsync("You failed to grab the power-up!");
            }
        }

        [Command("checkearn")]
        [Summary("Shows whether you can earn money from Sieges and if not, when.")]
        public async Task SiegeCheckearnCommand()
        => await Checkearn(Type);

        [Command("clear")]
        [Summary("Clears the list of contestants.")]
        public async Task SiegeClearCommand()
        => await Clear(Type);

        [Command("contestants")]
        [Alias("marbles", "participants")]
        [Summary("Shows a list of all the contestants in the Siege.")]
        public async Task SiegeContestantsCommand()
        => await ShowContestants(Type);

        [Command("remove")]
        [Summary("Removes a contestant from the contestant list.")]
        public async Task SiegeRemoveCommand([Remainder] string marbleToRemove)
        => await RemoveContestant(Type, marbleToRemove);

        [Command("info")]
        [Summary("Shows information about the Siege.")]
        public async Task SiegeInfoCommand()
        {
            ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
            var marbleOutput = new StringBuilder();
            var builder = new EmbedBuilder()
                .WithColor(GetColor(Context))
                .WithCurrentTimestamp()
                .WithTitle("Siege Info");
            if (_gamesService.Sieges.ContainsKey(fileId) && _gamesService.Sieges[fileId].Active)
            {
                var siege = _gamesService.Sieges[fileId];
                foreach (var marble in siege.Marbles)
                {
                    marbleOutput.AppendLine(marble.ToString(Context));
                }

                builder.AddField($"Boss: **{siege.Boss.Name}**", $"\nHealth: **{siege.Boss.Health}**/{siege.Boss.MaxHealth}\nAttacks: **{siege.Boss.Attacks.Length}**\nDifficulty: **{Enum.GetName(typeof(Difficulty), siege.Boss.Difficulty)}**");

                if (marbleOutput.Length > 1024)
                {
                    builder.AddField($"Marbles: **{siege.Marbles.Count}**", string.Concat(marbleOutput.ToString().Take(1024)));

                    for (int i = 1024; i < marbleOutput.Length; i += 1024)
                    {
                        builder.AddField("Marbles (cont.)", string.Concat(marbleOutput.ToString().Skip(i)));
                    }
                }
                else
                {
                    builder.AddField($"Marbles: **{siege.Marbles.Count}**", marbleOutput.ToString());
                }

                builder.WithDescription($"Damage Multiplier: **{siege.DamageMultiplier}**\nActive Power-up: **{Enum.GetName(typeof(PowerUp), siege.PowerUp)!.CamelToTitleCase()}**")
                     .WithThumbnailUrl(siege.Boss.ImageUrl);
            }
            else
            {
                if (_gamesService.Sieges.ContainsKey(fileId) && _gamesService.Sieges[fileId].Boss.Name != Boss.Empty.Name)
                {
                    var siege = _gamesService.Sieges[fileId];
                    builder.AddField($"Boss: **{siege.Boss.Name}**", $"\nHealth: **{siege.Boss.MaxHealth}**\nAttacks: **{siege.Boss.Attacks.Length}**\nDifficulty: **{Enum.GetName(typeof(Difficulty), siege.Boss.Difficulty)}**")
                        .WithThumbnailUrl(siege.Boss.ImageUrl);
                }

                if (!File.Exists($"Data{Path.DirectorySeparatorChar}{fileId}.siege"))
                {
                    marbleOutput.Append("No-one is signed up!");
                }

                using (var marbleListFile = new StreamReader($"Data{Path.DirectorySeparatorChar}{fileId}.siege"))
                {
                    if (marbleListFile.BaseStream.Length == 0)
                    {
                        marbleOutput.Append("No-one is signed up!");
                    }
                    else
                    {
                        var formatter = new BinaryFormatter();
                        var marbles = (List<(ulong id, string name)>)formatter.Deserialize(marbleListFile.BaseStream);

                        if (marbles.Count == 0)
                        {
                            marbleOutput.Append("No-one is signed up!");
                        }
                        else
                        {
                            string bold;
                            SocketUser user;
                            foreach (var (id, name) in marbles)
                            {
                                bold = name.Contains('*') || name.Contains('\\') ? "" : "**";
                                user = Context.Client.GetUser(id);
                                marbleOutput.AppendLine($"{bold}{name}{bold} [{user.Username}#{user.Discriminator}]");
                            }
                        }
                    }
                }
                builder.AddField("Marbles", marbleOutput.ToString());
                builder.WithDescription("Siege not started yet.");
            }
            await ReplyAsync(embed: builder.Build());
        }

        [Command("marbleinfo")]
        [Summary("Displays information about the current marble.")]
        [Alias("minfo")]
        public async Task MarbleInfoCommand(string? searchTerm = null)
        {
            ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
            if (!_gamesService.Sieges.ContainsKey(fileId))
            {
                await ReplyAsync($"**{Context.User.Username}**, could not find the requested marble!");
                return;
            }

            var currentMarble = searchTerm == null ? _gamesService.Sieges[fileId].Marbles.Find(m => m.Id == Context.User.Id)
                : _gamesService.Sieges[fileId].Marbles.Find(m => string.Compare(m.Name, searchTerm, true) == 0);
            if (currentMarble == null)
            {
                await ReplyAsync($"**{Context.User.Username}**, could not find the requested marble!");
                return;
            }

            await ReplyAsync(embed: new EmbedBuilder()
                .AddField("Health", $"**{currentMarble.Health}**/{currentMarble.MaxHealth}", true)
                .AddField("Status Effect", Enum.GetName(typeof(StatusEffect), currentMarble.StatusEffect), true)
                .AddField("Shield", currentMarble.Shield?.Name, true)
                .AddField("Damage Increase", $"{currentMarble.DamageBoost}%", true)
                .AddField("Damage Dealt", currentMarble.DamageDealt, true)
                .AddField("Power-up Hits", currentMarble.PowerUpHits, true)
                .AddField("Rocket Boots Used?", currentMarble.BootsUsed, true)
                .AddField("Qefpedun Charm Used?", currentMarble.QefpedunCharmUsed, true)
                .WithCurrentTimestamp()
                .WithColor(GetColor(Context))
                .WithTitle(currentMarble.Name)
                .Build());
        }

        [Command("leaderboard")]
        [Alias("leaderboard mostused")]
        [Summary("Shows a leaderboard of most used marbles in Sieges.")]
        public async Task SiegeLeaderboardCommand(int page = 1)
        {
            var winners = new SortedDictionary<string, int>();
            using (var leaderboardFile = new StreamReader($"Data{Path.DirectorySeparatorChar}SiegeMostUsed.txt"))
            {
                while (!leaderboardFile.EndOfStream)
                {
                    var racerInfo = (await leaderboardFile.ReadLineAsync())!;
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
            foreach (var winner in winners)
            {
                winList.Add((winner.Key, winner.Value));
            }

            winList = (from winner in winList orderby winner.value descending select winner).ToList();
            await ReplyAsync(embed: new EmbedBuilder()
                .WithColor(GetColor(Context))
                .WithCurrentTimestamp()
                .WithDescription(Leaderboard(winList, page))
                .WithTitle("Siege Leaderboard: Most Used")
                .Build());
        }

        [Command("boss")]
        [Alias("bossinfo")]
        [Summary("Returns information about a boss.")]
        public async Task SiegeBossCommand([Remainder] string searchTerm)
        {
            Boss boss;
            switch (searchTerm.ToLower().RemoveChar(' '))
            {
                case "pree":
                case "preethetree": boss = Boss.GetBoss("PreeTheTree"); break;
                case "hattmann": boss = Boss.GetBoss("HattMann"); break;
                case "orange": boss = Boss.GetBoss("Orange"); break;
                case "green": boss = Boss.GetBoss("Green"); break;
                case "destroyer": boss = Boss.GetBoss("Destroyer"); break;
                case "helpme":
                case "helpmethetree": boss = Boss.GetBoss("HelpMeTheTree"); break;
                case "erango": boss = Boss.GetBoss("Erango"); break;
                case "octopheesh": boss = Boss.GetBoss("Octopheesh"); break;
                case "red": boss = Boss.GetBoss("Red"); break;
                case "corruptpurple": boss = Boss.GetBoss("CorruptPurple"); break;
                case "chest": boss = Boss.GetBoss("Chest"); break;
                case "scaryface": boss = Boss.GetBoss("ScaryFace"); break;
                case "marblebot":
                case "marblebotprototype": boss = Boss.GetBoss("MarbleBotPrototype"); break;
                case "overlord": boss = Boss.GetBoss("Overlord"); break;
                case "rockgolem": boss = Boss.GetBoss("Rock Golem"); break;
                default:
                    await ReplyAsync("Could not find the requested boss!");
                    return;
            }
            if (boss.Stage > MarbleBotUser.Find(Context).Stage)
            {
                await ReplyAsync(embed: new EmbedBuilder()
                    .WithColor(GetColor(Context))
                    .WithCurrentTimestamp()
                    .WithDescription(StageTooHighString())
                    .WithThumbnailUrl(boss.ImageUrl)
                    .WithTitle(boss.Name)
                    .Build());
            }
            else
            {
                var attacks = new StringBuilder();
                foreach (var attack in boss.Attacks)
                {
                    attacks.AppendLine($"**{attack.Name}** (Accuracy: {attack.Accuracy}%) [Damage: {attack.Damage}] <MSE: {Enum.GetName(typeof(StatusEffect), attack.StatusEffect)}>");
                }

                var drops = new StringBuilder();
                foreach (var drop in boss.Drops)
                {
                    var dropAmount = drop.MinCount == drop.MaxCount ? drop.MinCount.ToString() : $"{drop.MinCount}-{drop.MaxCount}";
                    var idString = drop.ItemId.ToString("000");
                    drops.AppendLine($"`[{idString}]` **{Item.Find<Item>(idString).Name}**: {dropAmount} ({drop.Chance}%)");
                }
                await ReplyAsync(embed: new EmbedBuilder()
                    .AddField("Health", $"**{boss.MaxHealth}**")
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

        [Command("bosschance")]
        [Alias("spawnchance", "boss chance", "spawn chance", "chance")]
        [Summary("Displays the spawn chances of each boss.")]
        public async Task SiegeBossChanceCommand([Remainder] string option)
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
        public async Task SiegeBosslistCommand(int stage = 1)
        {
            if (stage != 1 && stage != 2)
            {
                await ReplyAsync("Invalid stage number!");
                return;
            }

            var builder = new EmbedBuilder()
                .WithColor(GetColor(Context))
                .WithCurrentTimestamp();
            string json;
            using (var bosses = new StreamReader($"Resources{Path.DirectorySeparatorChar}Bosses.json"))
            {
                json = bosses.ReadToEnd();
            }

            var playableBosses = Boss.GetBosses();
            var userStage = MarbleBotUser.Find(Context).Stage;
            foreach (var bossPair in playableBosses)
            {
                if (bossPair.Value.Stage == stage)
                {
                    var difficulty = bossPair.Value.Stage > userStage ? StageTooHighString() :
                        $"Difficulty: **{Enum.GetName(typeof(Difficulty), bossPair.Value.Difficulty)} {(int)bossPair.Value.Difficulty}**/10, Health: **{bossPair.Value.MaxHealth}**, Attacks: **{bossPair.Value.Attacks.Count()}**";
                    builder.AddField($"{bossPair.Value.Name}", difficulty);
                }
            }
            builder.WithDescription("Use `mb/siege boss <boss name>` for more info!")
                .WithTitle($"Playable MS Bosses: Stage {stage}");
            await ReplyAsync(embed: builder.Build());
        }

        [Command("powerup")]
        [Alias("power-up", "powerupinfo", "power-upinfo", "puinfo")]
        [Summary("Returns information about a power-up.")]
        public async Task SiegePowerupCommand(string searchTerm)
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

            if (string.IsNullOrEmpty(powerup))
            {
                await ReplyAsync("Could not find the requested power-up!");
                return;
            }

            await ReplyAsync(embed: new EmbedBuilder()
            .WithColor(GetColor(Context))
            .WithCurrentTimestamp()
            .WithDescription(desc)
            .WithThumbnailUrl(url)
            .WithTitle(powerup)
            .Build());
        }

        [Command("ping")]
        [Summary("Toggles whether you are pinged when a Siege that you are in starts.")]
        public async Task SiegePingCommand(string option = "")
        {
            var user = MarbleBotUser.Find(Context);
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

            MarbleBotUser.UpdateUser(user);
            if (user.SiegePing)
            {
                await ReplyAsync($"**{Context.User.Username}**, you will now be pinged when a Siege that you are in starts.\n(type `mb/siege ping` to turn off)");
            }
            else
            {
                await ReplyAsync($"**{Context.User.Username}**, you will no longer be pinged when a Siege that you are in starts.\n(type `mb/siege ping` to turn on)");
            }
        }

        public Boss ChooseStageOneBoss(IEnumerable<SiegeMarble> marbles)
        {
            var bossWeight = (int)Math.Round(marbles.Count() * ((_randomService.Rand.NextDouble() * 5) + 1));
            if (bossWeight < 7)
            {
                return Boss.GetBoss("PreeTheTree");
            }
            else if (bossWeight < 14)
            {
                return Boss.GetBoss("HelpMeTheTree");
            }
            else if (bossWeight < 22)
            {
                return Boss.GetBoss("HattMann");
            }
            else if (bossWeight < 30)
            {
                return Boss.GetBoss("Orange");
            }
            else if (bossWeight < 38)
            {
                return Boss.GetBoss("Erango");
            }
            else if (bossWeight < 46)
            {
                return Boss.GetBoss("Octopheesh");
            }
            else if (bossWeight < 54)
            {
                return Boss.GetBoss("Green");
            }
            else
            {
                return Boss.GetBoss("Destroyer");
            }
        }

        public Boss ChooseStageTwoBoss(IEnumerable<SiegeMarble> marbles)
        {
            var bossWeight = (int)Math.Round(marbles.Count() * ((_randomService.Rand.NextDouble() * 5) + 1));
            if (bossWeight < 10)
            {
                return Boss.GetBoss("Chest");
            }
            else if (bossWeight < 19)
            {
                return Boss.GetBoss("ScaryFace");
            }
            else if (bossWeight < 28)
            {
                return Boss.GetBoss("Red");
            }
            else if (bossWeight < 37)
            {
                return Boss.GetBoss("CorruptPurple");
            }
            else if (bossWeight < 46)
            {
                return Boss.GetBoss("MarbleBotPrototype");
            }
            else
            {
                return Boss.GetBoss("Overlord");
            }
        }

        [Command("help")]
        [Alias("")]
        [Priority(-1)]
        [Summary("Siege help.")]
        public async Task SiegeHelpCommand()
            => await ReplyAsync(embed: new EmbedBuilder()
                .AddField("How to play", new StringBuilder()
                    .AppendLine("Use `mb/siege signup <marble name>` to sign up as a marble! (you can only sign up once)")
                    .AppendLine("When everyone's done, use `mb/siege start`! The Siege begins automatically when 20 marbles have signed up.\n")
                    .AppendLine("When the Siege begins, use `mb/siege attack` to attack the boss!")
                    .AppendLine("Power-ups occasionally appear. Use `mb/siege grab` to try and activate the power-up (1/3 chance).\n")
                    .AppendLine("Check who's participating with `mb/siege contestants` and view Siege information with `mb/siege info`!")
                    .ToString())
                .AddField("Mechanics", new StringBuilder()
                    .AppendLine("There are a few differences between this and normal Marble Sieges:")
                    .AppendLine("- **Health Scaling**: Marble Health scales with difficulty ((difficulty + 2) * 5).")
                    .AppendLine("- **Vengeance**: When a marble dies, the damage multiplier goes up by 0.2 (0.4 if Morale Boost is active).")
                    .ToString())
                .AddField("More info", "For more information, visit https://github.com/doc671/MarbleBot/wiki/Marble-Siege.")
                .WithColor(GetColor(Context))
                .WithCurrentTimestamp()
                .WithTitle("Marble Siege!")
                .Build());
    }
}
