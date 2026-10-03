using MarbleBot.Services;
using MarbleBot.Services.Docs;
using System.Text;

namespace MarbleBot.Modules.Thyme;

[Group("propertylist", "Gets info from the Thyme Property List Google Doc.")]
public class PropertyListModule : MarbleBotSlashCommandModule
{
    private readonly ThymePropertyListService _propertyListService;
    private readonly GuildColorProvider _guildColorProvider;
    private readonly string _documentId;

    public PropertyListModule(ThymePropertyListService propertyListService, GuildColorProvider guildColorProvider, ExternalResources externalResources)
    {
        _propertyListService = propertyListService;
        _guildColorProvider = guildColorProvider;
        _documentId = externalResources.ThymePropertyListId;
    }

    [SlashCommand("info", "Returns info about a property or function in the Thyme Property List doc.")]
    public async Task InfoCommand([Summary("name", "The (case-sensitive) name of the property or function.")] string name)
    {
        await DeferAsync();

        IPropertyListInfo? rawInfo = await _propertyListService.GetPropertyInfoAsync(name);
        if (rawInfo == null)
        {
            await FollowupWithErrorAsync($"Could not find the requested property or function `{name}` in the Property List doc.");
            return;
        }

        var embedBuilder = new EmbedBuilder()
            .WithColor(await _guildColorProvider.GetColorAsync(Context))
            .WithTitle(rawInfo.Name);

        switch (rawInfo)
        {
            case OperationInfo operationInfo:
                if (operationInfo.Description != null)
                {
                    embedBuilder.WithDescription(operationInfo.Description);
                }

                embedBuilder.AddField("Operation", operationInfo.Operation);

                if (operationInfo.Parameters != null)
                {
                    var stringBuilder = new StringBuilder();
                    foreach (ParameterInfo parameter in operationInfo.Parameters)
                    {
                        stringBuilder.AppendLine($"- `{parameter.Name}` ({parameter.Type})");
                    }
                    embedBuilder.AddField("Parameters", stringBuilder.ToString());
                }
                break;

            case FunctionInfo functionInfo:
                if (functionInfo.Description != null)
                {
                    embedBuilder.WithDescription(functionInfo.Description);
                }

                if (functionInfo.Parameters != null)
                {
                    var stringBuilder = new StringBuilder();
                    foreach (ParameterInfo parameter in functionInfo.Parameters)
                    {
                        stringBuilder.AppendLine($"- `{parameter.Name}` ({parameter.Type})");
                    }
                    embedBuilder.AddField("Parameters", stringBuilder.ToString());
                }

                if (functionInfo.Code != null)
                {
                    embedBuilder.AddField("Code", functionInfo.Code);
                }
                break;

            case PropertyInfo propertyInfo:
                embedBuilder.WithDescription(propertyInfo.Description)
                    .AddField("Type", propertyInfo.Type);

                if (propertyInfo.Default != null)
                {
                    embedBuilder.AddField("Default", propertyInfo.Default);
                }
                break;

            case TextAttributeInfo textAttributeInfo:
                embedBuilder.WithDescription(textAttributeInfo.Description)
                    .AddField("Type", textAttributeInfo.Type);
                break;
        }

        await FollowupAsync(embed: embedBuilder.Build());
    }

    [SlashCommand("list", "Returns a list of properties or functions within a category in the Thyme Property List doc.")]
    public async Task ListCommand([Summary("categoryName", "The (case-sensitive) name of the category to list.")] string categoryName)
    {
        await DeferAsync();

        IReadOnlySet<string>? categoryProperties = await _propertyListService.GetAllPropertiesInCategoryAsync(categoryName);
        if (categoryProperties == null)
        {
            await FollowupWithErrorAsync($"Could not find the requested category \"{categoryName}\" in the Thyme Property List.");
            return;
        }

        var stringBuilder = new StringBuilder();
        foreach (string name in categoryProperties)
        {
            stringBuilder.AppendLine($"- {name}");
        }

        if (stringBuilder.Length > EmbedFieldBuilder.MaxFieldValueLength)
        {
            stringBuilder.Length = EmbedFieldBuilder.MaxFieldValueLength;
        }

        var embedBuilder = new EmbedBuilder()
            .WithColor(await _guildColorProvider.GetColorAsync(Context))
            .WithTitle(categoryName)
            .WithDescription(stringBuilder.ToString());

        await FollowupAsync(embed: embedBuilder.Build());
    }

    [SlashCommand("listcategories", "Returns a list of categories in the Thyme Property List doc.")]
    public async Task ListCategoriesCommand([Summary("page", "The page number to show. Each page contains 20 entries.")] int page = 1)
    {
        await DeferAsync();

        (EmbedBuilder? embedBuilder, ComponentBuilder? componentBuilder) = await ListCategoriesAsync(page);
        if (embedBuilder == null || componentBuilder == null)
        {
            await FollowupWithErrorAsync("Could not read the Thyme Property List.");
            return;
        }

        await FollowupAsync(embed: embedBuilder.Build(), components: componentBuilder.Build());
    }

    [ComponentInteraction("propertylist_listcategories_*", ignoreGroupNames: true)]
    public async Task ListCategoriesComponentInteraction(int page)
    {
        (EmbedBuilder? embedBuilder, ComponentBuilder? componentBuilder) = await ListCategoriesAsync(page);
        if (embedBuilder == null || componentBuilder == null)
        {
            await RespondWithErrorAsync("Could not read the Thyme Property List.");
            return;
        }

        await RespondAsync(embed: embedBuilder.Build(), components: componentBuilder.Build());
    }

    private async Task<(EmbedBuilder?, ComponentBuilder?)> ListCategoriesAsync(int page)
    {
        IReadOnlyList<string>? categoryNames = await _propertyListService.GetCategoryNamesAsync(page);
        if (categoryNames == null)
        {
            return (null, null);
        }

        var stringBuilder = new StringBuilder();
        foreach (string categoryName in categoryNames)
        {
            stringBuilder.AppendLine($"- {categoryName}");
        }

        var embedBuilder = new EmbedBuilder()
            .WithColor(await _guildColorProvider.GetColorAsync(Context))
            .WithTitle("Categories")
            .WithDescription(stringBuilder.ToString());

        int categoryCount = (await _propertyListService.GetCategoryCountAsync())!.Value;
        var componentBuilder = new ComponentBuilder()
            .WithButton("Previous", customId: $"propertylist_listcategories_{page - 1}", disabled: page <= 1)
            .WithButton("Next", customId: $"propertylist_listcategories_{page + 1}", disabled: categoryCount <= page * 20);

        return (embedBuilder, componentBuilder);
    }
}
