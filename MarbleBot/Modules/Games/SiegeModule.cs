using MarbleBot.Data.Repositories;
using MarbleBot.Data.Repositories.Games;
using MarbleBot.Entities;
using MarbleBot.Entities.Games;
using MarbleBot.Entities.Games.Siege;
using MarbleBot.Games.Siege;
using MarbleBot.Services;
using MarbleBot.Services.Games;
using System.Text;

namespace MarbleBot.Modules.Games;

[Group("siege", "The siege minigame, where you can join to fight a boss.")]
public class SiegeModule : MarbleBotSlashCommandModule
{
    private readonly SiegeSessionService _sessionService;
    private readonly AutoModFilterService _autoModFilterService;
    private readonly GuildColorProvider _guildColorProvider;
    private readonly SignupGameResultDao _winnerDao;
    private readonly BossDao _bossDao;
    private readonly UserDao _userDao;

    public SiegeModule(SiegeSessionService sessionService, AutoModFilterService autoModFilterService, GuildColorProvider guildColorProvider, SignupGameResultDao winnerDao, BossDao bossDao, UserDao userDao)
    {
        _sessionService = sessionService;
        _autoModFilterService = autoModFilterService;
        _guildColorProvider = guildColorProvider;
        _winnerDao = winnerDao;
        _bossDao = bossDao;
        _userDao = userDao;
    }

    [SlashCommand("bossinfo", "Returns information about a given boss.")]
    public async Task BossInfoCommand(string bossName)
    {
        var boss = await _bossDao.GetByNameAsync(bossName);
        if (boss == null)
        {
            await RespondWithErrorAsync($"Could not find boss with name {bossName}.");
            return;
        }

        var attacks = new StringBuilder();
        foreach (Attack attack in boss.Attacks)
        {
            if (attack.StatusEffect == StatusEffect.None)
            {
                attacks.AppendLine($"**{attack.Name}** (Accuracy: {attack.Accuracy}%) [Damage: {attack.Damage}]");
            }
            else
            {
                attacks.AppendLine($"**{attack.Name}** (Accuracy: {attack.Accuracy}%) [Damage: {attack.Damage}] <Status: {attack.StatusEffect}>");
            }
        }

        var drops = new StringBuilder();
        foreach (BossItemDrop drop in boss.Drops)
        {
            string dropAmount = drop.MinCount == drop.MaxCount ? drop.MinCount.ToString() : $"{drop.MinCount}-{drop.MaxCount}";
            string idString = drop.ItemId.ToString("000");
            drops.AppendLine($"`[{idString}]` **{drop.Item.Name}**: {dropAmount} ({drop.Chance}%)");
        }

        var builder = new EmbedBuilder()
            .AddField("Health", $"**{boss.MaxHealth}**")
            .AddField("Attacks", attacks.ToString())
            .AddField("Difficulty", $"**{boss.Difficulty} {(int)boss.Difficulty}**/10")
            .WithColor(await _guildColorProvider.GetColorAsync(Context))
            .WithThumbnailUrl(boss.ImageUrl)
            .WithTitle(boss.Name);

        if (drops.Length != 0)
        {
            builder.AddField("Drops", drops.ToString());
        }

        await RespondAsync(embed: builder.Build());
    }

    [SlashCommand("bosslist", "Returns a list of all bosses.")]
    public async Task BossListCommand()
    {
        IReadOnlyList<Boss> bosses = await _bossDao.GetAllAsync();
        if (bosses.Count == 0)
        {
            await RespondWithErrorAsync("The bot has no bosses loaded!");
            return;
        }

        var builder = new EmbedBuilder()
            .WithDescription("Use `/siege bossinfo <boss name>` for more info!")
            .WithTitle("Playable Siege Bosses");

        foreach (Boss boss in bosses)
        {
            string difficulty = $"Difficulty: **{boss.Difficulty} {(int)boss.Difficulty}**/10, Health: **{boss.MaxHealth}**, Attacks: **{boss.Attacks.Count}**";
            builder.AddField($"{boss.Name}", difficulty);
        }

        await RespondAsync(embed: builder.Build());
    }

    [SlashCommand("contestants", "Returns a list of the siege contestants in this channel.")]
    public async Task ContestantsCommand()
    {
        SiegeSession? session = await _sessionService.GetSessionAsync(Context.Channel.Id);
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
            .WithTitle("Marble Siege: Contestants")
            .AddField($"Contestants: {entryCount}", stringBuilder.ToString());

        await RespondAsync(embed: builder.Build());
    }

