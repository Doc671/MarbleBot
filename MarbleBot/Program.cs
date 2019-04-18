using Discord;
using Discord.WebSocket;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Docs.v1;
using Google.Apis.Docs.v1.Data;
using Google.Apis.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

            Global.StartTime = DateTime.UtcNow;
            _client = new DiscordSocketClient();

            string token = "";
            using (var stream = new StreamReader("MBT.txt")) token = stream.ReadLine();
            using (var stream = new StreamReader("MBK.txt")) Global.YTKey = stream.ReadLine();

            using (var arFile = new StreamReader("Resources\\Autoresponses.txt")) {
                while (!arFile.EndOfStream) {
                    var arPair = arFile.ReadLine().Split(';');
                    Global.Autoresponses.Add(arPair[0], arPair[1]);
                }
            }

            await _client.LoginAsync(TokenType.Bot, token);

            await _client.StartAsync();

            await _client.SetGameAsync("Try mb/help!");

            await Log($"MarbleBot by Doc671\nStarted running: {Global.StartTime.ToString("yyyy-MM-dd HH:mm:ss")}\n", true);

            _handler = new CommandHandler(_client);

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        public async static Task Log(string log, bool noDate = false) {
            var logString = noDate ? log : $"[{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")}] {log}";
            Console.WriteLine(logString);

            UserCredential credential;

            using (var stream = new FileStream("client_id.json", FileMode.Open, FileAccess.Read)) {
                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    new string[] { DocsService.Scope.Documents },
                    "user",
                    CancellationToken.None);
            }

            var service = new DocsService(new BaseClientService.Initializer() {
                HttpClientInitializer = credential,
                ApplicationName = "MarbleBot",
            });

            string documentId = "1HlzCpdMG7Wn5cAGDCLb_k9NqMk6jsZG9P1Q-VwCrp9E";

            // Get the end of the document
            DocumentsResource.GetRequest request = service.Documents.Get(documentId);
            Document doc = await request.ExecuteAsync();
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
            await service.Documents.BatchUpdate(body, documentId).ExecuteAsync();
        }
    }
}
