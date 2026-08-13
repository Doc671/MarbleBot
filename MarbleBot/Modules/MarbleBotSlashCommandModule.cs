using MarbleBot.Entities;

namespace MarbleBot.Modules;

public class MarbleBotSlashCommandModule : InteractionModuleBase<SocketInteractionContext>
{
    protected const string _unitOfMoney = "<:unitofmoney:372385317581488128>";
    protected const string _warning = "⚠️";

    private const string _leftArrowString = "⬅️";
    private Emote? _leftArrow;
    protected Emote LeftArrow => _leftArrow ??= Emote.Parse(_leftArrowString);

    private const string _rightArrowString = "➡️";
    private Emote? _rightArrow;
    protected Emote RightArrow => _rightArrow ??= Emote.Parse(_rightArrowString);

    protected static async Task AddAuthorAsync(IInteractionContext context, User marbleBotUser, EmbedBuilder builder)
    {
        var author = await context.Client.GetUserAsync(marbleBotUser.Id);
        if (author == null)
        {
            builder.WithTitle($"{marbleBotUser.Username} ({marbleBotUser.GlobalName})");
        }
        else
        {
            builder.WithAuthor(author);
        }
    }

    protected async Task RespondWithErrorAsync(string message)
    {
        await RespondAsync($"{_warning} | {message}", ephemeral: true);
    }
}
