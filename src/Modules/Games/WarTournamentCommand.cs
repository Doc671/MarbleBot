using Discord.Commands;
using MarbleBot.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MarbleBot.Modules.Games
{
    [Group("wartournament")]
    [Alias("wt")]
    [Summary("Participate in a Marble War tournament!")]
    [Remarks("Requires a channel in which slowmode is enabled.")]
    [RequireContext(ContextType.Guild)]
    [RequireOwner]
    public class WarTournamentCommand : GameModule
    {
        private static readonly string _warFilePath = $"Data{Path.DirectorySeparatorChar}WarTournament.json";

        private static Dictionary<ulong, WarTournamentInfo> GetTournamentsInfo()
        {
            if (!File.Exists(_warFilePath))
            {
                File.Create(_warFilePath).Close();
            }

            string json;
            using (var tournamentFile = new StreamReader(_warFilePath))
            {
                json = tournamentFile.ReadToEnd();
            }

            return JObject.Parse(json).ToObject<Dictionary<ulong, WarTournamentInfo>>();
        }

        private static void WriteTournamentsInfo(Dictionary<ulong, WarTournamentInfo> tournamentsInfo)
        {
            using var tournamentFile = new JsonTextWriter(new StreamWriter(_warFilePath));
            var serialiser = new JsonSerializer { Formatting = Formatting.Indented };
            serialiser.Serialize(tournamentFile, tournamentsInfo);
        }

        [Command("setup")]
        [Summary("Sets up a Marble War Tournament.")]
        [RequireUserPermission(Discord.ChannelPermission.ManageMessages)]
        public async Task WarTournamentSetupCommand(uint spaces, uint teamSize)
        {
            var tournamentsInfo = GetTournamentsInfo();
            tournamentsInfo.Add(Context.Guild.Id, new WarTournamentInfo(Context.Guild.Id, spaces, teamSize, null));
            WriteTournamentsInfo(tournamentsInfo);
            await ReplyAsync($"Successfully created war tournament with **{spaces}** spaces and team sizes of **{teamSize}**.");
        }

        [Command("signup")]
        [Alias("join")]
        [Summary("Sign up to the Marble War Tournament!")]
        public async Task WarTournamentSignupCommand(string teamName)
        {
            var tournamentsInfo = GetTournamentsInfo();
            if (tournamentsInfo.ContainsKey(Context.Guild.Id))
            {
                if (tournamentsInfo[Context.Guild.Id].Marbles.Any(teamPair => teamPair.Value.Contains(Context.Guild.Id)))
                {
                    await SendErrorAsync($"**{Context.User.Username}**, you have already signed up!");
                    return;
                }

                if (tournamentsInfo[Context.Guild.Id].Marbles.ContainsKey(teamName))
                {
                    tournamentsInfo[Context.Guild.Id].Marbles[teamName].Add(Context.User.Id);
                }
                else
                {
                    tournamentsInfo[Context.Guild.Id].Marbles.Add(teamName, new List<ulong> { Context.User.Id });
                }
            }

            WriteTournamentsInfo(tournamentsInfo);

            await ReplyAsync($"**{Context.User.Username}** has successfully signed up to Team **{teamName}**!");
        }
    }
}
