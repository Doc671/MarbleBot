using MarbleBot.Extensions;

namespace MarbleBot.Tests.Extensions;

public class IntExtensionsTests
{
    [Theory]
    [InlineData(1, "st")]
    [InlineData(2, "nd")]
    [InlineData(3, "rd")]
    [InlineData(4, "th")]
    [InlineData(11, "th")]
    [InlineData(12, "th")]
    [InlineData(13, "th")]
    [InlineData(21, "st")]
    [InlineData(102, "nd")]
    [InlineData(111, "th")]
    public void Ordinal_ReturnsCorrectSuffix(int number, string expectedSuffix)
    {
        string result = number.GetOrdinalSuffix();
        Assert.Equal(expectedSuffix, result);
    }
}
