using Discord;
using Discord.WebSocket;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Docs.v1;
using Google.Apis.Docs.v1.Data;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Util.Store;
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
    internal sealed class Program
    {
        private static void Main()
        => new Program().StartAsync().GetAwaiter().GetResult();

        private readonly static DiscordSocketClient _client = new DiscordSocketClient();

#pragma warning disable IDE0052 // Remove unread private members
        private CommandHandler _handler;
#pragma warning restore IDE0052 // Remove unread private members

        private readonly static DocsService _service = new DocsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = Global.Credential,
                ApplicationName = "MarbleBot",
            });

        private readonly static DocumentsResource.GetRequest _request = _service.Documents.Get(_documentId);

        private const string _documentId = "1HlzCpdMG7Wn5cAGDCLb_k9NqMk6jsZG9P1Q-VwCrp9E";

        private async Task StartAsync()
        {
            Console.Title = "MarbleBot";

            Global.StartTime = DateTime.UtcNow;

            string token = "";
            using (var stream = new StreamReader($"Keys{Path.DirectorySeparatorChar}MBT.txt")) token = stream.ReadLine();

            using (var guildFile = new StreamReader($"Data{Path.DirectorySeparatorChar}Guilds.json"))
            {
                string json;
                using (var users = new StreamReader($"Data{Path.DirectorySeparatorChar}Guilds.json"))
                    json = await users.ReadToEndAsync();
                var allServers = JsonConvert.DeserializeObject<Dictionary<ulong, MarbleBotGuild>>(json);
                foreach (var guild in allServers)
                {
                    var guild2 = guild.Value;
                    guild2.Id = guild.Key;
                    Global.Servers.Add(guild2);
                }
            }

            using (var stream = new FileStream($"Keys{Path.DirectorySeparatorChar}client_id.json", FileMode.Open, FileAccess.Read))
            {
                Global.Credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    new string[] { DocsService.Scope.Documents, SheetsService.Scope.Spreadsheets },
                    "user",
                    CancellationToken.None,
                    new FileDataStore("token.json", true));
            }

            await _client.LoginAsync(TokenType.Bot, token);

            await _client.StartAsync();

            await _client.SetGameAsync("Try mb/help!");

            Log($"MarbleBot by Doc671\nStarted running: {Global.StartTime.ToString("yyyy-MM-dd HH:mm:ss")}\n", true);

            _handler = new CommandHandler(_client);

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        internal static void Log(string log, bool noDate = false)
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
