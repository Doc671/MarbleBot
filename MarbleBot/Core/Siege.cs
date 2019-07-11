using Discord;
using Discord.Commands;
using MarbleBot.Extensions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static MarbleBot.MarbleBotModule;

namespace MarbleBot.Core
{
    /// <summary> Represents a siege game. </summary>
    public class Siege : IDisposable
    {
        public Task Actions { get; set; }
        public bool Active { get; set; } = true;
        public int AttackTime { get; set; } = 15000;
        public Boss Boss { get; set; } = Boss.Empty;
        public double DamageMultiplier => (1.0 + (Marbles.Aggregate(0, (totalDeaths, m) =>
                                                        {
                                                            if (m.HP < 1) totalDeaths++;
                                                            return totalDeaths;
                                                        }) * 0.2)) * (Morales + 1);
        public ulong Id { get; }
        public DateTime LastMorale { get; set; } = DateTime.Parse("2019-01-01 00:00:00");
        public List<SiegeMarble> Marbles { get; set; } = new List<SiegeMarble>();
        public int Morales { get; set; }
        public string PowerUp { get; set; } = "";
        public string PUImageUrl { get; set; } = "";
        public bool VictoryCalled { get; set; }

        public async Task DealDamageAsync(SocketCommandContext context, int dmg)
        {
            Boss.HP -= dmg;
            if (Boss.HP < 1)
                await SiegeVictoryAsync(context);
            if ((string.Compare(Boss.Name, "Destroyer", true) == 0) && Boss.HP <= Boss.MaxHP >> 1)
            {
                AttackTime = 12000;
                Boss.ImageUrl = "https://cdn.discordapp.com/attachments/296376584238137355/567456912430333962/DestroyerCorrupted.png";
            }
        }

        private bool _disposed = false;
        public void Dispose() => Dispose(true);

        private void Dispose(bool disposing)
        {
            Active = false;
            if (_disposed) return;
            _disposed = true;
            Boss.ResetHP();
            using (var marbleList = new StreamWriter($"Data{Path.DirectorySeparatorChar}{Id}siege.csv", false))
                marbleList.Write("");
            Global.SiegeInfo.TryRemove(Id, out _);
            if (disposing)
            {
                Actions.Wait();
                Actions.Dispose();
            }
        }

        public static Boss GetBoss(string searchTerm)
        {
            string json;
            using (var bosses = new StreamReader($"Resources{Path.DirectorySeparatorChar}Bosses.json")) json = bosses.ReadToEnd();
            var obj = new Dictionary<string, JObject>(JObject.Parse(json).ToObject<IDictionary<string, JObject>>(),
                StringComparer.InvariantCultureIgnoreCase);
            var boss = Boss.Empty;
            if (searchTerm.Contains(' ') || (string.Compare(searchTerm[0].ToString(), searchTerm[0].ToString(), true) == 0))
                searchTerm = searchTerm.ToPascalCase();
            if (obj.ContainsKey(searchTerm)) boss = obj[searchTerm].ToObject<Boss>();
            return boss;
        }

