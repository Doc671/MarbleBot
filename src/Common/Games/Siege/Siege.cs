using Discord;
using Discord.Commands;
using MarbleBot.Extensions;
using MarbleBot.Modules.Games.Services;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static MarbleBot.Modules.MarbleBotModule;

namespace MarbleBot.Common
{
    /// <summary> Represents a siege game. </summary>
    public class Siege : IDisposable
    {
        /// <summary> The siege game. </summary>
        public Task Actions { get; set; }
        /// <summary> Whether the siege is active. </summary>
        public bool Active { get; set; } = true;
        /// <summary> The boss that the marbles are fighting. </summary>
        public Boss Boss { get; set; } = Boss.Empty;
        /// <summary> The number which marble damage is multiplied by. </summary>
        public double DamageMultiplier => (1.0 + (Marbles.Aggregate(0, (totalDeaths, m) =>
                                                        {
                                                            if (m.HP < 1) totalDeaths++;
                                                            return totalDeaths;
                                                        }) * 0.2)) * (Morales + 1);
        /// <summary> The ID of the user's DM or guild where the siege is being played. </summary>
        public ulong Id { get; }
        /// <summary> The last time a Morale Boost power-up was activated. </summary>
        public DateTime LastMorale { get; set; } = new DateTime(2019, 1, 1);
        /// <summary> The marbles (player characters) fighting the boss. </summary>
        public List<SiegeMarble> Marbles { get; set; }
        /// <summary> The number of Morale Boost power-ups active. </summary>
        public int Morales { get; set; }
        /// <summary> The current power-up that can be grabbed. </summary>
        public PowerUp PowerUp { get; set; }

        private bool _disposed = false;
        private readonly GamesService _service;
        private bool _victoryCalled = false;

