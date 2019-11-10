using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MarbleBot.Common;
using MarbleBot.Extensions;
using Newtonsoft.Json.Linq;
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

        [Command("signup")]
        [Alias("join")]
        [Summary("Sign up to the Marble Siege!")]
        public async Task SiegeSignupCommand([Remainder] string marbleName = "")
        => await Signup(Type, marbleName, 20, async () => { await SiegeStartCommand(); });

        [Command("start")]
        [Alias("begin")]
        [Summary("Starts the Marble Siege Battle.")]
        public async Task SiegeStartCommand([Remainder] string over = "")
        {
            ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;

            if (GamesService.SiegeInfo.ContainsKey(fileId) && GamesService.SiegeInfo[fileId].Active)
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

            SiegeMarble marble;
            MarbleBotUser user;
            var marbles = new List<SiegeMarble>();
            int stageTotal = 0;
            foreach (var (id, name) in rawMarbles)
            {
                marble = new SiegeMarble()
                {
                    Id = id,
                    Name = name,
                };

                user = GetUser(Context, id);
                stageTotal += user.Stage;

                if (user.Items.ContainsKey(63) && user.Items[63] >= 1)
                {
                    marble.Shield = GetItem<Item>("063");
                }

                if (user.Items.ContainsKey(80))
                {
                    marble.DamageIncrease = 110;
                }
                else if (user.Items.ContainsKey(74))
                {
                    marble.DamageIncrease = 95;
                }
                else if (user.Items.ContainsKey(71))
                {
                    marble.DamageIncrease = 60;
                }
                else if (user.Items.ContainsKey(66))
                {
                    marble.DamageIncrease = 40;
                }

                marbles.Add(marble);
            }
            if (GamesService.SiegeInfo.ContainsKey(fileId))
            {
                GamesService.SiegeInfo[fileId].Marbles = marbles;
            }
            else
            {
                GamesService.SiegeInfo.GetOrAdd(fileId, new Siege(GamesService, RandomService, Context, marbles));
            }

            if (marbleCount == 0)
            {
                await SendErrorAsync($"**{Context.User.Username}**, no-one is signed up!");
                return;
            }

            var currentSiege = GamesService.SiegeInfo[fileId];
            currentSiege.Active = true;

            // Pick boss & set battle stats based on boss
            if (over.Contains("override") && (BotCredentials.AdminIds.Any(id => id == Context.User.Id) || Context.IsPrivate))
            {
                currentSiege.Boss = Siege.GetBoss(over.Split(' ')[1].RemoveChar(' '));
            }
            else if (string.Compare(currentSiege.Boss.Name, "", true) == 0)
            {
                // Choose a stage 1 or stage 2 boss depending on the stage of each participant
                float stage = stageTotal / (float)currentSiege.Marbles.Count;
                if (stage == 1f)
                {
                    StageOneBossChooser(currentSiege);
                }
                else if (stage == 2f)
                {
                    StageTwoBossChooser(currentSiege);
                }
                else
                {
                    stage--;
                    if (RandomService.Rand.NextDouble() < stage)
                    {
                        StageTwoBossChooser(currentSiege);
                    }
                    else
                    {
                        StageOneBossChooser(currentSiege);
                    }
                }
            }

            var hp = ((int)currentSiege.Boss.Difficulty + 2) * 5;
            var marbleOutput = new StringBuilder();
            var mentionOutput = new StringBuilder();
            SocketUser socketUser;
            for (int i = 0; i < currentSiege.Marbles.Count; i++)
            {
                marble = currentSiege.Marbles[i];
                marble.SetHP(hp);
                socketUser = Context.Client.GetUser(marble.Id);
                marbleOutput.AppendLine($"**{marble.Name}** [{socketUser.Username}#{socketUser.Discriminator}]");
                if (GetUser(Context, marble.Id).SiegePing)
                {
                    mentionOutput.Append($"<@{marble.Id}> ");
                }
            }

            var builder = new EmbedBuilder()
                .WithColor(GetColor(Context))
                .WithCurrentTimestamp()
                .WithDescription("Get ready! Use `mb/siege attack` to attack and `mb/siege grab` to grab power-ups when they appear!")
                .WithTitle("The Siege has begun!")
                .WithThumbnailUrl(currentSiege.Boss.ImageUrl)
                .AddField($"Marbles: **{currentSiege.Marbles.Count}**", marbleOutput.ToString())
                .AddField($"Boss: **{currentSiege.Boss.Name}**", new StringBuilder()
                    .AppendLine($"HP: **{currentSiege.Boss.HP}**")
                    .AppendLine($"Attacks: **{currentSiege.Boss.Attacks.Count}**")
                    .AppendLine($"Difficulty: **{Enum.GetName(typeof(Difficulty), currentSiege.Boss.Difficulty)} {(int)currentSiege.Boss.Difficulty}**/10")
                    .ToString());

            // Siege Start
            var countdownMessage = await ReplyAsync("**3**");
            await Task.Delay(1000);
            await countdownMessage.ModifyAsync(m => m.Content = "**2**");
            await Task.Delay(1000);
            await countdownMessage.ModifyAsync(m => m.Content = "**1**");
            await Task.Delay(1000);
            await countdownMessage.ModifyAsync(m => m.Content = "**BEGIN THE SIEGE!**");
            currentSiege.Actions = Task.Run(async () => { await currentSiege.BossActions(Context); });
            await ReplyAsync(embed: builder.Build());
            if (mentionOutput.Length != 0 || (BotCredentials.AdminIds.Any(id => id == Context.User.Id) && !over.Contains("noping")))
            {
                await ReplyAsync(mentionOutput.ToString());
            }
        }

        [Command("stop")]
        [RequireOwner]
        public async Task SiegeStopCommand()
        {
            GamesService.SiegeInfo[Context.IsPrivate ? Context.User.Id : Context.Guild.Id].Dispose();
            await ReplyAsync("Siege successfully stopped.");
        }

        [Command("attack", RunMode = RunMode.Async)]
        [Alias("bonk")]
        [Summary("Attacks the boss.")]
        public async Task SiegeAttackCommand()
        {
            ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
            if (!GamesService.SiegeInfo.ContainsKey(fileId) || !GamesService.SiegeInfo[fileId].Active)
            {
                await SendErrorAsync("There is no currently ongoing Siege!");
                return;
            }

            var currentSiege = GamesService.SiegeInfo[fileId];
            var currentMarble = currentSiege.Marbles.Find(m => m.Id == Context.User.Id);
            if (currentMarble == null)
            {
                await SendErrorAsync($"**{Context.User.Username}**, you aren't in this Siege!");
                return;
            }

            if (currentMarble.HP == 0)
            {
                await SendErrorAsync($"**{Context.User.Username}**, you are out and can no longer attack!");
                return;
            }

            if (DateTime.UtcNow.Subtract(currentMarble.LastMoveUsed).TotalSeconds < 5)
            {
                await SendErrorAsync($"**{Context.User.Username}**, you must wait for {GetDateString(currentMarble.LastMoveUsed.Subtract(DateTime.UtcNow.AddSeconds(-5)))} until you can attack again!");
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

            var marbleDamage = RandomService.Rand.Next(1, 25);
            if (marbleDamage > 20)
            {
                marbleDamage = (int)Math.Round(marbleDamage * 1.5); // Critical attack
            }

            string title;
            string url;
            if (marbleDamage < 8)
            {
                title = "Slow attack!";
                url = "https://cdn.discordapp.com/attachments/296376584238137355/548217423623356418/SiegeAttackSlow.png";
            }
            else if (marbleDamage < 15)
            {
                title = "Fast attack!";
                url = "https://cdn.discordapp.com/attachments/296376584238137355/548217417847799808/SiegeAttackFast.png";
            }
            else if (marbleDamage < 21)
            {
                title = "Brutal attack!";
                url = "https://cdn.discordapp.com/attachments/296376584238137355/548217407337005067/SiegeAttackBrutal.png";
            }
            else
            {
                title = "CRITICAL attack!";
                url = "https://cdn.discordapp.com/attachments/296376584238137355/548217425359798274/SiegeAttackCritical.png";
            }
            marbleDamage = (int)Math.Round(marbleDamage * currentSiege.DamageMultiplier * (currentMarble.StatusEffect == StatusEffect.Chill ? 0.5 : 1.0) * (currentMarble.DamageIncrease / 100.0 + 1));

            if (currentMarble.Cloned)
            {
                await currentSiege.DealDamage(Context, marbleDamage * 5);
                builder.AddField("Clones attack!", $"Each of the clones dealt **{marbleDamage}** damage to the boss! The clones then disappeared!");
                currentMarble.Cloned = false;
            }

            await currentSiege.DealDamage(Context, marbleDamage);
            currentMarble.DamageDealt += marbleDamage;
            builder.WithTitle(title)
                .WithThumbnailUrl(url)
                .WithDescription($"**{currentMarble.Name}** dealt **{marbleDamage}** damage to **{currentSiege.Boss.Name}**!")
                .AddField("Boss HP", $"**{currentSiege.Boss.HP}**/{currentSiege.Boss.MaxHP}");

            await ReplyAsync(embed: builder.Build());

            if (currentSiege.Boss.HP < 1)
            {
                await currentSiege.Victory(Context);
            }
        }

        [Command("grab", RunMode = RunMode.Async)]
        [Summary("Has a 1/3 chance of activating the power-up.")]
        public async Task SiegeGrabCommand()
        {
            ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;

            if (!GamesService.SiegeInfo.ContainsKey(fileId) || !GamesService.SiegeInfo[fileId].Active)
            {
                await ReplyAsync($"**{Context.User.Username}**, there is no currently ongoing Siege!");
                return;
            }

            var currentSiege = GamesService.SiegeInfo[fileId];
            var currentMarble = currentSiege.Marbles.Find(m => m.Id == Context.User.Id);
            if (currentMarble == null)
            {
                await ReplyAsync($"**{Context.User.Username}**, you aren't in this Siege!");
                return;
            }

            if (currentSiege.PowerUp == PowerUp.None)
            {
                await ReplyAsync($"**{Context.User.Username}**, there is no power-up to grab!");
                return;
            }

            if (currentMarble.HP < 1)
            {
                await ReplyAsync($"**{Context.User.Username}**, you are out and can no longer grab power-ups!");
                return;
            }

            if (DateTime.UtcNow.Subtract(currentMarble.LastMoveUsed).TotalSeconds < 5)
            {
                await SendErrorAsync($"**{Context.User.Username}**, you must wait for {GetDateString(currentMarble.LastMoveUsed.Subtract(DateTime.UtcNow.AddSeconds(-5)))} until you can attack again!");
                return;
            }

            currentMarble.LastMoveUsed = DateTime.UtcNow;

            if (RandomService.Rand.Next(0, 3) == 0)
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
                        var choice = RandomService.Rand.Next(0, 2);
                        string ally;
                        string url;
                        switch (choice)
                        {
                            case 0: ally = "Frigidium"; url = "https://cdn.discordapp.com/attachments/296376584238137355/543745898690379816/Frigidium.png"; break;
                            case 1: ally = "Neptune"; url = "https://cdn.discordapp.com/attachments/296376584238137355/543745899591893012/Neptune.png"; break;
                            default: ally = "MarbleBot"; url = ""; break;
                        }
                        var dmg = RandomService.Rand.Next(25, 30) * currentSiege.Boss.Stage * ((int)currentSiege.Boss.Difficulty >> 1);
                        currentSiege.Boss.HP -= (int)Math.Round(dmg * currentSiege.DamageMultiplier);
                        if (currentSiege.Boss.HP < 0)
                        {
                            currentSiege.Boss.HP = 0;
                        }

                        builder.WithThumbnailUrl(url)
                            .AddField("Boss HP", $"**{currentSiege.Boss.HP}**/{currentSiege.Boss.MaxHP}")
                            .WithDescription($"**{currentMarble.Name}** activated **Summon**! **{ally}** came into the arena and dealt **{dmg}** damage to the boss!");
                        break;
                }
                currentSiege.PowerUp = PowerUp.None;
                await ReplyAsync(embed: builder.WithTitle("POWER-UP ACTIVATED!")
                    .Build());
            }
            else
            {
                await ReplyAsync("You failed to grab the power-up!");
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
            if (GamesService.SiegeInfo.ContainsKey(fileId) && GamesService.SiegeInfo[fileId].Active)
            {
                var siege = GamesService.SiegeInfo[fileId];
                foreach (var marble in siege.Marbles)
                {
                    marbleOutput.AppendLine(marble.ToString(Context));
                }

                builder.AddField($"Boss: **{siege.Boss.Name}**", $"\nHP: **{siege.Boss.HP}**/{siege.Boss.MaxHP}\nAttacks: **{siege.Boss.Attacks.Count}**\nDifficulty: **{Enum.GetName(typeof(Difficulty), siege.Boss.Difficulty)}**");

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

                builder.WithDescription($"Damage Multiplier: **{siege.DamageMultiplier}**\nActive Power-up: **{Enum.GetName(typeof(PowerUp), siege.PowerUp).CamelToTitleCase()}**")
                     .WithThumbnailUrl(siege.Boss.ImageUrl);
            }
            else
            {
                if (GamesService.SiegeInfo.ContainsKey(fileId) && GamesService.SiegeInfo[fileId].Boss != null)
                {
                    var siege = GamesService.SiegeInfo[fileId];
                    builder.AddField($"Boss: **{siege.Boss.Name}**", $"\nHP: **{siege.Boss.MaxHP}**\nAttacks: **{siege.Boss.Attacks.Count}**\nDifficulty: **{Enum.GetName(typeof(Difficulty), siege.Boss.Difficulty)}**")
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
        public async Task MarbleInfoCommand(string searchTerm = null)
        {
            ulong fileId = Context.IsPrivate ? Context.User.Id : Context.Guild.Id;
            if (!GamesService.SiegeInfo.ContainsKey(fileId))
            {
                await ReplyAsync($"**{Context.User.Username}**, could not find the requested marble!");
                return;
            }

            var currentMarble = searchTerm == null ? GamesService.SiegeInfo[fileId].Marbles.Find(m => m.Id == Context.User.Id)
                : GamesService.SiegeInfo[fileId].Marbles.Find(m => string.Compare(m.Name, searchTerm, true) == 0);
            if (currentMarble == null)
            {
                await ReplyAsync($"**{Context.User.Username}**, could not find the requested marble!");
                return;
            }

            await ReplyAsync(embed: new EmbedBuilder()
                .AddField("HP", $"**{currentMarble.HP}**/{currentMarble.MaxHP}", true)
                .AddField("Status Effect", Enum.GetName(typeof(StatusEffect), currentMarble.StatusEffect), true)
                .AddField("Shield", currentMarble.Shield.Name, true)
                .AddField("Damage Increase", $"{currentMarble.DamageIncrease}%", true)
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
                    var racerInfo = await leaderboardFile.ReadLineAsync();
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
                case "preethetree": boss = Siege.GetBoss("PreeTheTree"); break;
                case "hattmann": boss = Siege.GetBoss("HattMann"); break;
                case "orange": boss = Siege.GetBoss("Orange"); break;
                case "green": boss = Siege.GetBoss("Green"); break;
                case "destroyer": boss = Siege.GetBoss("Destroyer"); break;
                case "helpme":
                case "helpmethetree": boss = Siege.GetBoss("HelpMeTheTree"); break;
                case "erango": boss = Siege.GetBoss("Erango"); break;
                case "octopheesh": boss = Siege.GetBoss("Octopheesh"); break;
                case "red": boss = Siege.GetBoss("Red"); break;
                case "corruptpurple": boss = Siege.GetBoss("CorruptPurple"); break;
                case "chest": boss = Siege.GetBoss("Chest"); break;
                case "scaryface": boss = Siege.GetBoss("ScaryFace"); break;
                case "marblebot":
                case "marblebotprototype": boss = Siege.GetBoss("MarbleBotPrototype"); break;
                case "overlord": boss = Siege.GetBoss("Overlord"); break;
                case "rockgolem": boss = Siege.GetBoss("Rock Golem"); break;
                default:
                    await ReplyAsync("Could not find the requested boss!");
                    return;
            }
            if (boss.Stage > GetUser(Context).Stage)
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
                    drops.AppendLine($"`[{idString}]` **{GetItem<Item>(idString).Name}**: {dropAmount} ({drop.Chance}%)");
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
            var obj = GetUsersObject();
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
            if (user.SiegePing)
            {
                await ReplyAsync($"**{Context.User.Username}**, you will now be pinged when a Siege that you are in starts.\n(type `mb/siege ping` to turn off)");
            }
            else
            {
                await ReplyAsync($"**{Context.User.Username}**, you will no longer be pinged when a Siege that you are in starts.\n(type `mb/siege ping` to turn on)");
            }
        }

        public void StageOneBossChooser(Siege currentSiege)
        {
            var bossWeight = (int)Math.Round(currentSiege.Marbles.Count * ((RandomService.Rand.NextDouble() * 5) + 1));
            if (bossWeight < 7)
            {
                currentSiege.Boss = Siege.GetBoss("PreeTheTree");
            }
            else if (bossWeight < 14)
            {
                currentSiege.Boss = Siege.GetBoss("HelpMeTheTree");
            }
            else if (bossWeight < 22)
            {
                currentSiege.Boss = Siege.GetBoss("HattMann");
            }
            else if (bossWeight < 30)
            {
                currentSiege.Boss = Siege.GetBoss("Orange");
            }
            else if (bossWeight < 38)
            {
                currentSiege.Boss = Siege.GetBoss("Erango");
            }
            else if (bossWeight < 46)
            {
                currentSiege.Boss = Siege.GetBoss("Octopheesh");
            }
            else if (bossWeight < 54)
            {
                currentSiege.Boss = Siege.GetBoss("Green");
            }
            else
            {
                currentSiege.Boss = Siege.GetBoss("Destroyer");
            }
        }

        public void StageTwoBossChooser(Siege currentSiege)
        {
            var bossWeight = (int)Math.Round(currentSiege.Marbles.Count * ((RandomService.Rand.NextDouble() * 5) + 1));
            if (bossWeight < 10)
            {
                currentSiege.Boss = Siege.GetBoss("Chest");
            }
            else if (bossWeight < 19)
            {
                currentSiege.Boss = Siege.GetBoss("ScaryFace");
            }
            else if (bossWeight < 28)
            {
                currentSiege.Boss = Siege.GetBoss("Red");
            }
            else if (bossWeight < 37)
            {
                currentSiege.Boss = Siege.GetBoss("CorruptPurple");
            }
            else if (bossWeight < 46)
            {
                currentSiege.Boss = Siege.GetBoss("MarbleBotPrototype");
            }
            else
            {
                currentSiege.Boss = Siege.GetBoss("Overlord");
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