        public async Task ItemAttack(SocketCommandContext context, JObject obj, int itemId,
                                     int damage, bool consumable = false, int ammoId = 0,
                                     int ammoRequired = 1, int accuracyDivisor = 1)
        {
            var item = GetItem(itemId.ToString("000"));
            var ammo = GetItem(ammoId.ToString("000"));
            var marble = Marbles.Find(m => m.Id == context.User.Id);

            if (marble.HP == 0)
            {
                await context.Channel.SendMessageAsync($"**{context.User.Username}**, you are out and can no longer attack!");
                return;
            }

            var user = GetUser(context);

            // Removes items from the user (consumable offensive items or ammo)
            void UpdateUser(Item itm, int noOfItems)
            {
                user.Items[itm.Id] -= noOfItems;
                user.NetWorth -= itm.Price * noOfItems;
                WriteUsers(obj, context.User, user);
            }

            if (item.Id == 10 && marble.QefpedunCharmUsed)
            {
                await context.Channel.SendMessageAsync($"**{context.User.Username}**, you can only use the **{item.Name}** once per battle!");
                return;
            }

            if (ammoId != 0 && user.Items[ammoId] < ammoRequired)
            {
                await context.Channel.SendMessageAsync($"**{context.User.Username}**, you do not have enough ammo for this item!");
                return;
            }

            if (item.WarClass != WarClass.None)
            {
                damage = (int)Math.Round(damage * DamageMultiplier);
                if (ammoId != 0) damage += ammo.Damage;
                await DealDamageAsync(context, damage);
                await context.Channel.SendMessageAsync(embed: new EmbedBuilder()
                    .AddField("Boss HP", $"**{Boss.HP}**/{Boss.MaxHP}")
                    .WithColor(GetColor(context))
                    .WithCurrentTimestamp()
                    .WithDescription($"**{marble.Name}** used their **{item.Name}**, dealing **{damage}** damage to the boss!")
                    .WithTitle(item.Name)
                    .Build());
                marble.DamageDealt += damage;
                if (ammoId != 0) UpdateUser(ammo, -ammoRequired);
            }
            else if (Global.Rand.Next(0, 100) < (float)marble.ItemAccuracy / accuracyDivisor)
            {
                damage = (int)Math.Round(damage * DamageMultiplier);
                await DealDamageAsync(context, damage);
                await context.Channel.SendMessageAsync(embed: new EmbedBuilder()
                    .AddField("Boss HP", $"**{Boss.HP}**/{Boss.MaxHP}")
                    .WithColor(GetColor(context))
                    .WithCurrentTimestamp()
                    .WithDescription($"**{marble.Name}** used their **{item.Name}**, dealing **{damage}** damage to the boss!")
                    .WithTitle(item.Name)
                    .Build());
                marble.DamageDealt += damage;
                marble.ItemAccuracy -= 30;
                if (consumable) UpdateUser(item, 1);
                if (item.Id == 10) marble.QefpedunCharmUsed = true;
            }
            else await context.Channel.SendMessageAsync(embed: new EmbedBuilder()
                                    .WithColor(GetColor(context))
                                    .WithCurrentTimestamp()
                                    .WithDescription($"**{marble.Name}** used their **{item.Name}**! It missed!")
                                    .WithTitle(item.Name)
                                    .Build());
        }

        public void SetPowerUp(string PU)
        {
            PowerUp = PU;
            PUImageUrl = PU switch
            {
                "Clone" => "https://cdn.discordapp.com/attachments/296376584238137355/541373091495018496/PUClone.png",
                "Cure" => "https://cdn.discordapp.com/attachments/296376584238137355/541373094724501524/PUCure.png",
                "Heal" => "https://cdn.discordapp.com/attachments/296376584238137355/541373096238514202/PUHeal.png",
                "Morale Boost" => "https://cdn.discordapp.com/attachments/296376584238137355/541373099090903070/PUMoraleBoost.png",
                "Overclock" => "https://cdn.discordapp.com/attachments/296376584238137355/541373101649428480/PUOverclock.png",
                "Summon" => "https://cdn.discordapp.com/attachments/296376584238137355/541373120129531939/PUSummon.png",
                _ => ""
            };
        }

