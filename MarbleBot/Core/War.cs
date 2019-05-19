using Discord;
using Discord.Commands;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarbleBot.Core
{
    /// <summary> Represents a game of war. </summary>
    public class War : IDisposable
    {
        public Task Actions { get; set; }
        public IEnumerable<WarMarble> AllMarbles => Team1.Union(Team2);
        public bool EndCalled { get; set; }
        public ulong Id { get; set; }
        public List<WarMarble> Team1 { get; set; } = new List<WarMarble>();
        public string Team1Name { get; set; }
        public List<WarMarble> Team2 { get; set; } = new List<WarMarble>();
        public string Team2Name { get; set; }

        private WarMarble _aiMarble;
        private bool _aiMarblePresent = false;
        private bool _disposed = false;

        public void Dispose() => Dispose(true);

        private void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing) Actions.Dispose();
            Global.WarInfo.Remove(Id);
            using (var marbleList = new StreamWriter($"Data\\{Id}war.csv", false))
                marbleList.Write("");
            Team1 = null;
            Team2 = null;
            _disposed = true;
        }

        public void SetAIMarble(WarMarble aiMarble)
        {
            _aiMarble = aiMarble;
            _aiMarblePresent = true;
        }

        public async Task WarActions(SocketCommandContext context)
        {
            var startTime = DateTime.UtcNow;
            var timeout = false;
            while (!timeout && Team1.Sum(m => m.HP) > 0 && Team2.Sum(m => m.HP) > 0)
            {
                await Task.Delay(7000);
                if (DateTime.UtcNow.Subtract(startTime).TotalMinutes >= 10)
                {
                    timeout = true;
                    break;
                }
                else if (_aiMarblePresent)
                {
                    var enemyTeam = _aiMarble.Team == 1 ? Team2 : Team1;
                    var randMarble = enemyTeam[Global.Rand.Next(0, enemyTeam.Count)];
                    var dmg = (int)Math.Round(_aiMarble.Weapon.Damage * (1 + _aiMarble.DamageIncrease / 100d) * (1 - 0.2 * Convert.ToDouble(_aiMarble.Shield.Id == 63)));
                    randMarble.HP -= dmg;
                    await context.Channel.SendMessageAsync(embed: new EmbedBuilder()
                        .WithColor(MarbleBotModule.GetColor(context))
                        .WithCurrentTimestamp()
                        .WithDescription($"**{_aiMarble.Name}** dealt **{dmg}** damage to **{randMarble.Name}** with **{_aiMarble.Weapon.Name}**!")
                        .WithTitle($"**{_aiMarble.Name}** attacks!")
                        .Build());
                }
            }
            if (!timeout) await WarEndAsync(context);
            Dispose(true);
        }

        public async Task WarEndAsync(SocketCommandContext context)
        {
            if (EndCalled) return;
            var t1Total = Team1.Sum(m => m.HP);
            var t2Total = Team2.Sum(m => m.HP);
            var winningTeam = t1Total > t2Total ? Team1 : Team2;
            var builder = new EmbedBuilder()
                .WithColor(MarbleBotModule.GetColor(context))
                .WithCurrentTimestamp()
                .WithTitle($"Team {(t1Total > t2Total ? Team1Name : Team2Name)} has defeated Team {(t1Total > t2Total ? Team2Name : Team2Name)}!");
            var obj = MarbleBotModule.GetUsersObj();
            foreach (var marble in winningTeam)
            {
                var user = MarbleBotModule.GetUser(context, obj, marble.Id);
                if (DateTime.UtcNow.Subtract(user.LastWarWin).TotalHours > 6)
                {
                    var output = new StringBuilder();
                    var earnings = 0;
                    if (marble.DamageDealt > 0)
                    {
                        earnings = marble.DamageDealt * 5;
                        output.AppendLine($"Damage dealt (x5): {Global.UoM}**{marble.DamageDealt:n}**");
                        user.WarWins++;
                    }
                    else break;
                    if (marble.HP > 0)
                    {
                        earnings += 200;
                        output.AppendLine($"Alive bonus: {Global.UoM}**{200:n}**");
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
                        output.AppendLine($"__**Total: {Global.UoM}{earnings:n}**__");
                        builder.AddField($"**{context.Client.GetUser(marble.Id).Username}**'s earnings", output.ToString());
                        obj.Remove(marble.Id.ToString());
                        obj.Add(new JProperty(marble.Id.ToString(), JObject.FromObject(user)));
                    }
                }
            }
            await context.Channel.SendMessageAsync(embed: builder.Build());
            MarbleBotModule.WriteUsers(obj);
            Dispose(true);
        }

        ~War() => Dispose(true);
    }
}
