using System;
using System.IO;
using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace MarbleBot
{
    class Program
    {
        static void Main(string[] args)
        => new Program().StartAsync().GetAwaiter().GetResult();

        public static DiscordSocketClient _client;

        private CommandHandler _handler;

        public async Task StartAsync()
        {
            Console.Title = "MarbleBot";
            Global.StartTime = DateTime.UtcNow;
            _client = new DiscordSocketClient();

            string token = "";
            using (StreamReader stream = new StreamReader("C:/Folder/MBT.txt")) {
                while (!stream.EndOfStream) token = stream.ReadLine();
            }

            await _client.LoginAsync(TokenType.Bot, token);

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
