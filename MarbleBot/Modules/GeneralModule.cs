using MarbleBot.Services;
using MarbleBot.Utilities;
using System.Text;

namespace MarbleBot.Modules;

public class GeneralModule : MarbleBotSlashCommandModule
{
    private readonly AutoModFilterService _autoModFilterService;

    public GeneralModule(AutoModFilterService autoModFilterService)
    {
        _autoModFilterService = autoModFilterService;
    }

    [SlashCommand("choose", "Randomly chooses between choices.")]
    public async Task ChooseCommand(
        [Summary("choices", "Choices to choose from.")] string choices,
        [Summary("separator", "Which character to separate the choices by (default: comma ,).")] char separator)
    {
        string[] choiceArray = choices.Split(separator)
            .Select(choice =>
            {
                string trimmedChoice = choice.Trim();
                return _autoModFilterService.FilterAsync(trimmedChoice, Context).Result;
            })
            .ToArray();
        int randIndex = Random.Shared.Next(choiceArray.Length);
        await RespondAsync(choiceArray[randIndex]);
    }

    [SlashCommand("coben", "Calculates the Chance of Being Eliminated Next (COBEN) for each given contestant.")]
    public async Task CobenCommand(
        [Summary("names", "A comma-separated list of contestant names (e.g. Red,Green,Blue).")] string names,
        [Summary("currentPoints", "A comma-separated list of points corresponding to the contestant names (e.g. 200,150,100).")] string currentPoints,
        [Summary("pointsToDistribute", "A comma-separated list of the points that will be given after the next event (e.g. 300,200,100).")] string pointsToDistribute,
        [Summary("eliminationCount", "The number of eliminations that will happen after the next event."), MinValue(1)] int eliminationCount = 1,
        [Summary("simulationCount", "The number of times the event will be simulated to provide the COBEN estimate."), MinValue(1), MaxValue(2_000_000)] int simulationCount = 1_000_000,
        [Summary("showIntervals", "Whether to show the Wilson intervals. These are estimated ranges for the COBEN value.")] bool showIntervals = false,
        [Summary("decimalPlaces", "How many decimal places to show COBEN values to."), MinValue(0), MaxValue(7)] int decimalPlaces = 2)
    {
        string[] nameArray = names.Split(',')
            .Select(name =>
            {
                string trimmedName = name.Trim();
                return _autoModFilterService.FilterAsync(trimmedName, Context).Result;
            })
            .ToArray();

        if (nameArray.Length == 0)
        {
            await RespondWithErrorAsync("At least one name must be provided.");
            return;
        }

        long[] currentPointsArray = currentPoints.Split(',')
            .Select(point => long.Parse(point.Trim()))
            .ToArray();

        if (nameArray.Length != currentPointsArray.Length)
        {
            await RespondWithErrorAsync("The number of names must match the number of points.");
            return;
        }

        long[] pointsToDistributeArray = pointsToDistribute.Split(",")
            .Select(pointsToDistribute => long.Parse(pointsToDistribute.Trim()))
            .ToArray();

        if (pointsToDistributeArray.Length > nameArray.Length)
        {
            await RespondWithErrorAsync("The number of points to distribute cannot be larger than the number of contestants.");
            return;
        }

        DateTime startTime = DateTime.Now;
        await DeferAsync();

        Array.Sort(currentPointsArray, nameArray, Comparer<long>.Create((a, b) => b.CompareTo(a)));
        
        Dictionary<string, CobenResult> cobenValues;
        if (nameArray.Length == eliminationCount)
        {
            cobenValues = nameArray.ToDictionary(name => name, name => new CobenResult(1f, 1f, 1f));
        }
        else
        {
            cobenValues = await Task.Run(() => CobenCalculator.Calculate(nameArray, currentPointsArray, pointsToDistributeArray, eliminationCount, simulationCount, showIntervals));
        }

        var stringBuilder = new StringBuilder();
        string percentageFormat = $"P{decimalPlaces}";
        for (int i = 0; i < nameArray.Length; i++)
        {
            int place = i + 1;
            string placeOrdinal = place.GetOrdinalSuffix();
            string name = nameArray[i];
            CobenResult cobenResult = cobenValues[name];
            string chanceString = cobenResult.Chance.ToString(percentageFormat);
            if (showIntervals)
            {
                string lowerIntervalString = cobenResult.LowerInterval.ToString(percentageFormat);
                string upperIntervalString = cobenResult.UpperInterval.ToString(percentageFormat);
                stringBuilder.AppendLine($"{place}{placeOrdinal}: {name.ToDiscordBold()} (Points: {currentPointsArray[i]}, COBEN: **{chanceString}** [{lowerIntervalString}, {upperIntervalString}])");
            }
            else
            {
                stringBuilder.AppendLine($"{place}{placeOrdinal}: {name.ToDiscordBold()} (Points: {currentPointsArray[i]}, COBEN: **{chanceString}**)");
            }
        }

        DateTime endTime = DateTime.Now;
        TimeSpan timeTaken = endTime - startTime;
        string timeTakenString;
        if (timeTaken.TotalHours >= 1)
        {
            timeTakenString = timeTaken.ToString(@"h\:mm\:ss");
        }
        else if (timeTaken.TotalMinutes >= 1)
        {
            timeTakenString = timeTaken.ToString(@"m\:ss");
        }
        else
        {
            timeTakenString = $@"{timeTaken:s\.ff} seconds";
        }

        var embedBuilder = new EmbedBuilder()
            .WithTitle("Chance of Being Eliminated Next (COBEN)")
            .WithDescription(stringBuilder.ToString())
            .WithFooter($"Points to be distributed after the next event: {pointsToDistribute}\n\nEliminations after the next event: {eliminationCount}\nNumber of simulations used to calculate: {simulationCount}\nTime taken to calculate: {timeTakenString}");

        await FollowupAsync(embed: embedBuilder.Build());
    }

