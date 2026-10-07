using MarbleBot.Data.Repositories.Games;
using MarbleBot.Entities.Games;
using MarbleBot.Services;
using MarbleBot.Services.Games;
using System.Text;

namespace MarbleBot.Modules.Games;

[Group("race", "The race minigame, which randomly picks a winner.")]
public class RaceModule : MarbleBotSlashCommandModule
{
    private readonly RaceSessionService _sessionService;
    private readonly AutoModFilterService _autoModFilterService;
    private readonly GuildColorProvider _guildColorProvider;
    private readonly SignupGameResultDao _winnerDao;

    public RaceModule(RaceSessionService sessionService, AutoModFilterService autoModFilterService, GuildColorProvider guildColorProvider, SignupGameResultDao winnerDao)
    {
        _sessionService = sessionService;
        _autoModFilterService = autoModFilterService;
        _guildColorProvider = guildColorProvider;
        _winnerDao = winnerDao;
    }

    [SlashCommand("contestants", "Returns a list of the race contestants in this channel.")]
    public async Task ContestantsCommand()
    {
        RaceSession? session = await _sessionService.GetSessionAsync(Context.Channel.Id);
        if (session == null)
        {
            await RespondWithErrorAsync("Nobody has signed up!");
            return;
        }

        int entryCount = session.GetEntryCount();
        if (entryCount == 0)
        {
            await RespondWithErrorAsync("Nobody has signed up!");
            return;
        }

        IReadOnlyList<SignupEntry> entries = session.GetEntries();
        var stringBuilder = new StringBuilder();
        foreach (SignupEntry entry in entries)
        {
            IUser user = await Context.Channel.GetUserAsync(entry.UserId);
            stringBuilder.AppendLine($"{entry.MarbleName.ToDiscordBold()} [{user.Username}]");
        }

        var builder = new EmbedBuilder()
            .WithColor(await _guildColorProvider.GetColorAsync(Context))
            .WithTitle("Marble Race: Contestants")
            .AddField($"Contestants: {entryCount}", stringBuilder.ToString());

        await RespondAsync(embed: builder.Build());
    }

    [SlashCommand("join", "Signs up your given marble name for the race.")]
    public async Task JoinCommand(string? marbleName = null)
    {
        const int marbleLimit = 10;
        RaceSession session = await _sessionService.GetOrCreateSessionAsync(Context.Channel.Id, marbleLimit);

        marbleName ??= Context.User.Username;

        const int characterLimit = 100;
        if (marbleName.Length > characterLimit)
        {
            await RespondWithErrorAsync("Your marble name exceeds the 100 character limit.");
            return;
        }

        marbleName = await _autoModFilterService.FilterAsync(marbleName, Context);

        if (!await _sessionService.TryAddEntryAsync(Context.Channel.Id, Context.User.Id, marbleName))
        {
            if (session.GetEntryCount() >= marbleLimit)
            {
                await RespondWithErrorAsync("The current race in this channel is full. A race participant must start the race using /race start.");
            }
            else 
            {
                await RespondWithErrorAsync("The race in this channel is still ongoing.");
            }
            return;
        }

        var builder = new EmbedBuilder()
            .WithColor(await _guildColorProvider.GetColorAsync(Context))
            .AddField("Marble Race: Signed up!", $"**{Context.User.Username}** has successfully signed up as {marbleName.ToDiscordBold()}!");

        if (session.GetEntryCount() == marbleLimit)
        {
            builder.AddField("Marble limit reached", $"{marbleLimit} marbles have signed up! Use /race start to start the race!");
        }

        await RespondAsync(embed: builder.Build());
    }

    [SlashCommand("mostuses", "Returns a usage leaderboard for all marble names.")]
    public async Task MostUsesCommand(
        [Summary("thisChannelOnly", "If true, only shows the leaderboard based on uses in this channel. Global leaderboard otherwise.")] bool thisChannelOnly,
        [Summary("page", "The page number of the leaderboard to show. Each page contains 20 marble names.")] int page = 1)
    {
        (EmbedBuilder embedBuilder, ComponentBuilder componentBuilder) = await GetMostUsesBuilders(thisChannelOnly, page);

        await RespondAsync(embed: embedBuilder.Build(), components: componentBuilder.Build());
    }


    [ComponentInteraction("race_mostuses_previous_*_*_*", ignoreGroupNames: true)]
    public async Task MostUsesPreviousCommand(ulong id, bool thisChannelOnly, int page)
    {
        if (Context.User.Id != id)
        {
            return;
        }

        (EmbedBuilder embedBuilder, ComponentBuilder componentBuilder) = await GetMostUsesBuilders(thisChannelOnly, page);

        var messageComponent = (Context.Interaction as SocketMessageComponent)!;
        await messageComponent.UpdateAsync(message =>
        {
            message.Embed = embedBuilder.Build();
            message.Components = componentBuilder.Build();
        });
    }

