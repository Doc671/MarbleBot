using MarbleBot.Services;
using MarbleBot.Services.Docs;
using System.Text;

namespace MarbleBot.Modules.Thyme;

[Group("additionalfunctions", "Gets info from the Thyme Additional Functions Google Doc.")]
public class AdditionalFunctionsModule : MarbleBotSlashCommandModule
{
    private readonly ThymeAdditionalFunctionsService _additionalFunctionsService;
    private readonly GuildColorProvider _guildColorProvider;
    private readonly string _documentId;

    public AdditionalFunctionsModule(ThymeAdditionalFunctionsService additionalFunctionsService, GuildColorProvider guildColorProvider, ExternalResources externalResources)
    {
        _additionalFunctionsService = additionalFunctionsService;
        _guildColorProvider = guildColorProvider;
        _documentId = externalResources.ThymeAdditionalFunctionsId;
    }

    [SlashCommand("info", "Returns info about a function in the Thyme Additional Functions doc.")]
    public async Task InfoCommand(string name)
    {
        await DeferAsync();

        AdditionalFunctionInfo? function = await _additionalFunctionsService.GetFunctionInfoAsync(name);
        if (function == null)
        {
            await FollowupWithErrorAsync("Could not find the requested function in the Additional Functions doc. Use `/additionalfunctions list` to see options.");
            return;
        }

        var parameterBuilder = new StringBuilder();
        foreach (ParameterInfo parameter in function.Parameters)
        {
            parameterBuilder.AppendLine($"- {parameter.Name} ({parameter.Type}): {parameter.Description}");
        }

        string embedDescription = function.Credit == null 
            ? function.Description 
            : $"{function.Credit}\n\n{function.Description}";

        var embedBuilder = new EmbedBuilder()
            .WithColor(await _guildColorProvider.GetColorAsync(Context))
            .WithTitle(function.Name)
            .WithDescription(embedDescription)
            .AddField("Usage", function.Usage, true)
            .AddField("Example", function.Example, true)
            .AddField("Parameters", parameterBuilder.ToString());

        string code = function.Code;
        if (code.Length > EmbedFieldBuilder.MaxFieldValueLength)
        {
            code = code.RemoveChar(' ')
                .RemoveChar('\n')
                .Insert(5, "\n");   // Re-insert the line break between the beginning of the code block and the code itself

            if (code.Length > EmbedFieldBuilder.MaxFieldValueLength)
            {
                code = code[0..EmbedFieldBuilder.MaxFieldValueLength];
                embedBuilder.WithFooter($"The function is too long to fit within this message. For the full function, please see https://docs.google.com/document/d/{_documentId}.");
            }
            else
            {
                embedBuilder.WithFooter("This function is minified to fit within this message.");
            }
        }

        embedBuilder.AddField("Code", code);

        await FollowupAsync(embed: embedBuilder.Build());
    }

    [SlashCommand("list", "Lists functions from the Thyme Additional Functions doc.")]
    public async Task ListCommand(string category = "Mathematical")
    {
        await DeferAsync();

        string? result = await _additionalFunctionsService.ListHeadersAsync(category);
        if (string.IsNullOrEmpty(result))
        {
            await FollowupWithErrorAsync("Could not access the Thyme Additional Functions doc.");
            return;
        }

        var embedBuilder = new EmbedBuilder()
            .WithColor(await _guildColorProvider.GetColorAsync(Context))
            .WithTitle($"Thyme Additional Functions: {category}")
            .WithDescription(result);

        var selectMenuBuilder = new SelectMenuBuilder()
            .WithCustomId("additionalfunc_select")
            .WithOptions(_additionalFunctionsService.GetTabNames()!
                .Select(name => new SelectMenuOptionBuilder(name, name))
                .ToList());

        var componentBuilder = new ComponentBuilder()
            .WithSelectMenu(selectMenuBuilder);

        await FollowupAsync(embed: embedBuilder.Build(), components: componentBuilder.Build());
    }

    [ComponentInteraction("additionalfunc_select", ignoreGroupNames: true)]
    public async Task ListSelectCommand(string[] selectedValues)
    {
        await ListCommand(selectedValues[0]);
    }
}
