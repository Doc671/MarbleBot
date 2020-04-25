using Discord;
using Discord.Commands;
using MarbleBot.Modules.Games.Services;
using MarbleBot.Services;
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
        public bool Active { get; private set; } = false;
        public int ActiveMoraleBoosts { get; set; }
        public Boss? Boss { get; set; } = null;
        public float DamageMultiplier { get; private set; } = 1f;
        public ulong Id { get; }
        public DateTime LastMorale { get; set; } = DateTime.MinValue;
        public List<SiegeMarble> Marbles { get; }
        public PowerUp PowerUp { get; set; }

        private readonly SocketCommandContext _context;
        private bool _disposed = false;
        private readonly GamesService _gamesService;
        private readonly RandomService _randomService;
        private bool _victoryCalled = false;

        private void AttackMarble(SiegeMarble marble, Attack attack, EmbedBuilder embedBuilder, ref bool attackMissed)
        {
            if (!(_randomService.Rand.Next(0, 100) > attack.Accuracy))
            {
                marble.DealDamage(attack.Damage);
                attackMissed = false;
                if (marble.Health < 1)
                {
                    marble.Health = 0;
                    DamageMultiplier += 0.2f;
                    embedBuilder.AddField($"**{marble.Name}** has been killed! :skull:", $"Health: **0**/{marble.MaxHealth}\nDamage Multiplier: **{DamageMultiplier}**");
                }
                else
                {
                    InflictStatusEffect(marble, attack.StatusEffect == marble.StatusEffect ? StatusEffect.None : attack.StatusEffect, embedBuilder);
                }
            }
        }

        // Separate task dealing with time-based boss responses
        private async Task BossActions()
        {
            var startTime = DateTime.UtcNow;
            bool timeout = false;

            await Task.Delay(15000);
            while (Boss!.Health > 0 && !timeout && Marbles.Any(m => m.Health != 0) && !_disposed)
            {
                var attack = Boss.Attacks[_randomService.Rand.Next(0, Boss.Attacks.Length)];
                var embedBuilder = new EmbedBuilder()
                    .WithAuthor(Boss.Name, Boss.ImageUrl)
                    .WithColor(GetColor(_context))
                    .WithCurrentTimestamp()
                    .WithDescription($"**{Boss.Name}** used **{attack.Name}**!")
                    .WithTitle($"WARNING: {attack.Name.ToUpper()} INBOUND! :warning:");

                bool attackMissed = true;
                var aliveMarbles = Marbles.Where(marble => marble.Health != 0);
                foreach (var marble in aliveMarbles)
                {
                    AttackMarble(marble, attack, embedBuilder, ref attackMissed);
                }

                if (attackMissed)
                {
                    embedBuilder.AddField("Missed!", "No-one got hurt!");
                }

                foreach (var marble in aliveMarbles)
                {
                    PerformStatusEffect(marble, embedBuilder);
                }

                // Wear off Morale Boost
                if (ActiveMoraleBoosts > 0 && (DateTime.UtcNow - LastMorale).TotalSeconds > 20)
                {
                    ActiveMoraleBoosts--;
                    embedBuilder.AddField("Morale Boost has worn off!",
                        $"The effects of a Morale Boost power-up have worn off! The damage multiplier is now **{DamageMultiplier}**!");
                }

                SpawnNewPowerUp(embedBuilder);

                await _context.Channel.SendMessageAsync(embed: embedBuilder.Build());

                await Task.Delay(15000);

                timeout = (DateTime.UtcNow - startTime).TotalMinutes > 20;
            }

            if (Boss.Health > 0 && !_disposed)
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
                        .WithDescription($"All the marbles died!\n**{Boss.Name}** won!\nFinal Health: **{Boss.Health}**/{Boss.MaxHealth}")
                        .AddField($"Fallen Marbles: **{Marbles.Count}**", marbles.ToString())
                        .WithThumbnailUrl(Boss.ImageUrl)
                        .WithTitle("Siege Failure! :skull_crossbones:")
                        .Build());
                }
                Dispose(true);
            }
        }

        public async Task DealDamageToBoss(int damageToDeal)
        {
            Boss!.Health -= damageToDeal;
            if (Boss.Health < 1)
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

            _gamesService.Sieges.TryRemove(Id, out _);
            if (disposing && Actions != null)
            {
                Actions.Wait();
                Actions.Dispose();
            }
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

        private void InflictStatusEffect(SiegeMarble marble, StatusEffect statusEffect, EmbedBuilder embedBuilder)
        {
            switch (statusEffect)
            {
                case StatusEffect.Chill:
                    marble.StatusEffect = StatusEffect.Chill;
                    embedBuilder.AddField($"**{marble.Name}** has been chilled! All attacks will deal half damage unless cured!", $"Health: **{marble.Health}**/{marble.MaxHealth}\nStatus Effect: **Chill**");
                    break;
                case StatusEffect.Doom:
                    marble.StatusEffect = StatusEffect.Doom;
                    embedBuilder.AddField($"**{marble.Name}** has been doomed and will die in ~45 seconds if not cured!", $"Health: **{marble.Health}**/{marble.MaxHealth}\nStatus Effect: **Doom**");
                    marble.DoomStart = DateTime.UtcNow;
                    break;
                case StatusEffect.Poison:
                    marble.StatusEffect = StatusEffect.Poison;
                    embedBuilder.AddField($"**{marble.Name}** has been poisoned and will lose Health every ~15 seconds until cured/at 1 Health!", $"Health: **{marble.Health}**/{marble.MaxHealth}\nStatus Effect: **Poison**");
                    break;
                case StatusEffect.Stun:
                    marble.StatusEffect = StatusEffect.Stun;
                    embedBuilder.AddField($"**{marble.Name}** has been stunned and cannot attack for the next ~15 seconds!", $"Health: **{marble.Health}**/{marble.MaxHealth}\nStatus Effect: **Stun**");
                    marble.LastStun = DateTime.UtcNow;
                    break;
                default: embedBuilder.AddField($"**{marble.Name}** has been damaged!", $"Health: **{marble.Health}**/{marble.MaxHealth}"); break;
            }
        }

        public async Task ItemAttack(int itemId, int damage, bool consumable = false)
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

            if (marble.Health == 0)
            {
                await _context.Channel.SendMessageAsync($":warning: | **{_context.User.Username}**, you are out and can no longer attack!");
                return;
            }

            double totalSeconds = (DateTime.UtcNow - marble.LastMoveUsed).TotalSeconds;
            if (totalSeconds < 5)
            {
                await _context.Channel.SendMessageAsync($":warning: | **{_context.User.Username}**, you must wait for {GetDateString(5 - totalSeconds):n2} until you can act again!");
                return;
            }

            var user = MarbleBotUser.Find(_context);
            var item = Item.Find<Item>(itemId.ToString("000"));
            if (item.Id == 10 && marble.QefpedunCharmUsed)
            {
                await _context.Channel.SendMessageAsync($"**{_context.User.Username}**, you can only use the **{item.Name}** once per battle!");
                return;
            }

            damage = (int)MathF.Round(damage * DamageMultiplier);
            await _context.Channel.SendMessageAsync(embed: new EmbedBuilder()
                .AddField("Boss Health", $"**{Math.Max(Boss!.Health - damage, 0)}**/{Boss.MaxHealth}")
                .WithAuthor(_context.User)
                .WithColor(GetColor(_context))
                .WithCurrentTimestamp()
                .WithDescription($"**{marble.Name}** used their **{item.Name}**, dealing **{damage}** damage to the boss!")
                .WithTitle($"{item.Name} :boom:")
                .Build());

            await DealDamageToBoss(damage);
            marble.DamageDealt += damage;
            marble.LastMoveUsed = DateTime.UtcNow;

            if (consumable)
            {
                user.Items[item.Id]--;
                user.NetWorth -= item.Price;
                MarbleBotUser.UpdateUser(user);
            }

            if (item.Id == 10)
            {
                marble.QefpedunCharmUsed = true;
            }
        }

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
                .WithTitle("Siege Victory! :trophy:")
                .WithDescription($"**{Boss!.Name}** has been defeated!");
            var usersDict = MarbleBotUser.GetUsers();

            for (int i = 0; i < Marbles.Count; i++)
            {
                var marble = Marbles[i];
                var user = await MarbleBotUser.FindAsync(_context, usersDict, marble.Id);
                var output = new StringBuilder();

                // Advance user's stage if necessary
                if (user.Stage == 1 && Boss.Name == "Destroyer" && ((marble.DamageDealt > 0 && marble.Health > 0) || marble.DamageDealt > 149))
                {
                    user.Stage = 2;
                    output.AppendLine($"**You have entered Stage II!** Much new content has been unlocked - see `mb/advice` for more info!");
                }
                else if (user.Stage == 2 && Boss.Name == "Overlord" && ((marble.DamageDealt > 0 && marble.Health > 0) || marble.DamageDealt > 149))
                {
                    user.Stage = 3;
                    output.AppendLine($"**You have entered Stage III!**");
                }

                int earnings = marble.DamageDealt + (marble.PowerUpHits * 50);
                if ((DateTime.UtcNow - user.LastSiegeWin).TotalHours > 6 && marble.DamageDealt > 0)
                {
                    output.AppendLine($"Damage dealt: {UnitOfMoney}**{marble.DamageDealt:n2}**");

                    if (marble.PowerUpHits > 0)
                    {
                        output.AppendLine($"Power-ups grabbed (x50): {UnitOfMoney}**{marble.PowerUpHits * 50:n2}**");
                    }

                    if (marble.Health > 0)
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
                        if (marble.Health > 0)
                        {
                            user.LastSiegeWin = DateTime.UtcNow;
                        }

                        if (Boss.Drops.Length > 0)
                        {
                            output.AppendLine("**Item Drops:**");
                            bool dropPresent = false;
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

                                    var item = Item.Find<Item>(itemDrops.ItemId.ToString("000"));
                                    user.NetWorth += item.Price * noOfDrops;
                                    output.AppendLine($"`[{itemDrops.ItemId:000}]` {item.Name} x{noOfDrops}");
                                }
                            }
                            if (!dropPresent)
                            {
                                output.AppendLine("None");
                            }
                        }

                        output.AppendLine($"__**Total: {UnitOfMoney}{earnings:n2}**__");
                        user.Balance += earnings;
                        user.NetWorth += earnings;
                        builder.AddField($"**{_context.Client.GetUser(marble.Id).Username}**'s earnings", output.ToString());
                        usersDict.Remove(marble.Id);
                        usersDict.Add(marble.Id, user);
                    }
                }
            }
            await _context.Channel.SendMessageAsync(embed: builder.Build());
            MarbleBotUser.UpdateUsers(usersDict);
            Dispose(true);
        }

        private void PerformStatusEffect(SiegeMarble marble, EmbedBuilder embedBuilder)
        {
            switch (marble.StatusEffect)
            {
                case StatusEffect.Doom:
                    if ((DateTime.UtcNow - marble.DoomStart).TotalSeconds > 45)
                    {
                        marble.Health = 0;
                        embedBuilder.AddField($"**{marble.Name}** has died of Doom!", $"Health: **0**/{marble.MaxHealth}\nDamage Multiplier: **{DamageMultiplier}**");
                    }
                    break;
                case StatusEffect.Poison:
                    if ((DateTime.UtcNow - marble.LastPoisonTick).TotalSeconds > 15)
                    {
                        if (marble.Health < 1)
                        {
                            break;
                        }

                        marble.Health -= (int)MathF.Round(marble.MaxHealth / 10f);
                        marble.LastPoisonTick = DateTime.UtcNow;
                        if (marble.Health < 2)
                        {
                            marble.Health = 1;
                            marble.StatusEffect = StatusEffect.None;
                        }
                        embedBuilder.AddField($"**{marble.Name}** has taken Poison damage!", $"Health: **{marble.Health}**/{marble.MaxHealth}");
                    }
                    marble.LastPoisonTick = DateTime.UtcNow;
                    break;
            }
        }

        private void SpawnNewPowerUp(EmbedBuilder embedBuilder)
        {
            if (PowerUp == PowerUp.None)
            {
                switch ((PowerUp)_randomService.Rand.Next(0, 8))
                {
                    case PowerUp.Clone:
                        PowerUp = PowerUp.Clone;
                        embedBuilder.AddField("Power-up spawned!", "A **Clone** power-up has spawned in the arena! Use `mb/siege grab` to try and activate it!")
                            .WithThumbnailUrl(GetPowerUpImageUrl(PowerUp));
                        break;
                    case PowerUp.Cure:
                        if (Marbles.Any(m => m.StatusEffect != StatusEffect.None))
                        {
                            PowerUp = PowerUp.Cure;
                            embedBuilder.AddField("Power-up spawned!", "A **Cure** power-up has spawned in the arena! Use `mb/siege grab` to try and activate it!")
                                .WithThumbnailUrl(GetPowerUpImageUrl(PowerUp));
                        }
                        break;
                    case PowerUp.MoraleBoost:
                        PowerUp = PowerUp.MoraleBoost;
                        embedBuilder.AddField("Power-up spawned!", "A **Morale Boost** power-up has spawned in the arena! Use `mb/siege grab` to try and activate it!")
                            .WithThumbnailUrl(GetPowerUpImageUrl(PowerUp));
                        break;
                    case PowerUp.Summon:
                        PowerUp = PowerUp.Summon;
                        embedBuilder.AddField("Power-up spawned!", "A **Summon** power-up has spawned in the arena! Use `mb/siege grab` to try and activate it!")
                            .WithThumbnailUrl(GetPowerUpImageUrl(PowerUp));
                        break;
                }
            }
        }

        public void Start()
        {
            Actions = Task.Run(async () => { await BossActions(); });
            Active = true;
        }

        public override string ToString() => $"[{Id}] {Boss?.Name}: {Marbles.Count}";

        public async Task WeaponAttack(Weapon weapon)
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

            if (marble.Health == 0)
            {
                await _context.Channel.SendMessageAsync($"**{_context.User.Username}**, you are out and can no longer attack!");
                return;
            }

            double totalSeconds = (DateTime.UtcNow - marble.LastMoveUsed).TotalSeconds;
            if (totalSeconds < 5)
            {
                await _context.Channel.SendMessageAsync($":warning: | **{_context.User.Username}**, you must wait for {GetDateString(5 - totalSeconds):n2} until you can act again!");
                return;
            }

            var ammo = new Ammo();
            var user = MarbleBotUser.Find(_context);

            if (weapon.Ammo.Length != 0)
            {
                for (int i = weapon.Ammo.Length - 1; i >= 0; i--)
                {
                    if (user.Items.ContainsKey(weapon.Ammo[i]) && user.Items[weapon.Ammo[i]] >= weapon.Hits)
                    {
                        ammo = Item.Find<Ammo>(weapon.Ammo[i].ToString("000"));
                        break;
                    }
                }

                if (ammo.Id == 0)
                {
                    await _context.Channel.SendMessageAsync($"**{_context.User.Username}**, you do not have enough ammo for this item!");
                    return;
                }

                user.Items[ammo.Id] -= weapon.Hits;
                user.NetWorth -= ammo.Price * weapon.Hits;
                MarbleBotUser.UpdateUser(user);
            }

            var builder = new EmbedBuilder()
                .WithAuthor(_context.User)
                .WithColor(GetColor(_context))
                .WithCurrentTimestamp()
                .WithTitle($"{weapon.Name} :boom:");

            int totalDamage = 0;
            int damage;
            if (weapon.Hits == 1)
            {
                if (_randomService.Rand.Next(0, 100) < weapon.Accuracy)
                {
                    totalDamage = (int)Math.Round((weapon.Damage + (weapon.WeaponClass == WeaponClass.Ranged || weapon.WeaponClass == WeaponClass.Artillery ? ammo.Damage : 0.0))
                        * (_randomService.Rand.NextDouble() * 0.4 + 0.8) * 3d * DamageMultiplier);
                    marble.LastMoveUsed = DateTime.UtcNow;
                    builder.WithDescription($"**{marble.Name}** used their **{weapon.Name}**, dealing **{totalDamage}** damage to **{Boss!.Name}**!");
                }
                else
                {
                    builder.WithDescription($"**{marble.Name}** used their **{weapon.Name}**! It missed!");
                }
            }
            else
            {
                for (int i = 0; i < weapon.Hits; i++)
                {
                    if (_randomService.Rand.Next(0, 100) < weapon.Accuracy)
                    {
                        damage = (int)Math.Round((weapon.Damage + (weapon.WeaponClass == WeaponClass.Ranged || weapon.WeaponClass == WeaponClass.Artillery ? ammo.Damage : 0.0))
                            * (_randomService.Rand.NextDouble() * 0.4 + 0.8) * 3d * DamageMultiplier);
                        totalDamage += damage;
                        builder.AddField($"Attack {i + 1}", $"**{damage}** damage to **{Boss!.Name}**.");
                    }
                    else
                    {
                        builder.AddField($"Attack {i + 1}", "Missed!");
                    }

                    await Task.Delay(1);
                }
                marble.DamageDealt += totalDamage;
                builder.WithDescription($"**{marble.Name}** used their **{weapon.Name}**, dealing a total of **{totalDamage}** damage to **{Boss!.Name}**!");

                if (totalDamage != 0)
                {
                    marble.LastMoveUsed = DateTime.UtcNow;
                }
            }

            await _context.Channel.SendMessageAsync(embed: builder
                        .AddField("Boss Health", $"**{Math.Max(Boss!.Health - totalDamage, 0)}**/{Boss.MaxHealth}")
                        .Build());

            await DealDamageToBoss(totalDamage);
            marble.DamageDealt += totalDamage;
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