    [ComponentInteraction("race_mostuses_next_*_*_*", ignoreGroupNames: true)]
    public async Task MostUsesNextCommand(ulong id, bool thisChannelOnly, int page)
    {
        if (Context.User.Id != id)
        {
            return;
        }

        (EmbedBuilder embedBuilder, ComponentBuilder componentBuilder) = await GetMostUsesBuilders(thisChannelOnly, page);

        var messageComponent = (Context.Interaction as SocketMessageComponent)!;
        await messageComponent.UpdateAsync(message =>
        {
            message.Embed = embedBuilder.Build();
            message.Components = componentBuilder.Build();
        });
    }

    private async Task<(EmbedBuilder embedBuilder, ComponentBuilder componentBuilder)> GetMostUsesBuilders(bool thisChannelOnly, int page)
    {
        IReadOnlyList<LeaderboardEntry> results;
        const int itemsPerPage = 20;
        if (thisChannelOnly)
        {
            results = await _winnerDao.GetUsesAsync(Context.Channel.Id, GameType.Race, page, itemsPerPage);
        }
        else
        {
            results = await _winnerDao.GetUsesAsync(GameType.Race, page, itemsPerPage);
        }

        bool resultsPresent = results.Count != 0;

        var resultOutput = new StringBuilder();
        string title;
        if (!resultsPresent)
        {
            title = "Race Leaderboard";
            resultOutput.Append("There are no marble names on this page.");
        }
        else
        {
            title = "Race Leaderboard: Usage";
            for (int i = 0; i < results.Count; i++)
            {
                LeaderboardEntry? result = results[i];
                if (result != null)
                {
                    resultOutput.AppendLine($"{result.Place}{result.Place.GetOrdinalSuffix()}: **{result.MarbleName}** ({result.Uses})");
                }
            }
        }

        string filteredOutput = await _autoModFilterService.FilterAsync(resultOutput.ToString(), Context);

        var embedBuilder = new EmbedBuilder()
            .WithColor(await _guildColorProvider.GetColorAsync(Context))
            .WithTitle(title)
            .WithDescription(filteredOutput)
            .WithCurrentTimestamp();

        var componentBuilder = new ComponentBuilder()
            .WithButton("Previous", customId: $"race_mostuses_previous_{Context.User.Id}_{thisChannelOnly}_{page - 1}", disabled: page <= 1)
            .WithButton("Next", customId: $"race_mostuses_next_{Context.User.Id}_{thisChannelOnly}_{page + 1}", disabled: results.Count <= page * itemsPerPage);
        return (embedBuilder, componentBuilder);
    }

    [SlashCommand("mostwins", "Returns a win leaderboard for all marble names.")]
    public async Task MostWinsCommand(
        [Summary("thisChannelOnly", "If true, only shows the leaderboard based on wins in this channel. Global leaderboard otherwise.")] bool thisChannelOnly, 
        [Summary("winPercentage", "If true, ranks the leaderboard based on win percentage rather than absolute win count.")] bool winPercentage,
        [Summary("page", "The page number of the leaderboard to show. Each page contains 20 marble names.")] int page = 1)
    {
        (EmbedBuilder embedBuilder, ComponentBuilder componentBuilder) = await GetMostWinsBuilders(thisChannelOnly, winPercentage, page);

        await RespondAsync(embed: embedBuilder.Build(), components: componentBuilder.Build());
    }

    [ComponentInteraction("race_mostwins_previous_*_*_*_*", ignoreGroupNames: true)]
    public async Task MostWinsPreviousCommand(ulong id, bool thisChannelOnly, bool winPercentage, int page)
    {
        if (Context.User.Id != id)
        {
            return;
        }

        (EmbedBuilder embedBuilder, ComponentBuilder componentBuilder) = await GetMostWinsBuilders(thisChannelOnly, winPercentage, page);

        var messageComponent = (Context.Interaction as SocketMessageComponent)!;
        await messageComponent.UpdateAsync(message =>
        {
            message.Embed = embedBuilder.Build();
            message.Components = componentBuilder.Build();
        });
    }

    [ComponentInteraction("race_mostwins_next_*_*_*_*", ignoreGroupNames: true)]
    public async Task MostWinsNextCommand(ulong id, bool thisChannelOnly, bool winPercentage, int page)
    {
        if (Context.User.Id != id)
        {
            return;
        }

        (EmbedBuilder embedBuilder, ComponentBuilder componentBuilder) = await GetMostWinsBuilders(thisChannelOnly, winPercentage, page);

        var messageComponent = (Context.Interaction as SocketMessageComponent)!;
        await messageComponent.UpdateAsync(message =>
        {
            message.Embed = embedBuilder.Build();
            message.Components = componentBuilder.Build();
        });
    }

