using Discord;
using Discord.Commands;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarbleBot.BaseClasses
{
    public class Siege : IDisposable
    {
        public Task Actions { get; set; }
        public bool Active { get; set; }
        public int AttackTime { get; set; } = 15000;
        public Boss Boss { get; set; } = Boss.Empty;
        public double DamageMultiplier { get {
                var deathCount = Marbles.Aggregate(0, (totalDeaths, m) => {
                    if (m.HP < 1) totalDeaths++;
                    return totalDeaths;
                });
                return (1.0 + (deathCount * 0.2)) * (Morales + 1);
            }
        }
        public DateTime LastMorale { get; set; } = DateTime.Parse("2019-01-01 00:00:00");
        public List<Marble> Marbles { get; set; } = new List<Marble>();
        public byte Morales { get; set; }
        public string PowerUp { get; set; } = "";
        public string PUImageUrl { get; set; } = "";
        public bool VictoryCalled { get; set; }

        public async Task DealDamageAsync(SocketCommandContext context, int dmg) {
            Boss.HP -= dmg;
            if (Boss.HP < 1) {
                Active = false;
                var id = context.IsPrivate ? context.User.Id : context.Guild.Id;
                await SiegeVictoryAsync(context, id);
            }
            if (Boss.Name == "Destroyer" && Boss.HP <= Boss.MaxHP / 2f) {
                AttackTime = 12000;
                Boss.ImageUrl = "https://cdn.discordapp.com/attachments/296376584238137355/567456912430333962/DestroyerCorrupted.png";
            }
        }

        private bool disposed = false;
        public void Dispose() => Dispose(true);

        private void Dispose(bool disposing) {
            if (disposed) return;
            if (disposing) {
                Actions.Wait();
                Actions.Dispose();
            }
            Boss.ResetHP();
            Marbles = null;
            disposed = true;
        }

        public void SetPowerUp(string PU) {
            PowerUp = PU;
            switch (PU) {
                case "Clone": PUImageUrl = "https://cdn.discordapp.com/attachments/296376584238137355/541373091495018496/PUClone.png"; break;
                case "Cure": PUImageUrl = "https://cdn.discordapp.com/attachments/296376584238137355/541373094724501524/PUCure.png"; break;
                case "Heal": PUImageUrl = "https://cdn.discordapp.com/attachments/296376584238137355/541373096238514202/PUHeal.png"; break;
                case "Morale Boost": PUImageUrl = "https://cdn.discordapp.com/attachments/296376584238137355/541373099090903070/PUMoraleBoost.png"; break;
                case "Overclock": PUImageUrl = "https://cdn.discordapp.com/attachments/296376584238137355/541373101649428480/PUOverclock.png"; break;
                case "Summon": PUImageUrl = "https://cdn.discordapp.com/attachments/296376584238137355/541373120129531939/PUSummon.png"; break;
                default: PUImageUrl = ""; break;
            }
        }

        // Separate task dealing with time-based boss responses
        public async Task SiegeBossActionsAsync(SocketCommandContext context, ulong id) {
            var startTime = DateTime.UtcNow;
            var timeout = false;
            do {
                await Task.Delay(AttackTime);
                if (Boss.HP < 1) {
                    if (!VictoryCalled) await SiegeVictoryAsync(context, id);
                    break;
                } else if (DateTime.UtcNow.Subtract(startTime).TotalMinutes >= 10) {
                    timeout = true;
                    break;
                } else {
                    // Attack marbles
                    var atk = Boss.Attacks[Global.Rand.Next(0, Boss.Attacks.Length)];
                    var builder = new EmbedBuilder()
                        .WithColor(MarbleBotModule.GetColor(context))
                        .WithCurrentTimestamp()
                        .WithDescription($"**{Boss.Name}** used **{atk.Name}**!")
                        .WithThumbnailUrl(Boss.ImageUrl)
                        .WithTitle($"WARNING: {atk.Name.ToUpper()} INBOUND!");
                    var hits = 0;
                    foreach (var marble in Marbles) {
                        if (marble.HP > 0) {
                            if (!(Global.Rand.Next(0, 100) > atk.Accuracy)) {
                                marble.HP -= atk.Damage;
                                hits++;
                                if (marble.HP < 1) {
                                    marble.HP = 0;
                                    builder.AddField($"**{marble.Name}** has been killed!", $"HP: **0**/{marble.MaxHP}\nDamage Multiplier: **{DamageMultiplier}**");
                                } else {
                                    var mse = atk.StatusEffect == marble.StatusEffect ? MSE.None : atk.StatusEffect;
                                    switch (mse) {
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
                                        builder.AddField($"**{marble.Name}** has died of Doom!", $"HP: **0**/{marble.MaxHP}\nDamage Multiplier: **{DamageMultiplier}**");
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
                    await context.Channel.SendMessageAsync(embed: builder.Build());

                    // Wear off Morale Boost
                    if (DateTime.UtcNow.Subtract(LastMorale).TotalSeconds > 20 && Morales > 0) {
                        Morales--;
                        await context.Channel.SendMessageAsync($"The effects of a Morale Boost power-up have worn off! The damage multiplier is now **{DamageMultiplier}**!");
                    }
                    
                    // Cause new power-up to appear
                    if (PowerUp == "") {
                        switch (Global.Rand.Next(0, Boss.Attacks.Length)) {
                            case 0:
                                SetPowerUp("Morale Boost");
                                await context.Channel.SendMessageAsync(embed: new EmbedBuilder()
                                    .WithColor(MarbleBotModule.GetColor(context))
                                    .WithCurrentTimestamp()
                                    .WithDescription("A **Morale Boost** power-up has spawned in the arena! Use `mb/siege grab` to try and activate it!")
                                    .WithThumbnailUrl(PUImageUrl)
                                    .WithTitle("Power-up spawned!")
                                    .Build());
                                break;
                            case 1:
                                SetPowerUp("Clone");
                                await context.Channel.SendMessageAsync(embed: new EmbedBuilder()
                                    .WithColor(MarbleBotModule.GetColor(context))
                                    .WithCurrentTimestamp()
                                    .WithDescription("A **Clone** power-up has spawned in the arena! Use `mb/siege grab` to try and activate it!")
                                    .WithThumbnailUrl(PUImageUrl)
                                    .WithTitle("Power-up spawned!").Build());
                                break;
                            case 2:
                                SetPowerUp("Summon");
                                await context.Channel.SendMessageAsync(embed: new EmbedBuilder()
                                    .WithColor(MarbleBotModule.GetColor(context))
                                    .WithCurrentTimestamp()
                                    .WithDescription("A **Summon** power-up has spawned in the arena! Use `mb/siege grab` to try and activate it!")
                                    .WithThumbnailUrl(PUImageUrl)
                                    .WithTitle("Power-up spawned!")
                                    .Build());
                                break;
                            case 3:
                                if (Marbles.Any(m => m.StatusEffect != MSE.None)) {
                                    SetPowerUp("Cure");
                                        await context.Channel.SendMessageAsync(embed: new EmbedBuilder()
                                        .WithColor(MarbleBotModule.GetColor(context))
                                        .WithCurrentTimestamp()
                                        .WithDescription("A **Cure** power-up has spawned in the arena! Use `mb/siege grab` to try and activate it!")
                                        .WithThumbnailUrl(PUImageUrl)
                                        .WithTitle("Power-up spawned!")
                                        .Build());
                                }
                                break;
                        }
                    }

                    // Siege failure
                    if (Marbles.Sum(m => m.HP) < 1) {
                        var marbles = new StringBuilder();
                        foreach (var marble in Marbles)
                            marbles.AppendLine(marble.ToString(context));
                        await context.Channel.SendMessageAsync(embed: new EmbedBuilder()
                            .WithColor(MarbleBotModule.GetColor(context))
                            .WithCurrentTimestamp()
                            .WithDescription($"All the marbles died!\n**{Boss.Name}** won!\nFinal HP: **{Boss.HP}**/{Boss.MaxHP}")
                            .AddField($"Fallen Marbles: **{Marbles.Count}**", marbles.ToString())
                            .WithThumbnailUrl(Boss.ImageUrl)
                            .WithTitle("Siege Failure!")
                            .Build());
                        break;
                    }
                }
            } while (Boss.HP > 0 || !timeout || Marbles.Sum(m => m.HP) < 1);
            Active = false;
            if (timeout || Marbles.Sum(m => m.HP) < 1) {
                Global.SiegeInfo.Remove(id);
                Dispose();
            }
            using (var marbleList = new StreamWriter(id + "siege.csv", false)) {
                await marbleList.WriteAsync("");
                marbleList.Close();
            }
            if (timeout) await context.Channel.SendMessageAsync("10 minute timeout reached! Siege aborted!");
        }

        public async Task SiegeVictoryAsync(SocketCommandContext context, ulong id) {
            if (VictoryCalled) return;
            VictoryCalled = true;
            var builder = new EmbedBuilder()
                .WithColor(MarbleBotModule.GetColor(context))
                .WithCurrentTimestamp()
                .WithTitle("Siege Victory!")
                .WithDescription($"**{Boss.Name}** has been defeated!");
            for (int i = 0; i < Marbles.Count; i++) {
                var marble = Marbles[i];
                var obj = MarbleBotModule.GetUsersObj();
                var user = MarbleBotModule.GetUser(context, obj, marble.Id);
                var output = new StringBuilder();
                if (user.Stage == 1 && Boss.Name == "Destroyer" && ((marble.DamageDealt > 0 && marble.HP > 0) || marble.DamageDealt > 149)) {
                    user.Stage = 2;
                    output.AppendLine($"Stage II! New items unlocked!");
                }
                int earnings = marble.DamageDealt + (marble.PUHits * 50);
                var didNothing = true;
                if (DateTime.UtcNow.Subtract(user.LastSiegeWin).TotalHours > 6) {
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
                        if (Boss.Drops.Length > 0) output.AppendLine("**Item Drops:**");
                        byte drops = 0;
                        foreach (var itemDrops in Boss.Drops) {
                            if (Global.Rand.Next(0, 100) < itemDrops.Chance) {
                                ushort amount;
                                if (itemDrops.MinCount == itemDrops.MaxCount) amount = itemDrops.MinCount;
                                else amount = (ushort)Global.Rand.Next(itemDrops.MinCount, itemDrops.MaxCount + 1);
                                if (user.Items.ContainsKey(itemDrops.ItemId)) user.Items[itemDrops.ItemId] +=amount;
                                else user.Items.Add(itemDrops.ItemId, amount);
                                var item = MarbleBotModule.GetItem(itemDrops.ItemId.ToString("000"));
                                user.NetWorth += item.Price * amount;
                                drops++;
                                output.AppendLine($"`[{itemDrops.ItemId.ToString("000")}]` {item.Name} x{amount}");
                            }
                        }
                        if (drops == 0) output.AppendLine("None");
                        output.AppendLine($"__**Total: {Global.UoM}{earnings:n}**__");
                        user.Balance += earnings;
                        user.NetWorth += earnings;
                    }
                }
                if (output.Length > 1 && !didNothing)
                    builder.AddField($"**{context.Client.GetUser(marble.Id).Username}**'s earnings", output.ToString());
                obj.Remove(marble.Id.ToString());
                obj.Add(new JProperty(marble.Id.ToString(), JObject.FromObject(user)));
                MarbleBotModule.WriteUsers(obj);
            }
            await context.Channel.SendMessageAsync(embed: builder.Build());
            Global.SiegeInfo.Remove(id);
            Dispose();
            using (var marbleList = new StreamWriter(id.ToString() + "csv", false)) {
                await marbleList.WriteAsync("");
                marbleList.Close();
            }
        }

        public override string ToString() => $"{Boss.Name}: {Marbles.Count}";

        public Siege(Marble[] marbles) { Marbles = new List<Marble>(marbles); }

        ~Siege() => Dispose(true);
    }
}
