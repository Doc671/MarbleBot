using System;

namespace MarbleBot.Extensions
{
    /// <summary>
    /// An extension method...
    /// </summary>
    
    public static class StringExtension
    {
        public static bool IsEmpty(this String str)
        {
            return (str == "" || str == " " || str == null || str == string.Empty);
        }
    }
}
