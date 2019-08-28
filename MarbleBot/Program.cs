using Discord;
using Discord.WebSocket;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Docs.v1;
using Google.Apis.Docs.v1.Data;
using Google.Apis.Services;
using MarbleBot.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarbleBot
{
    internal class Program
    {
        private static void Main()
        => new Program().StartAsync().GetAwaiter().GetResult();

        private static DiscordSocketClient _client;

#pragma warning disable IDE0052 // Remove unread private members
        private CommandHandler _handler;
#pragma warning restore IDE0052 // Remove unread private members

        private static UserCredential _credential;

        private static DocsService _service;

        private static DocumentsResource.GetRequest _request;

        private const string _documentId = "1HlzCpdMG7Wn5cAGDCLb_k9NqMk6jsZG9P1Q-VwCrp9E";

        public async Task StartAsync()
        {
            Console.Title = "MarbleBot";

            Global.StartTime = DateTime.UtcNow;

            _client = new DiscordSocketClient();

            string token = "";
            using (var stream = new StreamReader($"Keys{Path.DirectorySeparatorChar}MBT.txt")) token = stream.ReadLine();
            using (var stream = new StreamReader($"Keys{Path.DirectorySeparatorChar}MBK.txt")) Global.YTKey = stream.ReadLine();

            using (var srvrFile = new StreamReader($"Data{Path.DirectorySeparatorChar}Servers.json"))
            {
                string json;
                using (var users = new StreamReader($"Data{Path.DirectorySeparatorChar}Servers.json"))
                    json = await users.ReadToEndAsync();
                var allServers = JsonConvert.DeserializeObject<Dictionary<ulong, MarbleBotServer>>(json);
                foreach (var server in allServers)
                {
                    var server2 = server.Value;
                    server2.Id = server.Key;
                    Global.Servers.Add(server2);
                }
            }

            using (var autoresponseFile = new StreamReader($"Resources{Path.DirectorySeparatorChar}Autoresponses.txt"))
            {
                while (!autoresponseFile.EndOfStream)
                {
                    var autoresponsePair = (await autoresponseFile.ReadLineAsync()).Split(';');
                    Global.Autoresponses.Add(autoresponsePair[0], autoresponsePair[1]);
                }
            }

            using (var stream = new FileStream($"Keys{Path.DirectorySeparatorChar}client_id.json", FileMode.Open, FileAccess.Read))
            {
                _credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    new string[] { DocsService.Scope.Documents },
                    "user",
                    CancellationToken.None);
            }

            _service = new DocsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = _credential,
                ApplicationName = "MarbleBot",
            });

            _request = _service.Documents.Get(_documentId);

            await _client.LoginAsync(TokenType.Bot, token);

            await _client.StartAsync();

            await _client.SetGameAsync("Try mb/help!");

            Log($"MarbleBot by Doc671\nStarted running: {Global.StartTime.ToString("yyyy-MM-dd HH:mm:ss")}\n", true);

            _handler = new CommandHandler(_client);

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        public static void Log(string log, bool noDate = false)
        => Task.Run(() =>
        {
            var logString = noDate ? log : $"[{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")}] {log}";
            Console.WriteLine(logString);

            // Get the end of the document
            Document doc = _request.Execute();
            var index = doc.Body.Content.Last().EndIndex - 1;

            // Write to the document
            var requests = new List<Request> {
                new Request() {
                    InsertText = new InsertTextRequest() {
                        Text = $"\n\n{logString}",
                        Location = new Location() { Index = index }
                    }
                }
            };

            var body = new BatchUpdateDocumentRequest() { Requests = requests };
            _service.Documents.BatchUpdate(body, _documentId).Execute();
        });
    }
}