    [SlashCommand("join", "Signs up your given marble name for the siege.")]
    public async Task JoinCommand(string? marbleName = null)
    {
        const int marbleLimit = 10;
        SiegeSession session = await _sessionService.GetOrCreateSessionAsync(Context.Channel.Id, marbleLimit);
        if (session.ContainsId(Context.User.Id))
        {
            await RespondWithErrorAsync("You can only sign up for a siege once!");
            return;
        }

        marbleName ??= Context.User.Username;

        const int characterLimit = 100;
        if (marbleName.Length > characterLimit)
        {
            await RespondWithErrorAsync("Your marble name exceeds the 100 character limit.");
            return;
        }

        marbleName = await _autoModFilterService.FilterAsync(marbleName, Context);

        User user = await _userDao.GetOrCreateUserAsync(Context.User.Id, Context.User.Username, Context.User.GlobalName);
        if (!await _sessionService.TryAddEntryAsync(Context.Channel.Id, user, marbleName))
        {
            if (session.GetEntryCount() >= marbleLimit)
            {
                await RespondWithErrorAsync("The current siege in this channel is full. A siege participant must start the siege using /siege start.");
            }
            else
            {
                await RespondWithErrorAsync("The siege in this channel is still ongoing.");
            }
            return;
        }

        var builder = new EmbedBuilder()
            .WithColor(await _guildColorProvider.GetColorAsync(Context))
            .AddField("Marble Siege: Signed up!", $"**{Context.User.Username}** has successfully signed up as {marbleName.ToDiscordBold()}!");

        if (session.GetEntryCount() == marbleLimit)
        {
            builder.AddField("Marble limit reached", $"{marbleLimit} marbles have signed up! Use /siege start to start the siege!");
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

    [ComponentInteraction("siege_mostuses_previous_*_*", ignoreGroupNames: true)]
    public async Task MostUsesPreviousCommand(bool thisChannelOnly, int page)
    {
        (EmbedBuilder embedBuilder, ComponentBuilder componentBuilder) = await GetMostUsesBuilders(thisChannelOnly, page);

        var messageComponent = (Context.Interaction as SocketMessageComponent)!;
        await messageComponent.UpdateAsync(message =>
        {
            message.Embed = embedBuilder.Build();
            message.Components = componentBuilder.Build();
        });
    }

    [ComponentInteraction("siege_mostuses_next_*_*", ignoreGroupNames: true)]
    public async Task MostUsesNextCommand(bool thisChannelOnly, int page)
    {
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
            results = await _winnerDao.GetUsesAsync(Context.Channel.Id, GameType.Siege, page, itemsPerPage);
        }
        else
        {
            results = await _winnerDao.GetUsesAsync(GameType.Siege, page, itemsPerPage);
        }

        bool resultsPresent = results.Count != 0;

        var resultOutput = new StringBuilder();
        string title;
        if (!resultsPresent)
        {
            title = "Siege Leaderboard";
            resultOutput.Append("There are no marble names on this page.");
        }
        else
        {
            title = "Siege Leaderboard: Usage";
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
            .WithButton("Previous", customId: $"siege_mostuses_previous_{thisChannelOnly}_{page - 1}", disabled: page <= 1)
            .WithButton("Next", customId: $"siege_mostuses_next_{thisChannelOnly}_{page + 1}", disabled: results.Count <= page * itemsPerPage);
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

    [ComponentInteraction("siege_mostwins_previous_*_*_*", ignoreGroupNames: true)]
    public async Task MostWinsPreviousCommand(bool thisChannelOnly, bool winPercentage, int page)
    {
        (EmbedBuilder embedBuilder, ComponentBuilder componentBuilder) = await GetMostWinsBuilders(thisChannelOnly, winPercentage, page);

        var messageComponent = (Context.Interaction as SocketMessageComponent)!;
        await messageComponent.UpdateAsync(message =>
        {
            message.Embed = embedBuilder.Build();
            message.Components = componentBuilder.Build();
        });
    }

    [ComponentInteraction("siege_mostwins_next_*_*_*", ignoreGroupNames: true)]
    public async Task MostWinsNextCommand(bool thisChannelOnly, bool winPercentage, int page)
    {
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
            results = await _winnerDao.GetWinsAsync(Context.Channel.Id, GameType.Siege, page, itemsPerPage);
        }
        else
        {
            results = await _winnerDao.GetWinsAsync(GameType.Siege, page, itemsPerPage);
        }

        bool resultsPresent = results.Count != 0;

        var resultOutput = new StringBuilder();
        string title;
        if (!resultsPresent)
        {
            title = "Siege Leaderboard";
            resultOutput.Append("There are no marble names on this page.");
        }
        else if (winPercentage)
        {
            title = "Siege Leaderboard: Win Percentage";
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
            title = "Siege Leaderboard: Wins";
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
            .WithButton("Previous", customId: $"siege_mostwins_previous_{thisChannelOnly}_{winPercentage}_{page - 1}", disabled: page <= 1)
            .WithButton("Next", customId: $"siege_mostwins_next_{thisChannelOnly}_{winPercentage}_{page + 1}", disabled: results.Count <= page * itemsPerPage);
        return (embedBuilder, componentBuilder);
    }

    [SlashCommand("remove", "Removes your given marble name from the siege.")]
    public async Task RemoveCommand(string? marbleName = null)
    {
        SiegeSession? session = await _sessionService.GetSessionAsync(Context.Channel.Id);
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
            await RespondWithErrorAsync($"No marble with the name {marbleName.ToDiscordBold()} has signed up for the siege.");
            return;
        }

        await RespondAsync($"Successfully removed {marbleName.ToDiscordBold()}");
    }

    [SlashCommand("start", "Starts the siege.")]
    public async Task StartCommand()
    {
        SiegeSession? session = await _sessionService.GetSessionAsync(Context.Channel.Id);
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

        await RespondAsync("Beginning...");

        IUserMessage countdownMessage = await ReplyAsync("# 3");
        await Task.Delay(1000);
        await countdownMessage.ModifyAsync(message => message.Content = "# 2");
        await Task.Delay(1000);
        await countdownMessage.ModifyAsync(message => message.Content = "# 1");
        await Task.Delay(1000);
        await countdownMessage.ModifyAsync(message => message.Content = "# BEGIN THE SIEGE!");

        var embedBuilder = new EmbedBuilder()
            .WithTitle("Beginning...")
            .WithColor(await _guildColorProvider.GetColorAsync(Context));

        var componentBuilder = new ComponentBuilder()
            .WithButton("Bash", customId: "siege_bash", style: ButtonStyle.Primary, emote: new Emoji("💥"), disabled: true);

        componentBuilder.AddRow(new ActionRowBuilder()
            .WithButton(customId: "siege_dodge_veryleft", style: ButtonStyle.Secondary, emote: new Emoji("⏪"), disabled: true)
            .WithButton(customId: "siege_dodge_left", style: ButtonStyle.Secondary, emote: new Emoji("◀️"), disabled: true)
            .WithButton(customId: "siege_dodge_right", style: ButtonStyle.Secondary, emote: new Emoji("▶️"), disabled: true)
            .WithButton(customId: "siege_dodge_veryright", style: ButtonStyle.Secondary, emote: new Emoji("⏩"), disabled: true));

        IUserMessage siegeMessage = await ReplyAsync(embed: embedBuilder.Build(), components: componentBuilder.Build());

        await session.StartAsync(siegeMessage, embedBuilder, componentBuilder);
    }

    [ComponentInteraction("siege_bash", ignoreGroupNames: true)]
    public async Task BashCommand()
    {
        SiegeSession? session = await _sessionService.GetSessionAsync(Context.Channel.Id);
        if (session == null)
        {
            await RespondWithErrorAsync("There is no siege active in this channel.");
            return;
        }

        string? result = await session.TryBashAsync(Context.User.Id);
        if (result == null)
        {
            await DeferAsync();
        }
        else
        {
            await RespondWithErrorAsync(result);
        }
    }

    [ComponentInteraction("siege_dodge_veryleft", ignoreGroupNames: true)]
    public async Task DodgeVeryLeftCommand()
    {
        await DodgeCommand(-2);
    }

    [ComponentInteraction("siege_dodge_left", ignoreGroupNames: true)]
    public async Task DodgeLeftCommand()
    {
        await DodgeCommand(-1);
    }

    [ComponentInteraction("siege_dodge_right", ignoreGroupNames: true)]
    public async Task DodgeRightCommand()
    {
        await DodgeCommand(1);
    }

    [ComponentInteraction("siege_dodge_veryright", ignoreGroupNames: true)]
    public async Task DodgeVeryRightCommand()
    {
        await DodgeCommand(2);
    }

    public async Task DodgeCommand(int moveDelta)
    {
        SiegeSession? session = await _sessionService.GetSessionAsync(Context.Channel.Id);
        if (session == null)
        {
            await RespondWithErrorAsync("There is no siege active in this channel.");
            return;
        }

        string? result = await session.TryDodgeAsync(Context.User.Id, moveDelta);
        if (result == null)
        {
            await DeferAsync();
        }
        else
        {
            await RespondWithErrorAsync(result);
        }
    }
}
