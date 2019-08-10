using System;
using System.Text;
using System.Threading;

namespace MarbleBot.Extensions
{
    /// <summary> Extension methods </summary>
    public static class Extensions
    {       
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
            var chr = str.ToCharArray();
            var output = new StringBuilder();
            for (int i = 0; i < chr.Length; i++)
            {
                char c = chr[i];
                if (c != charToRemove) output.Append(c);
            }
            return output.ToString();
        }

        public static decimal ToDecimal(this string raw)
        {
            var rawNo = raw.Trim().Split(',');
            var rawNo2 = new StringBuilder();
            foreach (var nono in rawNo) rawNo2.Append(nono);
            return decimal.Parse(rawNo2.ToString());
        }

        public static int ToInt(this string raw)
        {
            var rawNo = raw.Trim().Split(',');
            var rawNo2 = new StringBuilder();
            foreach (var nono in rawNo) rawNo2.Append(nono);
            return int.Parse(rawNo2.ToString());
        }

        // Convert the string to Pascal case
        public static string ToPascalCase(this string str)
        {
            str = Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(str);
            string[] parts = str.Split(new char[] { }, StringSplitOptions.RemoveEmptyEntries);
            string result = string.Join(string.Empty, parts);
            return result;
        }
    }
}