    [SlashCommand("random", "Gives a random number between the given range.")]
    public async Task RandomCommand(
        [Summary("min", "The minimum number (inclusive).")] float min,
        [Summary("max", "The maximum number (inclusive).")] float max,
        [Summary("decimalPlaces", "The number of decimal places to give.")] int decimalPlaces = 0)
    {
        if (min > max)
        {
            (max, min) = (min, max);
        }

        if (decimalPlaces == 0
            && min is >= int.MinValue and <= int.MaxValue
            && max is >= int.MinValue and <= int.MaxValue)
        {
            int randomInt = Random.Shared.Next((int)min, (int)max + 1);
            await RespondAsync($"Random number between {min} and {max}: **{randomInt}**");
            return;
        }

        float randomNumber = Random.Shared.NextSingle() * (max - min) + min;
        string formattedNumber = randomNumber.ToString($"F{decimalPlaces}");
        await RespondAsync($"Random number between {min} and {max}: **{formattedNumber}**");
    }

    [SlashCommand("rate", "Gives a random rating out of 10.")]
    public async Task RateCommand([Summary("subject", "The subject to rate.")] string subject)
    {
        subject = await _autoModFilterService.FilterAsync(subject, Context);

        int rating = Random.Shared.Next(0, 11);

        string emoji = rating switch
        {
            0 => ":no_entry_sign:",
            1 => ":nauseated_face:",
            2 => ":rage:",
            3 => ":angry:",
            4 => ":slight_frown:",
            5 => ":neutral_face:",
            6 => ":slight_smile:",
            7 => ":grinning:",
            8 => ":thumbsup:",
            9 => ":white_check_mark:",
            10 => ":rofl:",
            _ => ":thinking:"
        };

        await RespondAsync($"**{Context.User.Username}**, I rate **{subject}** a **{rating}**/10. {emoji}");
    }

    [SlashCommand("reverse", "Reverses the given text.")]
    public async Task ReverseCommand([Summary("text", "Text to reverse.")] string text)
    {
        text = await _autoModFilterService.FilterAsync(text, Context);
        string reverseText = string.Concat(text.Reverse());
        reverseText = await _autoModFilterService.FilterAsync(reverseText, Context);
        await RespondAsync(reverseText);
    }
}
