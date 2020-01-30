using Discord;
using Discord.Commands;
using MarbleBot.Extensions;
using MarbleBot.Modules.Games.Services;
using MarbleBot.Services;
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
    public class Siege : IMarbleBotGame
    {
        public Task? Actions { get; set; }
        public bool Active { get; set; } = true;
        public Boss Boss { get; set; } = Boss.Empty;
        public float DamageMultiplier => (1f + (Marbles.Aggregate(0f, (totalDeaths, m) =>
                                                {
                                                    if (m.HP < 1)
                                                    {
                                                        totalDeaths++;
                                                    }

                                                    return totalDeaths;
                                                }) * 0.2f)) * (Morales + 1);
        public ulong Id { get; }
        public DateTime LastMorale { get; set; } = DateTime.MinValue;
        public List<SiegeMarble> Marbles { get; set; }
        public int Morales { get; set; }
        public PowerUp PowerUp { get; set; }

        private readonly SocketCommandContext _context;
        private bool _disposed = false;
        private readonly GamesService _gamesService;
        private readonly RandomService _randomService;
        private bool _victoryCalled = false;

        // Separate task dealing with time-based boss responses
        public async Task BossActions()
        {
            var startTime = DateTime.UtcNow;
            bool timeout = false;

            await Task.Delay(15000);
            while (Boss.HP > 0 && !timeout && !Marbles.All(m => m.HP == 0) && !_disposed)
            {
                // Attack marbles
                var attack = Boss.Attacks.ElementAt(_randomService.Rand.Next(0, Boss.Attacks.Count));
                var builder = new EmbedBuilder()
                    .WithAuthor(Boss.Name, Boss.ImageUrl)
                    .WithColor(GetColor(_context))
                    .WithCurrentTimestamp()
                    .WithDescription($"**{Boss.Name}** used **{attack.Name}**!")
                    .WithTitle($"WARNING: {attack.Name.ToUpper()} INBOUND!");

                var attackMissed = true;
                foreach (var marble in Marbles)
                {
                    if (marble.HP > 0)
                    {
                        if (!(_randomService.Rand.Next(0, 100) > attack.Accuracy))
                        {
                            marble.DealDamage(attack.Damage);
                            attackMissed = false;
                            if (marble.HP < 1)
                            {
                                marble.HP = 0;
                                builder.AddField($"**{marble.Name}** has been killed!", $"HP: **0**/{marble.MaxHP}\nDamage Multiplier: **{DamageMultiplier}**");
                            }
                            else
                            {
                                switch (attack.StatusEffect == marble.StatusEffect ? StatusEffect.None : attack.StatusEffect)
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
                                    if (marble.HP < 1)
                                    {
                                        break;
                                    }

                                    marble.HP -= (int)Math.Round(marble.MaxHP / 10d);
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

                if (attackMissed)
                {
                    builder.AddField("Missed!", "No-one got hurt!");
                }

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
                    switch (_randomService.Rand.Next(0, 8))
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

                await _context.Channel.SendMessageAsync(embed: builder.Build());

                await Task.Delay(15000);

                timeout = DateTime.UtcNow.Subtract(startTime).TotalMinutes > 20;
            }

            if (Boss.HP > 0 && !_disposed)
            {
                if (timeout)
                {
                    await _context.Channel.SendMessageAsync("20 minute timeout reached! Siege aborted!");
                }
                else
                {
                    var marbles = new StringBuilder();
                    foreach (var marble in Marbles)
                    {
                        marbles.AppendLine(marble.ToString(_context, false));
                    }

                    await _context.Channel.SendMessageAsync(embed: new EmbedBuilder()
                        .WithAuthor(Boss.Name, Boss.ImageUrl)
                        .WithColor(GetColor(_context))
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

        public async Task DealDamage(int damageToDeal)
        {
            Boss.HP -= damageToDeal;
            if (Boss.HP < 1)
            {
                await OnVictory();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            Active = false;
            _disposed = true;
            using (var marbleList = new StreamWriter($"Data{Path.DirectorySeparatorChar}{Id}.siege"))
            {
                marbleList.Write("");
            }

            _gamesService.SiegeInfo.TryRemove(Id, out _);
            if (disposing && Actions != null)
            {
                Actions.Wait();
                Actions.Dispose();
            }
        }

        public static Boss GetBoss(string searchTerm)
        {
            string json;
            using (var bosses = new StreamReader($"Resources{Path.DirectorySeparatorChar}Bosses.json"))
            {
                json = bosses.ReadToEnd();
            }

            var obj = new Dictionary<string, JObject>(JObject.Parse(json).ToObject<IDictionary<string, JObject>>()!,
                StringComparer.InvariantCultureIgnoreCase);
            var boss = Boss.Empty;
            if (searchTerm.Contains(' ') || (string.Compare(searchTerm[0].ToString(), searchTerm[0].ToString(), true) == 0))
            {
                searchTerm = searchTerm.ToPascalCase();
            }

            if (obj.ContainsKey(searchTerm))
            {
                boss = obj[searchTerm].ToObject<Boss>()!;
            }

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

        public async Task ItemAttack(JObject obj, int itemId,
int damage, bool consumable = false)
        {
            if (_disposed)
            {
                await _context.Channel.SendMessageAsync($":warning: | **{_context.User.Username}**, there is no currently ongoing Siege!");
                return;
            }

            var marble = Marbles.Find(m => m.Id == _context.User.Id);
            if (marble == null)
            {
                await _context.Channel.SendMessageAsync($":warning: | **{_context.User.Username}**, you aren't in this Siege!");
                return;
            }

            if (marble.HP == 0)
            {
                await _context.Channel.SendMessageAsync($":warning: | **{_context.User.Username}**, you are out and can no longer attack!");
                return;
            }

            if (DateTime.UtcNow.Subtract(marble.LastMoveUsed).TotalSeconds < 5)
            {
                await _context.Channel.SendMessageAsync($":warning: | **{_context.User.Username}**, you must wait for {GetDateString(marble.LastMoveUsed.Subtract(DateTime.UtcNow.AddSeconds(-5)))} until you can attack again!");
                return;
            }

            var user = GetUser(_context);
            var item = GetItem<Item>(itemId.ToString("000"));
            if (item.Id == 10 && marble.QefpedunCharmUsed)
            {
                await _context.Channel.SendMessageAsync($"**{_context.User.Username}**, you can only use the **{item.Name}** once per battle!");
                return;
            }

            damage = (int)Math.Round(damage * DamageMultiplier);
            Boss.HP -= damage;
            await _context.Channel.SendMessageAsync(embed: new EmbedBuilder()
                .AddField("Boss HP", $"**{Boss.HP}**/{Boss.MaxHP}")
                .WithAuthor(_context.User)
                .WithColor(GetColor(_context))
                .WithCurrentTimestamp()
                .WithDescription($"**{marble.Name}** used their **{item.Name}**, dealing **{damage}** damage to the boss!")
                .WithTitle(item.Name)
                .Build());

            marble.DamageDealt += damage;

            if (consumable)
            {
                user.Items[item.Id]--;
                user.NetWorth -= item.Price;
                WriteUsers(obj, _context.User, user);
            }

            if (item.Id == 10)
            {
                marble.QefpedunCharmUsed = true;
            }
        }

        public override string ToString() => $"[{Id}] {Boss.Name}: {Marbles.Count}";

        private async Task OnVictory()
        {
            if (_victoryCalled)
            {
                return;
            }

            _victoryCalled = true;
            var builder = new EmbedBuilder()
                .WithColor(GetColor(_context))
                .WithCurrentTimestamp()
                .WithTitle("Siege Victory!")
                .WithDescription($"**{Boss.Name}** has been defeated!");
            var obj = GetUsersObject();

            for (int i = 0; i < Marbles.Count; i++)
            {
                var marble = Marbles[i];
                var user = await GetUserAsync(_context, obj, marble.Id);
                var output = new StringBuilder();

                // Advance user's stage if necessary
                if (user.Stage == 1 && Boss.Name == "Destroyer" && ((marble.DamageDealt > 0 && marble.HP > 0) || marble.DamageDealt > 149))
                {
                    user.Stage = 2;
                    output.AppendLine($"**You have entered Stage II!** Much new content has been unlocked - see `mb/advice` for more info!");
                }
                else if (user.Stage == 2 && Boss.Name == "Overlord" && ((marble.DamageDealt > 0 && marble.HP > 0) || marble.DamageDealt > 149))
                {
                    user.Stage = 3;
                    output.AppendLine($"**You have entered Stage III!**");
                }

                int earnings = marble.DamageDealt + (marble.PowerUpHits * 50);
                if (DateTime.UtcNow.Subtract(user.LastSiegeWin).TotalHours > 6 && marble.DamageDealt > 0)
                {
                    output.AppendLine($"Damage dealt: {UnitOfMoney}**{marble.DamageDealt:n2}**");

                    if (marble.PowerUpHits > 0)
                    {
                        output.AppendLine($"Power-ups grabbed (x50): {UnitOfMoney}**{marble.PowerUpHits * 50:n2}**");
                    }

                    if (marble.HP > 0)
                    {
                        earnings += 200;
                        output.AppendLine($"Alive bonus: {UnitOfMoney}**{200:n2}**");
                        user.SiegeWins++;
                    }

                    if (user.Items.ContainsKey(83))
                    {
                        earnings *= 3;
                        output.AppendLine("Pendant bonus: x**3**");
                    }

                    if (output.Length > 0)
                    {
                        if (marble.HP > 0)
                        {
                            user.LastSiegeWin = DateTime.UtcNow;
                        }

                        if (Boss.Drops.Count > 0)
                        {
                            output.AppendLine("**Item Drops:**");
                        }

                        var dropPresent = false;
                        foreach (var itemDrops in Boss.Drops)
                        {
                            if (_randomService.Rand.Next(0, 100) < itemDrops.Chance)
                            {
                                dropPresent = true;

                                int noOfDrops;
                                if (itemDrops.MinCount == itemDrops.MaxCount)
                                {
                                    noOfDrops = itemDrops.MinCount;
                                }
                                else
                                {
                                    noOfDrops = _randomService.Rand.Next(itemDrops.MinCount, itemDrops.MaxCount + 1);
                                }

                                if (user.Items.ContainsKey(itemDrops.ItemId))
                                {
                                    user.Items[itemDrops.ItemId] += noOfDrops;
                                }
                                else
                                {
                                    user.Items.Add(itemDrops.ItemId, noOfDrops);
                                }

                                var item = GetItem<Item>(itemDrops.ItemId.ToString("000"));
                                user.NetWorth += item.Price * noOfDrops;
                                output.AppendLine($"`[{itemDrops.ItemId.ToString("000")}]` {item.Name} x{noOfDrops}");
                            }
                        }
                        if (!dropPresent)
                        {
                            output.AppendLine("None");
                        }

                        output.AppendLine($"__**Total: {UnitOfMoney}{earnings:n2}**__");
                        user.Balance += earnings;
                        user.NetWorth += earnings;
                        builder.AddField($"**{_context.Client.GetUser(marble.Id).Username}**'s earnings", output.ToString());
                        obj.Remove(marble.Id.ToString());
                        obj.Add(new JProperty(marble.Id.ToString(), JObject.FromObject(user)));
                    }
                }
            }
            await _context.Channel.SendMessageAsync(embed: builder.Build());
            WriteUsers(obj);
            Dispose(true);
        }

        public async Task WeaponAttack(SocketCommandContext _context, Weapon weapon)
        {
            if (_disposed)
            {
                await _context.Channel.SendMessageAsync("There is no currently ongoing Siege!");
                return;
            }

            var marble = Marbles.Find(m => m.Id == _context.User.Id);
            if (marble == null)
            {
                await _context.Channel.SendMessageAsync($"**{_context.User.Username}**, you aren't in this Siege!");
                return;
            }

            if (marble.HP == 0)
            {
                await _context.Channel.SendMessageAsync($"**{_context.User.Username}**, you are out and can no longer attack!");
                return;
            }

            var ammo = new Ammo();
            var user = GetUser(_context);

            if (weapon.Ammo.Length != 0)
            {
                for (int i = weapon.Ammo.Length - 1; i >= 0; i--)
                {
                    if (user.Items.ContainsKey(weapon.Ammo[i]) && user.Items[weapon.Ammo[i]] >= weapon.Hits)
                    {
                        ammo = GetItem<Ammo>(weapon.Ammo[i].ToString("000"));
                        break;
                    }
                }

                if (ammo.Id == 0)
                {
                    await _context.Channel.SendMessageAsync($"**{_context.User.Username}**, you do not have enough ammo for this item!");
                    return;
                }

                var obj = GetUsersObject();
                user.Items[ammo.Id] -= weapon.Hits;
                user.NetWorth -= ammo.Price * weapon.Hits;
                WriteUsers(obj, _context.User, user);
            }

            var builder = new EmbedBuilder()
                .WithAuthor(_context.User)
                .WithColor(GetColor(_context))
                .WithCurrentTimestamp()
                .WithTitle(weapon.Name);

            if (weapon.Hits == 1)
            {
                if (_randomService.Rand.Next(0, 100) < weapon.Accuracy)
                {
                    var damage = (int)Math.Round((weapon.Damage + (weapon.WarClass == WeaponClass.Ranged || weapon.WarClass == WeaponClass.Artillery ? ammo.Damage : 0.0))
                        * (_randomService.Rand.NextDouble() * 0.4 + 0.8) * 3d * DamageMultiplier);
                    await DealDamage(damage);
                    marble.DamageDealt += damage;
                    builder.WithDescription($"**{marble.Name}** used their **{weapon.Name}**, dealing **{damage}** damage to **{Boss.Name}**!");
                }
                else
                {
                    builder.WithDescription($"**{marble.Name}** used their **{weapon.Name}**! It missed!");
                }
            }
            else
            {
                var totalDamage = 0;
                for (int i = 0; i < weapon.Hits; i++)
                {
                    if (_randomService.Rand.Next(0, 100) < weapon.Accuracy)
                    {
                        var damage = (int)Math.Round((weapon.Damage + (weapon.WarClass == WeaponClass.Ranged || weapon.WarClass == WeaponClass.Artillery ? ammo.Damage : 0.0))
                            * (_randomService.Rand.NextDouble() * 0.4 + 0.8) * 3d * DamageMultiplier);
                        await DealDamage(damage);
                        totalDamage += damage;
                        builder.AddField($"Attack {i + 1}", $"**{damage}** damage to **{Boss.Name}**.");
                    }
                    else
                    {
                        builder.AddField($"Attack {i + 1}", "Missed!");
                    }

                    await Task.Delay(1);
                }
                marble.DamageDealt += totalDamage;
                builder.WithDescription($"**{marble.Name}** used their **{weapon.Name}**, dealing a total of **{totalDamage}** damage to **{Boss.Name}**!");
            }
            await _context.Channel.SendMessageAsync(embed: builder
                        .AddField("Boss HP", $"**{Boss.HP}**/{Boss.MaxHP}")
                        .Build());
        }

        public Siege(SocketCommandContext context, GamesService gamesService, RandomService randomService, IEnumerable<SiegeMarble> marbles)
        {
            _context = context;
            _gamesService = gamesService;
            _randomService = randomService;
            Id = _context.IsPrivate ? _context.User.Id : _context.Guild.Id;
            Marbles = marbles.ToList();
        }

        ~Siege() => Dispose(false);
    }
}
