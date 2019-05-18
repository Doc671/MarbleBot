using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MarbleBot.Core
{
    /// <summary> Represents a game of war. </summary>
    public class War : IDisposable
    {
        public Task Actions { get; set; }
        public IEnumerable<WarMarble> AllMarbles => Team1.Union(Team2);
        public List<WarMarble> Team1 { get; set; } = new List<WarMarble>();
        public string Team1Name { get; set; }
        public List<WarMarble> Team2 { get; set; } = new List<WarMarble>();
        public string Team2Name { get; set; }

        private bool _disposed = false;

        public void Dispose() => Dispose(true);

        private void Dispose(bool disposing)
        {
            if (_disposed) return;
            //if (disposing) Actions.Dispose();
            Team1 = null;
            Team2 = null;
            _disposed = true;
        }

        public async Task WarActions(SocketCommandContext context)
        {
            await context.Channel.SendMessageAsync("Hi!");
            /*var startTime = DateTime.UtcNow;
            var timeout = false;
            do
            {
                await Task.Delay(15000);
                if (DateTime.UtcNow.Subtract(startTime).TotalMinutes >= 10)
                {
                    timeout = true;
                    break;
                }
            } while (!timeout && Team1.Sum(m => m.HP) > 0 && Team2.Sum(m => m.HP) > 0);*/
            Dispose(true);
        }

        ~War() => Dispose(true);
    }
}
