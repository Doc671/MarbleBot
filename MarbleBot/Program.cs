using Discord;
using Discord.WebSocket;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
/*using Google.Apis.Auth.OAuth2;
using Google.Apis.Docs.v1;
using Google.Apis.Docs.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;*/

namespace MarbleBot
{
    class Program
    {
        static void Main()
        => new Program().StartAsync().GetAwaiter().GetResult();

        public static DiscordSocketClient _client;

        private CommandHandler _handler;

        public async Task StartAsync()
        {
            Console.Title = "MarbleBot";

            var logPath = "MBLog-" + DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss") + ".txt";
            File.Create(logPath).Close();

            Trace.Listeners.Clear();

            TextWriterTraceListener twtl = new TextWriterTraceListener(logPath) {
                Name = "TextLogger",
                TraceOutputOptions = TraceOptions.ThreadId | TraceOptions.DateTime
            };

            ConsoleTraceListener ctl = new ConsoleTraceListener() {
                TraceOutputOptions = TraceOptions.DateTime
            };

            Trace.Listeners.Add(twtl);
            Trace.Listeners.Add(ctl);
            Trace.AutoFlush = true;

            Global.StartTime = DateTime.UtcNow;
            _client = new DiscordSocketClient();

            string token = "";
            if (File.Exists("C:/Folder/MBT.txt")) {
                using (var stream = new StreamReader("C:/Folder/MBT.txt")) token = stream.ReadLine();
                using (var stream = new StreamReader("C:/Folder/MBK.txt")) Global.YTKey = stream.ReadLine();
                using (var stream = new StreamReader("C:/Folder/MBK2.txt")) Global.GDKey = stream.ReadLine();
            } else {
                using (var stream = new StreamReader("MBT.txt")) token = stream.ReadLine();
                using (var stream = new StreamReader("MBK.txt")) Global.YTKey = stream.ReadLine();
                using (var stream = new StreamReader("MBK2.txt")) Global.GDKey = stream.ReadLine();
            }

            using (var ar = new StreamReader("Resources\\Autoresponses.txt")) {
                while (!ar.EndOfStream) {
                    var arar = ar.ReadLine().Split(';');
                    Global.Autoresponses.Add(arar[0], arar[1]);
                }
            }

            /*var service = new DocsService(new BaseClientService.Initializer {
                ApiKey = Global.GDKey,
                ApplicationName = GetType().ToString(),
            });*/

            await _client.LoginAsync(TokenType.Bot, token);

            await _client.StartAsync();

            Trace.WriteLine("MarbleBot by Doc671\nStarted running: " + Global.StartTime.ToString("yyyy-MM-dd HH:mm:ss") + "\n");

            _handler = new CommandHandler(_client);

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }
    }
}
