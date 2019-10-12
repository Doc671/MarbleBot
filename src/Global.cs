using System;

namespace MarbleBot
{
    /// <summary> Contains global variables. </summary>
    internal static class Global
    {
        internal const string UoM = "<:unitofmoney:372385317581488128>";
        internal static ushort DailyTimeout { get; set; } = 48;
        internal static Random Rand { get; } = new Random();
        internal static DateTime StartTime { get; set; }
    }
}
