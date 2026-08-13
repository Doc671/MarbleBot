using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MarbleBot.Data.Converters;

public class UlongToLongConverter : ValueConverter<ulong, long>
{
    public UlongToLongConverter() : base(ulongValue => unchecked((long)ulongValue), longValue => unchecked((ulong)longValue))
    {
    }
}
