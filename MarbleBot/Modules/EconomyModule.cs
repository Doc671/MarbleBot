using MarbleBot.Data.Repositories;
using MarbleBot.Entities;
using System.Text;

namespace MarbleBot.Modules;

public class EconomyModule : MarbleBotSlashCommandModule
{
    private readonly UserDao _userDao;

    public EconomyModule(UserDao userDao)
    {
        _userDao = userDao;
    }

    [SlashCommand("balance", "Returns how much money you or someone else has.")]
    public async Task BalanceCommand(
        [Summary("user", "Which user's balance to inspect. Will inspect your own balance if left blank.")] User? marbleBotUser = null)
    {
        marbleBotUser ??= await _userDao.GetUserAsync(Context.User.Id);

        decimal balance = marbleBotUser?.Balance ?? 0;
        decimal netWorth = marbleBotUser?.NetWorth ?? 0;

        var builder = new EmbedBuilder()
            .WithFooter($"{_warning} | Balances are frozen for now and may be flattened in the future.")
            .AddField("Balance", $"{_unitOfMoney}{balance}", true)
            .AddField("Net Worth", $"{_unitOfMoney}{netWorth}", true);

        if (marbleBotUser == null)
        {
            builder.WithAuthor(Context.User);
        }
        else
        {
            await AddAuthorAsync(Context, marbleBotUser, builder);
        }

        await RespondAsync(embed: builder.Build());
    }

    [SlashCommand("inventory", "Returns a list of your items.")]
    public async Task InventoryCommand(
        [Summary("user", "Which user's inventory to inspect. Will inspect your own inventory if left blank.")] User? marbleBotUser = null,
        [Summary("page", "The page number of the inventory to show. Each page contains 20 items.")] int page = 1)
    {
        marbleBotUser ??= await _userDao.GetUserAsync(Context.User.Id);
        if (marbleBotUser == null || marbleBotUser.Inventory == null || marbleBotUser.Inventory.Count == 0)
        {
            await RespondWithErrorAsync("You have no items.");
            return;
        }

        var itemOutput = new StringBuilder();
        const int itemsPerPage = 20;
        var itemsOnPage = marbleBotUser.Inventory.Skip((page - 1) * itemsPerPage).Take(itemsPerPage);
        bool itemsPresent = itemsOnPage.Any();
        if (itemsPresent)
        {
            foreach ((Item item, int quantity) in itemsOnPage)
            {
                itemOutput.AppendLine($"`[{item.Id:000}]` {item.Name}: {quantity}");
            }
        }
        else
        {
            itemOutput.Append("There are no items on this page.");
        }

        var embedBuilder = new EmbedBuilder()
            .WithDescription(itemOutput.ToString())
            .WithFooter($"{_warning} | Inventories are frozen for now. Items may be changed in the future.")
            .WithTitle(itemsPresent
                ? $"Page **{page}** of **{(marbleBotUser.Inventory.Count - 1) / itemsPerPage + 1}**"
                : "Invalid page");
        await AddAuthorAsync(Context, marbleBotUser, embedBuilder);

        var componentBuilder = new ComponentBuilder()
            .WithButton("Previous", customId: $"inventory_previous_{marbleBotUser.Id}_{page - 1}", disabled: page <= 1)
            .WithButton("Next", customId: $"inventory_next_{marbleBotUser.Id}_{page + 1}", disabled: marbleBotUser.Inventory.Count <= page * itemsPerPage);

        await RespondAsync(embed: embedBuilder.Build(), components: componentBuilder.Build());
    }

    [ComponentInteraction("inventory_previous_*_*")]
    public async Task InventoryPreviousCommand(ulong id, int page)
    {
        User? marbleBotUser = await _userDao.GetUserAsync(id);
        await InventoryCommand(marbleBotUser, page);
    }

    [ComponentInteraction("inventory_next_*_*")]
    public async Task InventoryNextCommand(ulong id, int page)
    {
        User? marbleBotUser = await _userDao.GetUserAsync(id);
        await InventoryCommand(marbleBotUser, page);
    }

    [SlashCommand("profile", "Returns your profile.")]
    public async Task ProfileCommand(
        [Summary("user", "Which user's profile to inspect. Will inspect your own balance if left blank.")] User? marbleBotUser = null)
    {
        marbleBotUser ??= await _userDao.GetUserAsync(Context.User.Id);

        decimal balance = marbleBotUser?.Balance ?? 0;
        decimal netWorth = marbleBotUser?.NetWorth ?? 0;

        //string lastDaily = marbleBotUser == null ? "N/A" : GetLastWinString(marbleBotUser.LastDaily);
        string lastRaceWin = marbleBotUser == null ? "N/A" : GetLastWinString(marbleBotUser.LastRaceWin);
        //string lastScavenge = marbleBotUser == null ? "N/A" : GetLastWinString(marbleBotUser.LastScavenge);
        //string lastSiegeWin = marbleBotUser == null ? "N/A" : GetLastWinString(marbleBotUser.LastSiegeWin);
        //string lastWarWin = marbleBotUser == null ? "N/A" : GetLastWinString(marbleBotUser.LastWarWin);

        EmbedBuilder? builder = new EmbedBuilder()
            .WithAuthor(Context.User)
            //.WithColor(GetColor(Context))
            .WithFooter($"{_warning} | Balances are frozen for now and may be flattened in the future.")
            .AddField("Balance", $"{_unitOfMoney}{balance}", true)
            .AddField("Net Worth", $"{_unitOfMoney}{netWorth}", true);

        if (marbleBotUser != null)
        {
            builder.AddField("Daily Streak", marbleBotUser.DailyStreak, true)
                .AddField("Siege Mentions", marbleBotUser.SiegePing, true)
                .AddField("War Mentions", marbleBotUser.WarPing, true)
                .AddField("Race Wins", marbleBotUser.RaceWins, true)
                .AddField("Siege Wins", marbleBotUser.SiegeWins, true)
                .AddField("War Wins", marbleBotUser.WarWins, true)
                //.AddField("Last Daily", lastDaily, true)
                .AddField("Last Race Win", lastRaceWin, true);
            //.AddField("Last Scavenge", lastScavenge, true)
            //.AddField("Last Siege Win", lastSiegeWin, true)
            //.AddField("Last War Win", lastWarWin, true);
        }

        await RespondAsync(embed: builder.Build());
    }

    private static string GetLastWinString(DateTime lastWin)
    {
        string lastWinString = "N/A";
        const int yearThreshold = 10;
        if (lastWin.Year > yearThreshold)
        {
            long unixTime = ((DateTimeOffset)lastWin).ToUnixTimeSeconds();
            lastWinString = $"<t:{unixTime}:S>";
        }
        return lastWinString;
    }
}
