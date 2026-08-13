using MarbleBot.Data.Repositories;
using MarbleBot.Entities;
using MarbleBot.Modals;
using MarbleBot.Services;
using System.Text;

namespace MarbleBot.Modules;

public class EconomyModule : MarbleBotSlashCommandModule
{
    private readonly UserDao _userDao;
    private readonly MarbleAppearanceDao _marbleAppearanceDao;
    private readonly MarbleRendererService _marbleRendererService;

    private const int _marbleImageDimension = 64;

    public EconomyModule(UserDao userDao, MarbleAppearanceDao marbleAppearanceDao, MarbleRendererService marbleRendererService)
    {
        _userDao = userDao;
        _marbleAppearanceDao = marbleAppearanceDao;
        _marbleRendererService = marbleRendererService;
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

    [SlashCommand("changeappearance", "Allows you to change your marble's appearance.")]
    public async Task ChangeAppearanceCommand()
    {
        await RespondWithModalAsync<MarbleAppearanceModal>($"appearance_modal_{Context.User.Id}");
    }

    [ModalInteraction("appearance_modal_*")]
    public async Task ChangeAppearanceCommand(ulong userId, MarbleAppearanceModal modal)
    {
        User marbleBotUser = await _userDao.GetOrCreateUserAsync(userId);

        var colors = new List<Color>();
        string[] hexCodes = modal.HexCodes?.Split('\n') ?? Array.Empty<string>();
        foreach (string hexCode in hexCodes)
        {
            const int maxColors = 8;
            if (Color.TryParse(hexCode.Trim(), out Color color) && colors.Count <= maxColors)
            {
                colors.Add(color);
            }
            else
            {
                await RespondWithErrorAsync($"Invalid color code: {hexCode}.");
                return;
            }
        }

        float? gradientAngleDegrees = null;
        if (modal.MarbleAppearanceType == MarbleAppearanceType.LinearGradient && !string.IsNullOrWhiteSpace(modal.GradientAngle))
        {
            if (float.TryParse(modal.GradientAngle, out float angle))
            {
                gradientAngleDegrees = angle;
            }
            else
            {
                await RespondWithErrorAsync($"Invalid gradient angle: {modal.GradientAngle}.");
                return;
            }
        }

        await _marbleAppearanceDao.UpdateAppearanceAsync(marbleBotUser.Id, modal.MarbleAppearanceType, colors, gradientAngleDegrees);
        await RespondAsync($"Your marble appearance has been updated successfully!", ephemeral: true);
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
        ulong id = Context.User.Id;
        marbleBotUser ??= await _userDao.GetUserAsync(id);

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

        MemoryStream appearanceStream = await _marbleRendererService.RenderMarbleApperanceAsync(id, 0,
            _marbleImageDimension, _marbleImageDimension, _marbleImageDimension / 2, _marbleImageDimension / 2, _marbleImageDimension / 2);

        builder.WithThumbnailUrl($"attachment://MarbleAppearance{id}.png");

        await RespondWithFileAsync(appearanceStream, $"MarbleAppearance{id}.png", embed: builder.Build());
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

    [SlashCommand("showappearance", "Returns your current marble appearance.")]
    public async Task ShowAppearanceCommand(string backgroundHexCode = "#000000")
    {
        ulong id = Context.User.Id;
        User? marbleBotUser = await _userDao.GetUserAsync(id);
        if (marbleBotUser == null)
        {
            await RespondWithErrorAsync("You don't have a marble appearance yet. Use /changeappearance to set one.");
            return;
        }

        uint background;
        if (backgroundHexCode.StartsWith('#'))
        {
            background = uint.Parse(backgroundHexCode[1..], System.Globalization.NumberStyles.HexNumber);
        }
        else
        {
            background = uint.Parse(backgroundHexCode, System.Globalization.NumberStyles.HexNumber);
        }

        if (backgroundHexCode.Length is 6 or 7 && background is not 0)
        {
            background |= 0xFF000000;
        }

        MemoryStream appearanceStream = await _marbleRendererService.RenderMarbleApperanceAsync(id, background, 
            _marbleImageDimension, _marbleImageDimension, _marbleImageDimension / 2, _marbleImageDimension / 2, _marbleImageDimension / 2);

        await RespondWithFileAsync(appearanceStream, $"MarbleAppearance{id}.png", "Current marble appearance.");
    }
}