        // Separate task dealing with time-based boss responses
        public async Task BossActions(SocketCommandContext context)
        {
            var startTime = DateTime.UtcNow;
            var timeout = false;
            do
            {
                await Task.Delay(15000);

                if (_disposed) return;

                if (Boss.HP < 1)
                {
                    if (!_victoryCalled) await Victory(context);
                    break;
                }

                if (DateTime.UtcNow.Subtract(startTime).TotalMinutes >= 20)
                {
                    timeout = true;
                    break;
                }

                // Attack marbles
                var atk = Boss.Attacks.ElementAt(Global.Rand.Next(0, Boss.Attacks.Count));
                var builder = new EmbedBuilder()
                    .WithAuthor(Boss.Name, Boss.ImageUrl)
                    .WithColor(GetColor(context))
                    .WithCurrentTimestamp()
                    .WithDescription($"**{Boss.Name}** used **{atk.Name}**!")
                    .WithTitle($"WARNING: {atk.Name.ToUpper()} INBOUND!");

                var attackMissed = true;
                foreach (var marble in Marbles)
                {
                    if (marble.HP > 0)
                    {
                        if (!(Global.Rand.Next(0, 100) > atk.Accuracy))
                        {
                            marble.DealDamage(atk.Damage);
                            attackMissed = false;
                            if (marble.HP < 1)
                            {
                                marble.HP = 0;
                                builder.AddField($"**{marble.Name}** has been killed!", $"HP: **0**/{marble.MaxHP}\nDamage Multiplier: **{DamageMultiplier}**");
                            }
                            else
                            {
                                switch (atk.StatusEffect == marble.StatusEffect ? StatusEffect.None : atk.StatusEffect)
                                {
                                    case StatusEffect.Chill:
                                        marble.StatusEffect = StatusEffect.Chill;
                                        builder.AddField($"**{marble.Name}** has been chilled! All attacks will deal half damage unless cured!", $"HP: **{marble.HP}**/{marble.MaxHP}\nStatus Effect: **Chill**");
                                        break;
                                    case StatusEffect.Doom:
                                        marble.StatusEffect = StatusEffect.Doom;
                                        builder.AddField($"**{marble.Name}** has been doomed and will die in ~45 seconds if not cured!", $"HP: **{marble.HP}**/{marble.MaxHP}\nStatus Effect: **Doom**");
                                        marble.DoomStart = DateTime.UtcNow;
                                        break;
                                    case StatusEffect.Poison:
                                        marble.StatusEffect = StatusEffect.Poison;
                                        builder.AddField($"**{marble.Name}** has been poisoned and will lose HP every ~15 seconds until cured/at 1 HP!", $"HP: **{marble.HP}**/{marble.MaxHP}\nStatus Effect: **Poison**");
                                        break;
                                    case StatusEffect.Stun:
                                        marble.StatusEffect = StatusEffect.Stun;
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
                            case StatusEffect.Doom:
                                if (DateTime.UtcNow.Subtract(marble.DoomStart).TotalSeconds > 45)
                                {
                                    marble.HP = 0;
                                    builder.AddField($"**{marble.Name}** has died of Doom!", $"HP: **0**/{marble.MaxHP}\nDamage Multiplier: **{DamageMultiplier}**");
                                }
                                break;
                            case StatusEffect.Poison:
                                if (DateTime.UtcNow.Subtract(marble.LastPoisonTick).TotalSeconds > 15)
                                {
                                    if (marble.HP < 1) break;
                                    marble.HP -= (int)Math.Round((double)marble.MaxHP / 10);
                                    marble.LastPoisonTick = DateTime.UtcNow;
                                    if (marble.HP < 2)
                                    {
                                        marble.HP = 1;
                                        marble.StatusEffect = StatusEffect.None;
                                    }
                                    builder.AddField($"**{marble.Name}** has taken Poison damage!", $"HP: **{marble.HP}**/{marble.MaxHP}");
                                }
                                marble.LastPoisonTick = DateTime.UtcNow;
                                break;
                        }
                    }
                }
                if (attackMissed) builder.AddField("Missed!", "No-one got hurt!");

                // Wear off Morale Boost
                if (Morales > 0 && DateTime.UtcNow.Subtract(LastMorale).TotalSeconds > 20)
                {
                    Morales--;
                    builder.AddField("Morale Boost has worn off!",
                        $"The effects of a Morale Boost power-up have worn off! The damage multiplier is now **{DamageMultiplier}**!");
                }

                // Cause new power-up to appear
                if (PowerUp == PowerUp.None)
                {
                    switch (Global.Rand.Next(0, 8))
                    {
                        case 1:
                            PowerUp = PowerUp.Clone;
                            builder.AddField("Power-up spawned!", "A **Clone** power-up has spawned in the arena! Use `mb/siege grab` to try and activate it!")
                                .WithThumbnailUrl(GetPowerUpImageUrl(PowerUp));
                            break;
                        case 2:
                            if (Marbles.Any(m => m.StatusEffect != StatusEffect.None))
                            {
                                PowerUp = PowerUp.Cure;
                                builder.AddField("Power-up spawned!", "A **Cure** power-up has spawned in the arena! Use `mb/siege grab` to try and activate it!")
                                    .WithThumbnailUrl(GetPowerUpImageUrl(PowerUp));
                            }
                            break;
                        case 4:
                            PowerUp = PowerUp.MoraleBoost;
                            builder.AddField("Power-up spawned!", "A **Morale Boost** power-up has spawned in the arena! Use `mb/siege grab` to try and activate it!")
                                .WithThumbnailUrl(GetPowerUpImageUrl(PowerUp));
                            break;
                        case 6:
                            PowerUp = PowerUp.Summon;
                            builder.AddField("Power-up spawned!", "A **Summon** power-up has spawned in the arena! Use `mb/siege grab` to try and activate it!")
                                .WithThumbnailUrl(GetPowerUpImageUrl(PowerUp));
                            break;
                    }
                }

                await context.Channel.SendMessageAsync(embed: builder.Build());

            } while (Boss.HP > 0 && !timeout && !Marbles.All(m => m.HP == 0) && !_disposed);

            if (Boss.HP > 0 && !_disposed)
            {
                if (timeout) await context.Channel.SendMessageAsync("20 minute timeout reached! Siege aborted!");
                else
                {
                    var marbles = new StringBuilder();
                    foreach (var marble in Marbles)
                        marbles.AppendLine(marble.ToString(context, false));
                    await context.Channel.SendMessageAsync(embed: new EmbedBuilder()
                        .WithAuthor(Boss.Name, Boss.ImageUrl)
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

        public async Task DealDamage(SocketCommandContext context, int dmg)
        {
            Boss.HP -= dmg;
            if (Boss.HP < 1) await Victory(context);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            Active = false;
            _disposed = true;
            Boss.ResetHP();
            using (var marbleList = new StreamWriter($"Data{Path.DirectorySeparatorChar}{Id}siege.csv", false))
                marbleList.Write("");
            _service.SiegeInfo.TryRemove(Id, out _);
            if (disposing && Actions != null)
            {
                Actions.Dispose();
            }
        }

        /// <summary> Gets a boss using a string. </summary>
        /// <param name="searchTerm"> The string to search with. </param>
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

        public static string GetPowerUpImageUrl(PowerUp powerUp)
            => powerUp switch
            {
                PowerUp.Clone => "https://cdn.discordapp.com/attachments/296376584238137355/541373091495018496/PUClone.png",
                PowerUp.Cure => "https://cdn.discordapp.com/attachments/296376584238137355/541373094724501524/PUCure.png",
                PowerUp.Heal => "https://cdn.discordapp.com/attachments/296376584238137355/541373096238514202/PUHeal.png",
                PowerUp.MoraleBoost => "https://cdn.discordapp.com/attachments/296376584238137355/541373099090903070/PUMoraleBoost.png",
                PowerUp.Overclock => "https://cdn.discordapp.com/attachments/296376584238137355/541373101649428480/PUOverclock.png",
                PowerUp.Summon => "https://cdn.discordapp.com/attachments/296376584238137355/541373120129531939/PUSummon.png",
                _ => ""
            };

        /// <summary> Attacks the boss with a non-weapon item. </summary>
        /// <param name="context"> The context of the command. </param>
        /// <param name="obj"> The JSON object storing the users. </param>
        /// <param name="itemId"> The ID of the item. </param>
        /// <param name="damage"> The damage being dealt by the item. </param>
        /// <param name="consumable"> Whether or not the item is consumed upon use. </param>
        public async Task ItemAttack(SocketCommandContext context, JObject obj, uint itemId,
                                     int damage, bool consumable = false)
        {
            if (_disposed)
            {
                await context.Channel.SendMessageAsync("There is no currently ongoing Siege!");
                return;
            }

            if (!Marbles.Any(m => m.Id == context.User.Id))
            {
                await context.Channel.SendMessageAsync($"**{context.User.Username}**, you aren't in this Siege!");
                return;
            }

            var marble = Marbles.Find(m => m.Id == context.User.Id);
            if (marble.HP == 0)
            {
                await context.Channel.SendMessageAsync($"**{context.User.Username}**, you are out and can no longer attack!");
                return;
            }

            var user = GetUser(context);
            var item = GetItem<Item>(itemId.ToString("000"));
            if (item.Id == 10 && marble.QefpedunCharmUsed)
            {
                await context.Channel.SendMessageAsync($"**{context.User.Username}**, you can only use the **{item.Name}** once per battle!");
                return;
            }

            damage = (int)Math.Round(damage * DamageMultiplier);
            await DealDamage(context, damage);
            await context.Channel.SendMessageAsync(embed: new EmbedBuilder()
                .AddField("Boss HP", $"**{Boss.HP}**/{Boss.MaxHP}")
                .WithAuthor(context.User)
                .WithColor(GetColor(context))
                .WithCurrentTimestamp()
                .WithDescription($"**{marble.Name}** used their **{item.Name}**, dealing **{damage}** damage to the boss!")
                .WithTitle(item.Name)
                .Build());
            marble.DamageDealt += damage;
            if (consumable)
            {
                user.Items[item.Id]--;
                user.NetWorth -= item.Price;
                WriteUsers(obj, context.User, user);
            }
            if (item.Id == 10) marble.QefpedunCharmUsed = true;
        }

        public override string ToString() => $"[{Id}] {Boss.Name}: {Marbles.Count}";

        public async Task Victory(SocketCommandContext context)
        {
            if (_victoryCalled) return;
            _victoryCalled = true;
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

                // Advance user's stage if necessary
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
                        if (Boss.Drops.Count > 0) output.AppendLine("**Item Drops:**");
                        var dropPresent = false;
                        foreach (var itemDrops in Boss.Drops)
                        {
                            if (Global.Rand.Next(0, 100) < itemDrops.Chance)
                            {
                                dropPresent = true;

                                int noOfDrops;
                                if (itemDrops.MinCount == itemDrops.MaxCount)
                                    noOfDrops = itemDrops.MinCount;
                                else
                                    noOfDrops = Global.Rand.Next(itemDrops.MinCount, itemDrops.MaxCount + 1);

                                if (user.Items.ContainsKey(itemDrops.ItemId))
                                    user.Items[itemDrops.ItemId] += noOfDrops;
                                else
                                    user.Items.Add(itemDrops.ItemId, noOfDrops);

                                var item = GetItem<Item>(itemDrops.ItemId.ToString("000"));
                                user.NetWorth += item.Price * noOfDrops;
                                output.AppendLine($"`[{itemDrops.ItemId.ToString("000")}]` {item.Name} x{noOfDrops}");
                            }
                        }
                        if (!dropPresent) output.AppendLine("None");
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

        /// <summary> Attacks the boss with a weapon. </summary>
        /// <param name="context"> The context of the command. </param>
        /// <param name="weapon"> The weapon used to attack. </param>
        public async Task WeaponAttack(SocketCommandContext context, Weapon weapon)
        {
            if (_disposed)
            {
                await context.Channel.SendMessageAsync("There is no currently ongoing Siege!");
                return;
            }

            if (!Marbles.Any(m => m.Id == context.User.Id))
            {
                await context.Channel.SendMessageAsync($"**{context.User.Username}**, you aren't in this Siege!");
                return;
            }

            var marble = Marbles.Find(m => m.Id == context.User.Id);
            if (marble.HP == 0)
            {
                await context.Channel.SendMessageAsync($"**{context.User.Username}**, you are out and can no longer attack!");
                return;
            }

            var ammo = new Ammo();
            var user = GetUser(context);

            if (weapon.Ammo != null)
            {
                for (int i = weapon.Ammo.Length - 1; i >= 0; i--)
                {
                    if (user.Items.ContainsKey(weapon.Ammo[i]) && user.Items[weapon.Ammo[i]] >= weapon.Uses)
                    {
                        ammo = GetItem<Ammo>(weapon.Ammo[i].ToString("000"));
                        break;
                    }
                }

                if (ammo.Id == 0)
                {
                    await context.Channel.SendMessageAsync($"**{context.User.Username}**, you do not have enough ammo for this item!");
                    return;
                }

                var obj = GetUsersObject();
                user.Items[ammo.Id] -= weapon.Uses;
                user.NetWorth -= ammo.Price * weapon.Uses;
                WriteUsers(obj, context.User, user);
            }

            var builder = new EmbedBuilder()
                .WithAuthor(context.User)
                .WithColor(GetColor(context))
                .WithCurrentTimestamp()
                .WithTitle(weapon.Name);

            if (weapon.Uses == 1)
            {
                if (Global.Rand.Next(0, 100) < weapon.Accuracy)
                {
                    var damage = (int)Math.Round((weapon.Damage + (weapon.WarClass == WeaponClass.Ranged || weapon.WarClass == WeaponClass.Artillery ? ammo.Damage : 0.0))
                        * (Global.Rand.NextDouble() * 0.4 + 0.8) * 3d * DamageMultiplier);
                    await DealDamage(context, damage);
                    marble.DamageDealt += damage;
                    builder.WithDescription($"**{marble.Name}** used their **{weapon.Name}**, dealing **{damage}** damage to **{Boss.Name}**!");
                }
                else builder.WithDescription($"**{marble.Name}** used their **{weapon.Name}**! It missed!");
            }
            else
            {
                var totalDamage = 0;
                for (int i = 0; i < weapon.Uses; i++)
                {
                    if (Global.Rand.Next(0, 100) < weapon.Accuracy)
                    {
                        var damage = (int)Math.Round((weapon.Damage + (weapon.WarClass == WeaponClass.Ranged || weapon.WarClass == WeaponClass.Artillery ? ammo.Damage : 0.0))
                            * (Global.Rand.NextDouble() * 0.4 + 0.8) * 3d * DamageMultiplier);
                        await DealDamage(context, damage);
                        totalDamage += damage;
                        builder.AddField($"Attack {i + 1}", $"**{damage}** damage to **{Boss.Name}**.");
                    }
                    else builder.AddField($"Attack {i + 1}", "Missed!");
                    await Task.Delay(1);
                }
                marble.DamageDealt += totalDamage;
                builder.WithDescription($"**{marble.Name}** used their **{weapon.Name}**, dealing a total of **{totalDamage}** damage to **{Boss.Name}**!");
            }
            await context.Channel.SendMessageAsync(embed: builder
                        .AddField("Boss HP", $"**{Boss.HP}**/{Boss.MaxHP}")
                        .Build());
        }

        public Siege(GamesService service, SocketCommandContext context, IEnumerable<SiegeMarble> marbles)
        {
            _service = service;
            Id = context.IsPrivate ? context.User.Id : context.Guild.Id;
            Marbles = marbles.ToList();
        }

        ~Siege() => Dispose(false);
    }
}
