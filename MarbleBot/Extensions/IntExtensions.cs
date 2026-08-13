namespace MarbleBot.Extensions;

public static class IntExtensions
{
    public static string GetOrdinalSuffix(this int number)
    {
        int hundredRemainder = Math.Abs(number % 100);
        if (hundredRemainder is >= 11 and <= 13)
        {
            return "th";
        }

        return Math.Abs(number % 10) switch
        {
            1 => "st",
            2 => "nd",
            3 => "rd",
            _ => "th"
        };
    }
}
