using MarbleBot.Extensions;

namespace MarbleBot.Tests.Extensions;

public class StringExtensionsTests
{
    [Theory]
    [InlineData("helloWorld", "Hello World")]
    [InlineData("aLongerSentenceWithMoreWords", "A Longer Sentence With More Words")]
    [InlineData("a", "A")]
    [InlineData("", "")]
    public void CamelToTitleCase_ReturnsCorrectlyFormattedString(string input, string expected)
    {
        string result = input.CamelToTitleCase();
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("hello world", 'o', "hell wrld")]
    [InlineData("a longer sentence with more words", 'e', "a longr sntnc with mor words")]
    [InlineData("test", 'z', "test")]
    [InlineData("", 'a', "")]
    public void RemoveChar_ReturnsStringWithCharacterRemoved(string input, char charToRemove, string expected)
    {
        string result = input.RemoveChar(charToRemove);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("hello", "**hello**")]
    [InlineData("correction*", "**correction\\***")]
    [InlineData("in*si*de", "**in\\*si\\*de**")]
    [InlineData("*both sides **", "**\\*both sides \\*\\***")]
    public void ToDiscordBold_ReturnsCorrectlyFormattedString(string input, string expected)
    {
        string result = input.ToDiscordBold();
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("hello world", true, "HelloWorld")]
    [InlineData("a longer sentence with more words", true, "ALongerSentenceWithMoreWords")]
    [InlineData("test", true, "Test")]
    [InlineData("scary_snake_case_appearance", false, "ScarySnakeCaseAppearance")]
    [InlineData("", true, "")]
    public void ToPascalCase_ReturnsStringInPascalCase(string input, bool removeWhitespaceOnly, string expected)
    {
        string result = input.ToPascalCase(removeWhitespaceOnly);
        Assert.Equal(expected, result);
    }
}
