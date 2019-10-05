using Discord;
using Discord.Commands;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static MarbleBot.Modules.MarbleBotModule;

namespace MarbleBot.Core
{
    /// <summary> Represents a game of war. </summary>
    public class War : IDisposable
    {
        public Task Actions { get; set; }
        public IEnumerable<WarMarble> AllMarbles { get; }
        public ulong Id { get; set; }
        public IReadOnlyCollection<WarMarble> Team1 { get; set; }
        public string Team1Name { get; set; }
        public IReadOnlyCollection<WarMarble> Team2 { get; set; }
        public string Team2Name { get; set; }

        private readonly WarMarble _aiMarble;
        private readonly bool _aiMarblePresent = false;
        private bool _disposed = false;
        private bool _endCalled = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;
            Global.WarInfo.TryRemove(Id, out _);
            using (var marbleList = new StreamWriter($"Data{Path.DirectorySeparatorChar}{Id}war.csv", false))
                marbleList.Write("");
            if (disposing && Actions != null)
            {
                Actions.Wait();
                Actions.Dispose();
            }
        }

        public async Task End(SocketCommandContext context)
        {
            if (_endCalled) return;
            _endCalled = true;
            var t1Total = Team1.Sum(m => m.HP);
            var t2Total = Team2.Sum(m => m.HP);
            var winningTeam = t1Total > t2Total ? Team1 : Team2;
            var builder = new EmbedBuilder()
                .WithColor(GetColor(context))
                .WithCurrentTimestamp()
                .WithTitle($"Team {(t1Total > t2Total ? Team1Name : Team2Name)} has defeated Team {(t1Total > t2Total ? Team2Name : Team1Name)}!");
            var t1Output = new StringBuilder();
            var t2Output = new StringBuilder();

            foreach (var marble in Team1)
            {
                var user = context.Client.GetUser(marble.Id);
                t1Output.AppendLine($"{marble.Name} (HP: **{marble.HP}**/{marble.MaxHP}, Wpn: {marble.Weapon}) [{user.Username}#{user.Discriminator}]");
            }

            foreach (var marble in Team2)
            {
                var user = context.Client.GetUser(marble.Id);
                t2Output.AppendLine($"{marble.Name} (HP: **{marble.HP}**/{marble.MaxHP}, Wpn: {marble.Weapon}) [{user.Username}#{user.Discriminator}]");
            }

            builder.AddField($"Team {Team1Name} Final Stats", t1Output.ToString())
                .AddField($"Team {Team2Name} Final Stats", t2Output.ToString());

            var obj = GetUsersObject();
            foreach (var marble in winningTeam)
            {
                var user = GetUser(context, obj, marble.Id);
                if (DateTime.UtcNow.Subtract(user.LastWarWin).TotalHours > 6)
                {
                    var output = new StringBuilder();
                    var earnings = 0;
                    if (marble.DamageDealt > 0)
                    {
                        earnings = marble.DamageDealt * 5;
                        output.AppendLine($"Damage dealt (x5): {Global.UoM}**{earnings:n2}**");
                        user.WarWins++;
                    }
                    else break;

                    if (marble.HP > 0)
                    {
                        earnings += 200;
                        output.AppendLine($"Alive bonus: {Global.UoM}**{200:n2}**");
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
                        output.AppendLine($"__**Total: {Global.UoM}{earnings:n2}**__");
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
                if (_disposed) return;
                else if (DateTime.UtcNow.Subtract(startTime).TotalMinutes >= 10)
                {
                    timeout = true;
                    break;
                }
                else if (_aiMarblePresent && _aiMarble.HP > 0)
                {
                    var enemyTeam = _aiMarble.Team == 1 ? Team2 : Team1;
                    var randMarble = enemyTeam.ElementAt(Global.Rand.Next(0, enemyTeam.Count));
                    if (Global.Rand.Next(0, 100) < _aiMarble.Weapon.Accuracy)
                    {
                        var dmg = (int)Math.Round(_aiMarble.Weapon.Damage * (1 + _aiMarble.DamageIncrease / 100d) * (1 - 0.2 * Convert.ToDouble(randMarble.Shield.Id == 63) * (0.5 + Global.Rand.NextDouble())));
                        randMarble.HP -= dmg;
                        await context.Channel.SendMessageAsync(embed: new EmbedBuilder()
                            .AddField("Remaining HP", $"**{randMarble.HP}**/{randMarble.MaxHP}")
                            .WithColor(GetColor(context))
                            .WithCurrentTimestamp()
                            .WithDescription($"**{_aiMarble.Name}** dealt **{dmg}** damage to **{randMarble.Name}** with **{_aiMarble.Weapon.Name}**!")
                            .WithTitle($"**{_aiMarble.Name}** attacks!")
                            .Build());
                    }
                    else await context.Channel.SendMessageAsync(embed: new EmbedBuilder()
                          .WithColor(GetColor(context))
                          .WithCurrentTimestamp()
                          .WithDescription($"**{_aiMarble.Name}** tried to attack **{randMarble.Name}** but missed!")
                          .WithTitle($"**{_aiMarble.Name}** attacks!")
                          .Build());
                }
            }
            while (!timeout && !_disposed && !Team1.All(m => m.HP == 0) && !Team2.All(m => m.HP == 0));
            if (!timeout) await End(context);
            else Dispose(true);
        }

        public War(ulong id, IEnumerable<WarMarble> team1, IEnumerable<WarMarble> team2, WarMarble aiMarble)
        {
            Id = id;
            _aiMarble = aiMarble;
            _aiMarblePresent = aiMarble != null;
            Team1 = team1.ToList();
            Team2 = team2.ToList();
            AllMarbles = Team1.Union(Team2);

            // Decide team names
            var nameList = new List<string>();
            using (var teamNames = new StreamReader($"Resources{Path.DirectorySeparatorChar}WarTeamNames.txt"))
            {
                while (!teamNames.EndOfStream)
                    nameList.Add(teamNames.ReadLine());
            }

            Team1Name = nameList[Global.Rand.Next(0, nameList.Count)];
            do Team2Name = nameList[Global.Rand.Next(0, nameList.Count)];
            while (string.Compare(Team1Name, Team2Name, false) == 0);
        }

        ~War() => Dispose(false);
    }
}
