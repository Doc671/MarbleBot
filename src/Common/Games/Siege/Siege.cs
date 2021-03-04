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
using System.Timers;
using static MarbleBot.Modules.MarbleBotModule;

namespace MarbleBot.Common.Games.Siege
{
    public class Siege
    {
        public bool Active { get; private set; }

        private int _activeMoraleBoosts;
        public int ActiveMoraleBoosts
        {
            get => _activeMoraleBoosts;
            set
            {
                DamageMultiplier *= MathF.Pow(2, value - _activeMoraleBoosts);
                _activeMoraleBoosts = value;
            }
        }

        public Boss Boss { get; }
        public float DamageMultiplier { get; private set; } = 1f;
        public DateTime LastMorale { get; set; } = DateTime.MinValue;
        public List<SiegeMarble> Marbles { get; set; }
        public PowerUp PowerUp { get; set; }

        private readonly SocketCommandContext _context;
        private readonly GamesService _gamesService;
        private readonly ulong _id;
        private readonly RandomService _randomService;
        private readonly Timer _timer = new(15000);
        private bool _finished;
        private DateTime _startTime;
        private bool _victoryCalled;

        public Siege(SocketCommandContext context, GamesService gamesService, RandomService randomService, Boss boss,
            IEnumerable<SiegeMarble> marbles)
        {
            _context = context;
            _gamesService = gamesService;
            _randomService = randomService;
            _id = _context.IsPrivate ? _context.User.Id : _context.Guild.Id;
            Boss = boss;
            Marbles = marbles.ToList();

            _timer.Elapsed += Timer_Elapsed;
        }

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
                    embedBuilder.AddField($"**{marble.Name}** has been killed! :skull:",
                        $"Health: **0**/{marble.MaxHealth}\nDamage Multiplier: **{DamageMultiplier}**");
                }
                else
                {
                    InflictStatusEffect(marble,
                        attack.StatusEffect == marble.StatusEffect ? StatusEffect.None : attack.StatusEffect,
                        embedBuilder);
                }
            }
        }

        public async Task DealDamageToBoss(int damageToDeal)
        {
            Boss!.Health -= damageToDeal;
            if (Boss.Health < 1)
            {
                _timer.Stop();
                await OnVictory();
            }
        }

        public void Finalise()
        {
            if (_finished)
            {
                return;
            }

            _finished = true;
            _gamesService.Sieges.TryRemove(_id, out _);
            using var marbleList = new StreamWriter($"Data{Path.DirectorySeparatorChar}{_id}.siege");
            marbleList.Write("");
        }

        public static string GetPowerUpImageUrl(PowerUp powerUp)
        {
            return powerUp switch
            {
                PowerUp.Clone =>
                    "https://cdn.discordapp.com/attachments/296376584238137355/541373091495018496/PUClone.png",
                PowerUp.Cure =>
                    "https://cdn.discordapp.com/attachments/296376584238137355/541373094724501524/PUCure.png",
                PowerUp.Heal =>
                    "https://cdn.discordapp.com/attachments/296376584238137355/541373096238514202/PUHeal.png",
                PowerUp.MoraleBoost =>
                    "https://cdn.discordapp.com/attachments/296376584238137355/541373099090903070/PUMoraleBoost.png",
                PowerUp.Overclock =>
                    "https://cdn.discordapp.com/attachments/296376584238137355/541373101649428480/PUOverclock.png",
                PowerUp.Summon =>
                    "https://cdn.discordapp.com/attachments/296376584238137355/541373120129531939/PUSummon.png",
                _ => ""
            };
        }

        private static void InflictStatusEffect(SiegeMarble marble, StatusEffect statusEffect, EmbedBuilder embedBuilder)
        {
            switch (statusEffect)
            {
                case StatusEffect.Chill:
                    marble.StatusEffect = StatusEffect.Chill;
                    embedBuilder.AddField($"**{marble.Name}** has been chilled! All attacks will deal half damage unless cured!",
                        $"Health: **{marble.Health}**/{marble.MaxHealth}\nStatus Effect: **Chill** :snowflake:");
                    break;
                case StatusEffect.Doom:
                    marble.StatusEffect = StatusEffect.Doom;
                    embedBuilder.AddField($"**{marble.Name}** has been doomed and will die in ~45 seconds if not cured!",
                        $"Health: **{marble.Health}**/{marble.MaxHealth}\nStatus Effect: **Doom** :skull_crossbones:");
                    marble.DoomStart = DateTime.UtcNow;
                    break;
                case StatusEffect.Poison:
                    marble.StatusEffect = StatusEffect.Poison;
                    embedBuilder.AddField($"**{marble.Name}** has been poisoned and will lose health every ~15 seconds until cured/at 1 Health!",
                        $"Health: **{marble.Health}**/{marble.MaxHealth}\nStatus Effect: **Poison** :nauseated_face:");
                    break;
                case StatusEffect.Stun:
                    marble.StatusEffect = StatusEffect.Stun;
                    embedBuilder.AddField($"**{marble.Name}** has been stunned and cannot attack for the next ~15 seconds!",
                        $"Health: **{marble.Health}**/{marble.MaxHealth}\nStatus Effect: **Stun** :zap:");
                    marble.LastStun = DateTime.UtcNow;
                    break;
                default:
                    embedBuilder.AddField($"**{marble.Name}** has been damaged!",
                        $"Health: **{marble.Health}**/{marble.MaxHealth}");
                    break;
            }
        }

        public async Task ItemAttack(int itemId, int damage, bool consumable = false)
        {
            if (_finished)
            {
                await _context.Channel.SendMessageAsync($":warning: | **{_context.User.Username}**, there is no currently ongoing Siege!");
                return;
            }

            SiegeMarble? marble = Marbles.Find(m => m.Id == _context.User.Id);
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
                await _context.Channel.SendMessageAsync($":warning: | **{_context.User.Username}**, you must wait for {GetDateString(5 - totalSeconds)} until you can act again!");
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

        private async Task OnFailure()
        {
            var marbles = new StringBuilder();
            foreach (var marble in Marbles)
            {
                marbles.AppendLine(marble.ToString(_context, false));
            }

            await _context.Channel.SendMessageAsync(embed: new EmbedBuilder()
                .WithAuthor(Boss.Name, Boss.ImageUrl)
                .WithColor(GetColor(_context))
                .WithDescription($"All the marbles died!\n**{Boss.Name}** won!\nFinal Health: **{Boss.Health}**/{Boss.MaxHealth}")
                .AddField($"Fallen Marbles: **{Marbles.Count}**", marbles.ToString())
                .WithThumbnailUrl(Boss.ImageUrl)
                .WithTitle("Siege Failure! :skull_crossbones:")
                .Build());

            Finalise();
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
                .WithTitle("Siege Victory! :trophy:")
                .WithDescription($"**{Boss!.Name}** has been defeated!");
            var usersDict = MarbleBotUser.GetUsers();

            foreach (SiegeMarble marble in Marbles)
            {
                var user = await MarbleBotUser.FindAsync(_context, usersDict, marble.Id);
                var output = new StringBuilder();

                // Advance user's stage if necessary
                if (user.Stage == 1 && Boss.Name == "Destroyer" &&
                    (marble.DamageDealt > 0 && marble.Health > 0 || marble.DamageDealt > 149))
                {
                    user.Stage = 2;
                    output.AppendLine("**You have entered Stage II!** Much new content has been unlocked - see `mb/advice` for more info!");
                }
                else if (user.Stage == 2 && Boss.Name == "Overlord" &&
                         (marble.DamageDealt > 0 && marble.Health > 0 || marble.DamageDealt > 149))
                {
                    user.Stage = 3;
                    output.AppendLine("**You have entered Stage III!**");
                }

                int earnings = marble.DamageDealt + marble.PowerUpHits * 50;
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
                            foreach (BossDropInfo itemDrops in Boss.Drops)
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
                                        noOfDrops = _randomService.Rand.Next(itemDrops.MinCount,
                                            itemDrops.MaxCount + 1);
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
                        builder.AddField($"**{_context.Client.GetUser(marble.Id).Username}**'s earnings",
                            output.ToString());
                        usersDict.Remove(marble.Id);
                        usersDict.Add(marble.Id, user);
                    }
                }
            }

            await _context.Channel.SendMessageAsync(embed: builder.Build());
            MarbleBotUser.UpdateUsers(usersDict);
            Finalise();
        }

        private void PerformStatusEffect(SiegeMarble marble, EmbedBuilder embedBuilder)
        {
            switch (marble.StatusEffect)
            {
                case StatusEffect.Doom:
                    if ((DateTime.UtcNow - marble.DoomStart).TotalSeconds > 45)
                    {
                        marble.Health = 0;
                        embedBuilder.AddField($"**{marble.Name}** has died of Doom!",
                            $"Health: **0**/{marble.MaxHealth}\nDamage Multiplier: **{DamageMultiplier}**");
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

                        embedBuilder.AddField($"**{marble.Name}** has taken Poison damage!",
                            $"Health: **{marble.Health}**/{marble.MaxHealth}");
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
                        embedBuilder.AddField("Power-up spawned!",
                                "A **Clone** power-up has spawned in the arena! Use `mb/siege grab` to try and activate it!")
                            .WithThumbnailUrl(GetPowerUpImageUrl(PowerUp));
                        break;
                    case PowerUp.Cure:
                        if (Marbles.Any(m => m.StatusEffect != StatusEffect.None))
                        {
                            PowerUp = PowerUp.Cure;
                            embedBuilder.AddField("Power-up spawned!",
                                    "A **Cure** power-up has spawned in the arena! Use `mb/siege grab` to try and activate it!")
                                .WithThumbnailUrl(GetPowerUpImageUrl(PowerUp));
                        }

                        break;
                    case PowerUp.MoraleBoost:
                        PowerUp = PowerUp.MoraleBoost;
                        embedBuilder.AddField("Power-up spawned!",
                                "A **Morale Boost** power-up has spawned in the arena! Use `mb/siege grab` to try and activate it!")
                            .WithThumbnailUrl(GetPowerUpImageUrl(PowerUp));
                        break;
                    case PowerUp.Summon:
                        PowerUp = PowerUp.Summon;
                        embedBuilder.AddField("Power-up spawned!",
                                "A **Summon** power-up has spawned in the arena! Use `mb/siege grab` to try and activate it!")
                            .WithThumbnailUrl(GetPowerUpImageUrl(PowerUp));
                        break;
                }
            }
        }

        public void Start()
        {
            Active = true;
            _startTime = DateTime.UtcNow;
            _timer.Start();
        }

        public void Stop()
        {
            Finalise();
        }

        private async void Timer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            if (_finished)
            {
                _timer.Stop();
                return;
            }

            Attack attack = Boss.Attacks[_randomService.Rand.Next(0, Boss.Attacks.Length)];
            var embedBuilder = new EmbedBuilder()
                .WithAuthor(Boss.Name, Boss.ImageUrl)
                .WithColor(GetColor(_context))
                .WithDescription($"**{Boss.Name}** used **{attack.Name}**!")
                .WithTitle($"WARNING: {attack.Name.ToUpper()} INBOUND! :warning:");

            bool attackMissed = true;
            var aliveMarbles = Marbles.Where(marble => marble.Health != 0).ToArray();
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
                    $"The effects of a Morale Boost power-up have worn off! The damage multiplier is now **{DamageMultiplier:n1}**!");
            }

            SpawnNewPowerUp(embedBuilder);

            await _context.Channel.SendMessageAsync(embed: embedBuilder.Build());

            if ((DateTime.UtcNow - _startTime).TotalMinutes > 20)
            {
                _timer.Stop();
                await _context.Channel.SendMessageAsync("20 minute timeout reached! Siege aborted!");
                Finalise();
            }
            else if (Marbles.Sum(marble => marble.Health) == 0)
            {
                _timer.Stop();
                await OnFailure();
            }
        }

        public override string ToString()
        {
            return $"[{_id}] {Boss.Name}: {Marbles.Count}";
        }

        public async Task WeaponAttack(Weapon weapon)
        {
            if (_finished)
            {
                await _context.Channel.SendMessageAsync("There is no currently ongoing Siege!");
                return;
            }

            SiegeMarble? marble = Marbles.Find(m => m.Id == _context.User.Id);
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
                await _context.Channel.SendMessageAsync($":warning: | **{_context.User.Username}**, you must wait for {GetDateString(5 - totalSeconds)} until you can act again!");
                return;
            }

            Ammo? ammo = null;
            var user = MarbleBotUser.Find(_context);

            if (weapon.Ammo.Length != 0)
            {
                ammo = user.GetAmmo(weapon);

                if (ammo == null)
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
                .WithTitle($"{weapon.Name} :boom:");

            int totalDamage = 0;
            int damage;
            if (weapon.Hits == 1)
            {
                if (_randomService.Rand.Next(0, 100) < weapon.Accuracy)
                {
                    totalDamage = CalculateWeaponDamage(weapon, ammo);
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
                        damage = CalculateWeaponDamage(weapon, ammo);
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

        private int CalculateWeaponDamage(Weapon weapon, Ammo? ammo)
        {
            double ammoIncrease = weapon.WeaponClass is WeaponClass.Ranged or WeaponClass.Artillery ? ammo!.Damage : 0.0;
            double randomMultiplier = _randomService.Rand.NextDouble() * 0.4 + 0.8;
            return (int)Math.Round((weapon.Damage + ammoIncrease) * randomMultiplier * 3d * DamageMultiplier);
        }
    }
}