    private async Task<(EmbedBuilder embedBuilder, ComponentBuilder componentBuilder)> GetMostWinsBuilders(bool thisChannelOnly, bool winPercentage, int page)
    {
        IReadOnlyList<LeaderboardEntry> results;
        const int itemsPerPage = 20;
        if (thisChannelOnly)
        {
            results = await _winnerDao.GetWinsAsync(Context.Channel.Id, GameType.Race, page, itemsPerPage);
        }
        else
        {
            results = await _winnerDao.GetWinsAsync(GameType.Race, page, itemsPerPage);
        }

        bool resultsPresent = results.Count != 0;

        var resultOutput = new StringBuilder();
        string title;
        if (!resultsPresent)
        {
            title = "Race Leaderboard";
            resultOutput.Append("There are no marble names on this page.");
        }
        else if (winPercentage)
        {
            title = "Race Leaderboard: Win Percentage";
            for (int i = 0; i < results.Count; i++)
            {
                LeaderboardEntry? result = results[i];
                if (result != null)
                {
                    float winFraction = (float)result.Wins / result.Uses;
                    resultOutput.AppendLine($"{result.Place}{result.Place.GetOrdinalSuffix()}: **{result.MarbleName}** ({winFraction:P})");
                }
            }
        }
        else
        {
            title = "Race Leaderboard: Wins";
            for (int i = 0; i < results.Count; i++)
            {
                LeaderboardEntry? result = results[i];
                if (result != null)
                {
                    resultOutput.AppendLine($"{result.Place}{result.Place.GetOrdinalSuffix()}: **{result.MarbleName}** ({result.Wins})");
                }
            }
        }

        string filteredOutput = await _autoModFilterService.FilterAsync(resultOutput.ToString(), Context);

        var embedBuilder = new EmbedBuilder()
            .WithColor(await _guildColorProvider.GetColorAsync(Context))
            .WithTitle(title)
            .WithDescription(filteredOutput)
            .WithCurrentTimestamp();

        var componentBuilder = new ComponentBuilder()
            .WithButton("Previous", customId: $"race_mostwins_previous_{Context.User.Id}_{thisChannelOnly}_{winPercentage}_{page - 1}", disabled: page <= 1)
            .WithButton("Next", customId: $"race_mostwins_next_{Context.User.Id}_{thisChannelOnly}_{winPercentage}_{page + 1}", disabled: results.Count <= page * itemsPerPage);
        return (embedBuilder, componentBuilder);
    }

    [SlashCommand("remove", "Removes your given marble name from the race.")]
    public async Task RemoveCommand(string? marbleName = null)
    {
        RaceSession? session = await _sessionService.GetSessionAsync(Context.Channel.Id);
        if (session == null)
        {
            await RespondWithErrorAsync("Nobody has signed up!");
            return;
        }

        int entryCount = session.GetEntryCount();
        if (entryCount == 0)
        {
            await RespondWithErrorAsync("Nobody has signed up!");
            return;
        }

        marbleName ??= Context.User.Username;
        marbleName = await _autoModFilterService.FilterAsync(marbleName, Context);

        if (!await _sessionService.TryRemoveEntryAsync(Context.Channel.Id, Context.User.Id, marbleName))
        {
            await RespondWithErrorAsync($"No marble with the name {marbleName.ToDiscordBold()} has signed up for the race.");
            return;
        }

        await RespondAsync($"Successfully removed {marbleName.ToDiscordBold()}");
    }

    [SlashCommand("start", "Starts the race.")]
    public async Task StartCommand()
    {
        RaceSession? session = await _sessionService.GetSessionAsync(Context.Channel.Id);
        if (session == null)
        {
            await RespondWithErrorAsync("Nobody has signed up!");
            return;
        }

        int entryCount = session.GetEntryCount();
        if (entryCount == 0)
        {
            await RespondWithErrorAsync("Nobody has signed up!");
            return;
        }

        if (entryCount == 1)
        {
            await RespondWithErrorAsync("Only one marble is signed up!");
            return;
        }

        await RespondAsync("Starting the race!");

        var builder = new EmbedBuilder()
            .WithColor(await _guildColorProvider.GetColorAsync(Context))
            .WithTitle("The race has started!");

        IUserMessage message = await ReplyAsync(embed: builder.Build());

        await foreach (RaceResult result in _sessionService.RunAsync(Context.Channel.Id))
        {
            if (result.Place == 1)
            {
                builder.AddField($"{result.MarbleName.ToDiscordBold()} wins!", 
                    $"{result.MarbleName} {result.ResultMessage}!");
            }
            else
            {
                builder.AddField($"{result.MarbleName.ToDiscordBold()} is eliminated!",
                    $"{result.MarbleName} {result.ResultMessage} and is now out of the competition!");
            }

            await message.ModifyAsync(message => message.Embed = builder.Build());
        }
    }
}
