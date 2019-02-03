using System;
using System.Text;

namespace MarbleBot.Extensions
{
    /// <summary>
    /// Extension methods
    /// </summary>
    
    public static class Extensions
    {
        public static bool IsEmpty(this String str) {
            return (str == "" || str == " " || str == null || str == string.Empty || string.IsNullOrEmpty(str) || string.IsNullOrWhiteSpace(str));
        }

        public static string Ordinal(this int no) {
            var ns = no.ToString();
            if (ns.Length > 2) ns.Substring((ns.Length - 3));
            no = int.Parse(ns);
            if (no > 20) no = no % 10;
            string ord;
            switch (no) {
                case 1: ord = "st"; break;
                case 2: ord = "nd"; break;
                case 3: ord = "rd"; break;
                default: ord = "th"; break;
            }
            return ord;
        }

        public static string RemoveChar(this String str, char charToRemove) {
            var chr = str.ToCharArray();
            var output = new StringBuilder();
            foreach (var c in chr) {
                if (c != charToRemove) output.Append(c);
            }
            return output.ToString();
        }

        public static int ToInt(this String raw)  {
            var rawNo = raw.Trim().Split(',');
            var rawNo2 = new StringBuilder();
            foreach (var nono in rawNo) rawNo2.Append(nono);
            return int.Parse(rawNo2.ToString());
        }

        public static decimal ToDecimal(this String raw) {
            var rawNo = raw.Trim().Split(',');
            var rawNo2 = new StringBuilder();
            foreach (var nono in rawNo) rawNo2.Append(nono);
            return decimal.Parse(rawNo2.ToString());
        }
    }
}
