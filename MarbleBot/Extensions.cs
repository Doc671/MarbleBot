using System;
using System.Text;
using System.Threading;

namespace MarbleBot.Extensions
{
    /// <summary> Extension methods </summary>
    public static class Extensions
    {
        public static string CamelToTitleCase(this string str)
        {
            var output = new StringBuilder();
            for (int i = 0; i < str.Length; i++)
            {
                if (i == 0)
                    output.Append(char.ToUpper(str[i]));
                else
                    output.Append(str[i]);

                // Add a space if the next character is a capital letter (indicating a new word)
                if (i != 0 && i < str.Length - 1 && char.IsUpper(str[i + 1]))
                    output.Append(" ");
            }
            return output.ToString();
        }

        public static bool IsEmpty(this string str)
            => string.Compare(str, "", true) == 0 || string.Compare(str, " ", true) == 0 || str == null || str == string.Empty 
            || string.IsNullOrEmpty(str) || string.IsNullOrWhiteSpace(str);

        public static string Ordinal(this int no)
        {
            if (no > 20) no %= 10;
            return no switch
            {
                1 => "st",
                2 => "nd",
                3 => "rd",
                _ => "th"
            };
        }

        public static string RemoveChar(this string str, char charToRemove)
        {
            var charArray = str.ToCharArray();
            var output = new StringBuilder();
            for (int i = 0; i < charArray.Length; i++)
            {
                char c = charArray[i];
                if (c != charToRemove) output.Append(c);
            }
            return output.ToString();
        }

        // Convert the string to Pascal case
        public static string ToPascalCase(this string str)
        {
            str = Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(str);
            string[] parts = str.Split(new char[] { }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(string.Empty, parts);
        }
    }
}