        // Separate task dealing with time-based boss responses
        public async Task SiegeBossActionsAsync(SocketCommandContext context)
        {
            var startTime = DateTime.UtcNow;
            var timeout = false;
            do
            {
                await Task.Delay(AttackTime);
                if (Boss.HP < 1)
                {
                    if (!VictoryCalled) await SiegeVictoryAsync(context);
                    break;
                }
                else if (DateTime.UtcNow.Subtract(startTime).TotalMinutes >= 20)
                {
                    timeout = true;
                    break;
                }
                else
                {
                    // Attack marbles
                    var atk = Boss.Attacks[Global.Rand.Next(0, Boss.Attacks.Length)];
                    var builder = new EmbedBuilder()
                        .WithColor(GetColor(context))
                        .WithCurrentTimestamp()
                        .WithDescription($"**{Boss.Name}** used **{atk.Name}**!")
                        .WithThumbnailUrl(Boss.ImageUrl)
                        .WithTitle($"WARNING: {atk.Name.ToUpper()} INBOUND!");
                    var hits = 0;
                    foreach (var marble in Marbles)
                    {
                        if (marble.HP > 0)
                        {
                            if (!(Global.Rand.Next(0, 100) > atk.Accuracy))
                            {
                                marble.DealDamage(atk.Damage);
                                hits++;
                                if (marble.HP < 1)
                                {
                                    marble.HP = 0;
                                    builder.AddField($"**{marble.Name}** has been killed!", $"HP: **0**/{marble.MaxHP}\nDamage Multiplier: **{DamageMultiplier}**");
                                }
                                else
                                {
                                    switch (atk.StatusEffect == marble.StatusEffect ? MSE.None : atk.StatusEffect)
                                    {
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
                            switch (marble.StatusEffect)
                            {
                                case MSE.Doom:
                                    if (DateTime.UtcNow.Subtract(marble.DoomStart).TotalSeconds > 45)
                                    {
                                        marble.HP = 0;
                                        builder.AddField($"**{marble.Name}** has died of Doom!", $"HP: **0**/{marble.MaxHP}\nDamage Multiplier: **{DamageMultiplier}**");
                                    }
                                    break;
                                case MSE.Poison:
                                    if (DateTime.UtcNow.Subtract(marble.LastPoisonTick).TotalSeconds > 15)
                                    {
                                        if (marble.HP < 1) break;
                                        marble.HP -= (int)Math.Round((double)marble.MaxHP / 10);
                                        marble.LastPoisonTick = DateTime.UtcNow;
                                        if (marble.HP < 2)
                                        {
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
                    if (DateTime.UtcNow.Subtract(LastMorale).TotalSeconds > 20 && Morales > 0)
                    {
                        Morales--;
                        await context.Channel.SendMessageAsync($"The effects of a Morale Boost power-up have worn off! The damage multiplier is now **{DamageMultiplier}**!");
                    }

                    // Cause new power-up to appear
                    if (string.Compare(PowerUp, "", true) == 0)
                    {
                        switch (Global.Rand.Next(0, Boss.Attacks.Length))
                        {
                            case 0:
                                SetPowerUp("Morale Boost");
                                await context.Channel.SendMessageAsync(embed: new EmbedBuilder()
                                    .WithColor(GetColor(context))
                                    .WithCurrentTimestamp()
                                    .WithDescription("A **Morale Boost** power-up has spawned in the arena! Use `mb/siege grab` to try and activate it!")
                                    .WithThumbnailUrl(PUImageUrl)
                                    .WithTitle("Power-up spawned!")
                                    .Build());
                                break;
                            case 1:
                                SetPowerUp("Clone");
                                await context.Channel.SendMessageAsync(embed: new EmbedBuilder()
                                    .WithColor(GetColor(context))
                                    .WithCurrentTimestamp()
                                    .WithDescription("A **Clone** power-up has spawned in the arena! Use `mb/siege grab` to try and activate it!")
                                    .WithThumbnailUrl(PUImageUrl)
                                    .WithTitle("Power-up spawned!").Build());
                                break;
                            case 2:
                                SetPowerUp("Summon");
                                await context.Channel.SendMessageAsync(embed: new EmbedBuilder()
                                    .WithColor(GetColor(context))
                                    .WithCurrentTimestamp()
                                    .WithDescription("A **Summon** power-up has spawned in the arena! Use `mb/siege grab` to try and activate it!")
                                    .WithThumbnailUrl(PUImageUrl)
                                    .WithTitle("Power-up spawned!")
                                    .Build());
                                break;
                            case 3:
                                if (Marbles.Any(m => m.StatusEffect != MSE.None))
                                {
                                    SetPowerUp("Cure");
                                    await context.Channel.SendMessageAsync(embed: new EmbedBuilder()
                                    .WithColor(GetColor(context))
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
                    if (Marbles.Sum(m => m.HP) < 1) break;
                }
            } while (Boss.HP > 0 && !timeout && Marbles.Sum(m => m.HP) > 0);
            if (Boss.HP > 0)
            {
                if (timeout) await context.Channel.SendMessageAsync("20 minute timeout reached! Siege aborted!");
                else
                {
                    var marbles = new StringBuilder();
                    foreach (var marble in Marbles)
                        marbles.AppendLine(marble.ToString(context, false));
                    await context.Channel.SendMessageAsync(embed: new EmbedBuilder()
                        .WithColor(GetColor(context))
                        .WithCurrentTimestamp()
                        .WithDescription($"All the marbles died!\n**{Boss.Name}** won!\nFinal HP: **{Boss.HP}**/{Boss.MaxHP}")
                        .AddField($"Fallen Marbles: **{Marbles.Count}**", marbles.ToString())
                        .WithThumbnailUrl(Boss.ImageUrl)
                        .WithTitle("Siege Failure!")
                        .Build());
                }
                Dispose(true);
            }
        }

        public async Task SiegeVictoryAsync(SocketCommandContext context)
        {
            if (VictoryCalled) return;
            VictoryCalled = true;
            var builder = new EmbedBuilder()
                .WithColor(GetColor(context))
                .WithCurrentTimestamp()
                .WithTitle("Siege Victory!")
                .WithDescription($"**{Boss.Name}** has been defeated!");
            var obj = GetUsersObject();
            for (int i = 0; i < Marbles.Count; i++)
            {
                var marble = Marbles[i];
                var user = GetUser(context, obj, marble.Id);
                var output = new StringBuilder();
                if (user.Stage == 1 && string.Compare(Boss.Name, "Destroyer", true) == 0 && ((marble.DamageDealt > 0 && marble.HP > 0) || marble.DamageDealt > 149))
                {
                    user.Stage = 2;
                    output.AppendLine($"**You have entered Stage II!** Much new content has been unlocked - see `mb/advice` for more info!");
                }
                else if (user.Stage == 2 && string.Compare(Boss.Name, "Overlord", true) == 0 && ((marble.DamageDealt > 0 && marble.HP > 0) || marble.DamageDealt > 149))
                {
                    user.Stage = 3;
                    output.AppendLine($"**You have entered Stage III!**");
                }
                int earnings = marble.DamageDealt + (marble.PowerUpHits * 50);
                if (DateTime.UtcNow.Subtract(user.LastSiegeWin).TotalHours > 6)
                {
                    if (marble.DamageDealt > 0)
                        output.AppendLine($"Damage dealt: {Global.UoM}**{marble.DamageDealt:n2}**");
                    else break;
                    if (marble.PowerUpHits > 0)
                        output.AppendLine($"Power-ups grabbed (x50): {Global.UoM}**{marble.PowerUpHits * 50:n2}**");
                    if (marble.HP > 0)
                    {
                        earnings += 200;
                        output.AppendLine($"Alive bonus: {Global.UoM}**{200:n2}**");
                        user.SiegeWins++;
                    }
                    if (user.Items.ContainsKey(83))
                    {
                        earnings *= 3;
                        output.AppendLine("Pendant bonus: x**3**");
                    }
                    if (output.Length > 0)
                    {
                        if (marble.HP > 0) user.LastSiegeWin = DateTime.UtcNow;
                        if (Boss.Drops.Length > 0) output.AppendLine("**Item Drops:**");
                        int drops = 0;
                        foreach (var itemDrops in Boss.Drops)
                        {
                            if (Global.Rand.Next(0, 100) < itemDrops.Chance)
                            {
                                ushort amount;
                                if (itemDrops.MinCount == itemDrops.MaxCount) amount = itemDrops.MinCount;
                                else amount = (ushort)Global.Rand.Next(itemDrops.MinCount, itemDrops.MaxCount + 1);
                                if (user.Items.ContainsKey(itemDrops.ItemId)) user.Items[itemDrops.ItemId] += amount;
                                else user.Items.Add(itemDrops.ItemId, amount);
                                var item = GetItem(itemDrops.ItemId.ToString("000"));
                                user.NetWorth += item.Price * amount;
                                drops++;
                                output.AppendLine($"`[{itemDrops.ItemId.ToString("000")}]` {item.Name} x{amount}");
                            }
                        }
                        if (drops == 0) output.AppendLine("None");
                        output.AppendLine($"__**Total: {Global.UoM}{earnings:n2}**__");
                        user.Balance += earnings;
                        user.NetWorth += earnings;
                        builder.AddField($"**{context.Client.GetUser(marble.Id).Username}**'s earnings", output.ToString());
                        obj.Remove(marble.Id.ToString());
                        obj.Add(new JProperty(marble.Id.ToString(), JObject.FromObject(user)));
                    }
                }
            }
            await context.Channel.SendMessageAsync(embed: builder.Build());
            WriteUsers(obj);
            Dispose(true);
        }

        public override string ToString() => $"{Boss.Name}: {Marbles.Count}";

        public Siege(SocketCommandContext context, SiegeMarble[] marbles)
        {
            Id = context.IsPrivate ? context.User.Id : context.Guild.Id;
            Marbles = new List<SiegeMarble>(marbles);
        }

        ~Siege() => Dispose(true);
    }
}
