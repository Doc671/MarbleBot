using System;
using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace MarbleBot
{
    class Program
    {
        static void Main(string[] args)
        => new Program().StartAsync().GetAwaiter().GetResult();

        private DiscordSocketClient _client;

        private CommandHandler _handler;

        public async Task StartAsync()
        {
            _client = new DiscordSocketClient();

            await _client.LoginAsync(TokenType.Bot, "Mjg2MjI4NTI2MjM0MDc1MTM2.DVh1NA.btjvCAsMN_Cx9ZY5suKKuawXKG4");

            await _client.StartAsync();

            _handler = new CommandHandler(_client);

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
