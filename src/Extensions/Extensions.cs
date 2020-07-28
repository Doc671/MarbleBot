using System;
using System.Drawing;
using System.Text;
using System.Threading;

namespace MarbleBot.Extensions
{
    public static class Extensions
    {
        public static string CamelToTitleCase(this string str)
        {
            var output = new StringBuilder();
            for (int i = 0; i < str.Length; i++)
            {
                if (i == 0)
                {
                    output.Append(char.ToUpper(str[i]));
                }
                else
                {
                    output.Append(str[i]);
                }

                // Add a space if the next character is a capital letter (indicating a new word)
                if (i != 0 && i < str.Length - 1 && char.IsUpper(str[i + 1]))
                {
                    output.Append(" ");
                }
            }

            return output.ToString();
        }

        public static void GetHsv(this Color color, out float hue, out float saturation, out float value)
        {
            int max = Math.Max(color.R, Math.Max(color.G, color.B));
            int min = Math.Min(color.R, Math.Min(color.G, color.B));

            hue = color.GetHue();
            saturation = max == 0 ? 0 : 1f - 1f * min / max;
            value = max / 255f;
        }

        public static string Ordinal(this int no)
        {
            if (no > 20)
            {
                no %= 10;
            }

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
            var output = new StringBuilder();
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] != charToRemove)
                {
                    output.Append(str[i]);
                }
            }

            return output.ToString();
        }

        // Convert the string to Pascal case
        public static string ToPascalCase(this string str)
        {
            str = Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(str);
            string[] parts = str.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries);
            return string.Join(string.Empty, parts);
        }
    }
}
