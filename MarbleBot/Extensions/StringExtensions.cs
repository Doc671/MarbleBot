using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MarbleBot.Extensions;

public static partial class StringExtensions
{
    public static string CamelToTitleCase(this string str)
    {
        if (string.IsNullOrEmpty(str))
        {
            return str;
        }

        var output = new StringBuilder();
        for (int i = 0; i < str.Length; i++)
        {
            output.Append(i == 0 ? char.ToUpper(str[i]) : str[i]);

            // Add a space if the next character is a capital letter (indicating a new word)
            if (i < str.Length - 1 && char.IsUpper(str[i + 1]))
            {
                output.Append(' ');
            }
        }

        return output.ToString();
    }

    public static string RemoveChar(this string str, char charToRemove)
    {
        return str.Replace(charToRemove.ToString(), string.Empty);
    }

    public static string ToDiscordBold(this string str)
    {
        var output = new StringBuilder();
        output.Append("**");
        foreach (char c in str)
        {
            output.Append(c switch
            {
                '\\' => "",
                '*' => "\\*",
                _ => c
            });
        }

        output.Append("**");
        return output.ToString();
    }

    public static string ToPascalCase(this string str, bool removeWhitespaceOnly = true)
    {
        if (string.IsNullOrEmpty(str))
        {
            return str;
        }

        str = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(str.ToLowerInvariant());
        string[] parts;
        if (removeWhitespaceOnly)
        {
            parts = str.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries);
        }
        else
        {
            parts = NonWordRegex()
                .Split(str)
                .Where(part => !string.IsNullOrEmpty(part))
                .ToArray();
        }
        return string.Join(string.Empty, parts);
    }

    [GeneratedRegex(@"[^a-zA-Z0-9]")]
    private static partial Regex NonWordRegex();
}
