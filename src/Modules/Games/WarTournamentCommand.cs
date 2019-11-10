using Discord.Commands;
using MarbleBot.Common;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
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
        private static readonly string _warFilePath = $"Data{Path.DirectorySeparatorChar}WarTournament.wt";

        private static List<WarTournamentInfo> GetTournamentsInfo()
        {
            if (!File.Exists(_warFilePath))
            {
                File.Create(_warFilePath).Close();
            }

            using var tournamentFile = new StreamReader(_warFilePath);
            var serialiser = new BinaryFormatter();
            return (List<WarTournamentInfo>)serialiser.Deserialize(tournamentFile.BaseStream);
        }

        private static void WriteTournamentsInfo(List<WarTournamentInfo> tournamentsInfo)
        {
            using var tournamentFile = new StreamWriter(_warFilePath);
            var serialiser = new BinaryFormatter();
            serialiser.Serialize(tournamentFile.BaseStream, tournamentsInfo);
        }

        [Command("setup")]
        [Summary("Sets up a Marble War Tournament.")]
        [RequireUserPermission(Discord.ChannelPermission.ManageMessages)]
        public async Task WarTournamentSetupCommand(uint spaces, uint teamSize)
        {
            var tournamentsInfo = GetTournamentsInfo();
            tournamentsInfo.Add(new WarTournamentInfo(Context.Guild.Id, spaces, teamSize, null));
            WriteTournamentsInfo(tournamentsInfo);
            await ReplyAsync($"Successfully created war tournament with **{spaces}** spaces and team sizes of **{teamSize}**.");
        }

        [Command("signup")]
        [Alias("join")]
        [Summary("Sign up to the Marble War Tournament!")]
        public async Task WarTournamentSignupCommand(string teamName)
        {
            var tournamentsInfo = GetTournamentsInfo();
            if (tournamentsInfo.Any(tInfo => tInfo.GuildId == Context.Guild.Id))
            {
                var tournamentInfo = tournamentsInfo.Find(tInfo => tInfo.GuildId == Context.Guild.Id);
                if (tournamentInfo.Marbles.Any(teamPair => teamPair.Value.Contains(Context.Guild.Id)))
                {
                    await SendErrorAsync($"**{Context.User.Username}**, you have already signed up!");
                    return;
                }

                if (tournamentInfo.Marbles.ContainsKey(teamName))
                {
                    tournamentInfo.Marbles[teamName].Add(Context.User.Id);
                }
                else
                {
                    tournamentInfo.Marbles.Add(teamName, new List<ulong> { Context.User.Id });
                }
            }

            WriteTournamentsInfo(tournamentsInfo);

            await ReplyAsync($"**{Context.User.Username}** has successfully signed up to Team **{teamName}**!");
        }
    }
}
