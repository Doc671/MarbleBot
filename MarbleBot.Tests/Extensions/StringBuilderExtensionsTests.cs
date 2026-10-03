using MarbleBot.Extensions;
using System.Text;

namespace MarbleBot.Tests.Extensions;

public class StringBuilderExtensionsTests
{
    [Theory]
    [InlineData("Hello,  ", new char[] { ' ' }, "Hello,")]
    [InlineData("Hello,  ", new char[] { ' ', ',' }, "Hello")]
    [InlineData("New line\n", new char[] { '\n' }, "New line")]
    [InlineData("New\nlines\n", new char[] { '\n' }, "New\nlines")]
    [InlineData("New line\r\n", new char[] { '\r', '\n' }, "New line")]
    [InlineData("New line\r\n", new char[] { '\r' }, "New line\r\n")]
    [InlineData("New line\r\n", new char[] { '\n' }, "New line\r")]
    public void TrimEnd_ReturnsTrimmedString(string value, char[] removeChars, string expected)
    {
        var stringBuilder = new StringBuilder(value);
        stringBuilder.TrimEnd(removeChars);
        Assert.Equal(expected, stringBuilder.ToString());
    }
}
