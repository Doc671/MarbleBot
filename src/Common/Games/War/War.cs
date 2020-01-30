using Discord;
using Discord.Commands;
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
    public class War : IMarbleBotGame
    {
        public Task? Actions { get; set; }
        public IEnumerable<WarMarble> AllMarbles => Team1.Marbles.Union(Team2.Marbles);
        public ulong Id { get; set; }

        public WarTeam Team1 { get; set; }
        public WarTeam Team2 { get; set; }

        private readonly WarMarble? _aiMarble;
        private bool _disposed = false;
        private bool _endCalled = false;
        private readonly GamesService _gamesService;
        private readonly RandomService _randomService;

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

            _disposed = true;
            _gamesService.WarInfo.TryRemove(Id, out _);
            using (var marbleList = new StreamWriter($"Data{Path.DirectorySeparatorChar}{Id}.war"))
            {
                marbleList.Write("");
            }

            if (disposing && Actions != null)
            {
                Actions.Wait();
                Actions.Dispose();
            }
        }

        public async Task OnGameEnd(SocketCommandContext context)
        {
            if (_endCalled)
            {
                return;
            }

            _endCalled = true;
            var t1Total = Team1.Marbles.Sum(m => m.HP);
            var t2Total = Team2.Marbles.Sum(m => m.HP);
            var winningTeam = t1Total > t2Total ? Team1 : Team2;
            var builder = new EmbedBuilder()
                .WithColor(GetColor(context))
                .WithCurrentTimestamp()
                .WithTitle($"Team {winningTeam.Name} has defeated Team {(t1Total > t2Total ? Team2 : Team1).Name}!");
            var t1Output = new StringBuilder();
            var t2Output = new StringBuilder();

            foreach (var marble in Team1.Marbles)
            {
                var user = context.Client.GetUser(marble.Id);
                t1Output.AppendLine($"{marble.Name} (HP: **{marble.HP}**/{marble.MaxHP}, Wpn: {marble.Weapon}) [{user.Username}#{user.Discriminator}]");
            }

            foreach (var marble in Team2.Marbles)
            {
                var user = context.Client.GetUser(marble.Id);
                t2Output.AppendLine($"{marble.Name} (HP: **{marble.HP}**/{marble.MaxHP}, Wpn: {marble.Weapon}) [{user.Username}#{user.Discriminator}]");
            }

            builder.AddField($"Team {Team1.Name} Final Stats", t1Output.ToString())
                .AddField($"Team {Team2.Name} Final Stats", t2Output.ToString());

            var obj = GetUsersObject();
            foreach (var marble in winningTeam.Marbles)
            {
                var user = await GetUserAsync(context, obj, marble.Id);
                if (DateTime.UtcNow.Subtract(user.LastWarWin).TotalHours > 6 && marble.DamageDealt > 0)
                {
                    var output = new StringBuilder();
                    var earnings = marble.DamageDealt * 5;
                    output.AppendLine($"Damage dealt (x5): {UnitOfMoney}**{earnings:n2}**");
                    user.WarWins++;

                    if (marble.HP > 0)
                    {
                        earnings += 200;
                        output.AppendLine($"Alive bonus: {UnitOfMoney}**{200:n2}**");
                    }

                    if (user.Items.ContainsKey(83))
                    {
                        earnings *= 3;
                        output.AppendLine("Pendant bonus: x**3**");
                    }

                    if (output.Length > 0)
                    {
                        user.LastWarWin = DateTime.UtcNow;
                        user.Balance += earnings;
                        user.NetWorth += earnings;
                        output.AppendLine($"__**Total: {UnitOfMoney}{earnings:n2}**__");
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

        public async Task WarActions(SocketCommandContext context)
        {
            var startTime = DateTime.UtcNow;
            var timeout = false;
            do
            {
                await Task.Delay(7000);
                if (_disposed)
                {
                    return;
                }
                else if (DateTime.UtcNow.Subtract(startTime).TotalMinutes >= 10)
                {
                    timeout = true;
                    break;
                }
                else if (_aiMarble != null && _aiMarble.HP > 0)
                {
                    var enemyTeam = _aiMarble.Team == 1 ? Team2 : Team1;
                    var randMarble = enemyTeam.Marbles.ElementAt(_randomService.Rand.Next(0, enemyTeam.Marbles.Count));
                    if (_randomService.Rand.Next(0, 100) < _aiMarble.Weapon.Accuracy)
                    {
                        var dmg = (int)Math.Round(_aiMarble.Weapon.Damage * (1 + _aiMarble.DamageIncrease / 100d) * (1 - 0.2 * (randMarble.Shield == null ? Convert.ToDouble(randMarble.Shield!.Id == 63) : 1) * (0.5 + _randomService.Rand.NextDouble())));
                        randMarble.HP -= dmg;
                        await context.Channel.SendMessageAsync(embed: new EmbedBuilder()
                            .AddField("Remaining HP", $"**{randMarble.HP}**/{randMarble.MaxHP}")
                            .WithColor(GetColor(context))
                            .WithCurrentTimestamp()
                            .WithDescription($"**{_aiMarble.Name}** dealt **{dmg}** damage to **{randMarble.Name}** with **{_aiMarble.Weapon.Name}**!")
                            .WithTitle($"**{_aiMarble.Name}** attacks!")
                            .Build());
                    }
                    else
                    {
                        await context.Channel.SendMessageAsync(embed: new EmbedBuilder()
                            .WithColor(GetColor(context))
                            .WithCurrentTimestamp()
                            .WithDescription($"**{_aiMarble.Name}** tried to attack **{randMarble.Name}** but missed!")
                            .WithTitle($"**{_aiMarble.Name}** attacks!")
                            .Build());
                    }
                }
            }
            while (!timeout && !_disposed && !Team1.Marbles.All(m => m.HP == 0) && !Team2.Marbles.All(m => m.HP == 0));

            if (!timeout)
            {
                await OnGameEnd(context);
            }
            else
            {
                Dispose(true);
            }
        }

        public War(GamesService gamesService, RandomService randomService, ulong id, IEnumerable<WarMarble> team1Marbles, IEnumerable<WarMarble> team2Marbles, WarMarble? aiMarble, WarBoost team1Boost, WarBoost team2Boost)
        {
            _gamesService = gamesService;
            _randomService = randomService;

            Id = id;
            _aiMarble = aiMarble;

            // Decide team names
            var nameList = new List<string>();
            using (var teamNames = new StreamReader($"Resources{Path.DirectorySeparatorChar}WarTeamNames.txt"))
            {
                while (!teamNames.EndOfStream)
                {
                    nameList.Add(teamNames.ReadLine()!);
                }
            }

            string team2Name;
            var team1Name = nameList[_randomService.Rand.Next(0, nameList.Count)];
            do
            {
                team2Name = nameList[_randomService.Rand.Next(0, nameList.Count)];
            }
            while (string.Compare(team1Name, team2Name, false) == 0);

            Team1 = new WarTeam(team1Name, team1Marbles, team1Boost);
            Team2 = new WarTeam(team2Name, team2Marbles, team2Boost);
        }

        ~War() => Dispose(false);
    }
}
