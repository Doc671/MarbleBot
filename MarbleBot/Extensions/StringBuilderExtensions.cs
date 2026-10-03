using System.Text;

namespace MarbleBot.Extensions;

public static class StringBuilderExtensions
{
    public static void TrimEnd(this StringBuilder stringBuilder, params char[] trimChars)
    {
        int i = stringBuilder.Length - 1;
        while (i >= 0 && trimChars.Contains(stringBuilder[i]))
        {
            i--;
        }

        stringBuilder.Length = i + 1;
    }
}
