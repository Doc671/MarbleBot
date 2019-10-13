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
        [Command("signup")]
        [Alias("join")]
        [Summary("Sign up to the Marble War Tournament!")]
        public async Task WarTournamentSignupCommand(string teamName)
        {
            string warFilePath = $"Data{Path.DirectorySeparatorChar}WarTournament.json";
            if (!File.Exists(warFilePath)) File.Create(warFilePath).Close();

            string json;
            using (var tournamentFile = new StreamReader(warFilePath))
                json = tournamentFile.ReadToEnd();

            var tournamentDict = JObject.Parse(json).ToObject<Dictionary<ulong, WarTournamentInfo>>();
            if (tournamentDict.ContainsKey(Context.Guild.Id))
            {
                if (tournamentDict[Context.Guild.Id].Marbles.Any(teamPair => teamPair.Value.Contains(Context.Guild.Id)))
                {
                    await SendErrorAsync($"**{Context.User.Username}**, you have already signed up!");
                    return;
                }

                if (tournamentDict[Context.Guild.Id].Marbles.ContainsKey(teamName))
                    tournamentDict[Context.Guild.Id].Marbles[teamName].Add(Context.User.Id);
                else
                    tournamentDict[Context.Guild.Id].Marbles.Add(teamName, new List<ulong> { Context.User.Id });
            }

            using (var tournamentFile = new JsonTextWriter(new StreamWriter(warFilePath)))
            {
                var serialiser = new JsonSerializer { Formatting = Formatting.Indented };
                serialiser.Serialize(tournamentFile, tournamentDict);
            }

            await ReplyAsync($"**{Context.User.Username}** has successfully signed up to Team **{teamName}**!");
        }
    }
}
