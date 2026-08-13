using Discord;
using MarbleBot.Extensions;

namespace MarbleBot.Tests.Extensions;

public class ColorExtensionsTests
{
    [Theory]
    [InlineData(0, 0, 0, 0f, 0f, 0f)]               // Black
    [InlineData(128, 128, 128, 0f, 0f, 0.5f)]       // Grey
    [InlineData(255, 255, 255, 0f, 0f, 1f)]         // White
    [InlineData(255, 0, 0, 0f, 1f, 1f)]             // Red
    [InlineData(0, 255, 0, 120f, 1f, 1f)]           // Green
    [InlineData(0, 0, 255, 240f, 1f, 1f)]           // Blue
    [InlineData(255, 255, 0, 60f, 1f, 1f)]          // Yellow
    [InlineData(0, 255, 255, 180f, 1f, 1f)]         // Cyan
    [InlineData(255, 0, 255, 300f, 1f, 1f)]         // Magenta
    [InlineData(128, 70, 51, 14f, 0.6f, 0.5f)]      // Chestnut
    [InlineData(61, 153, 130, 165f, 0.6f, 0.6f)]    // Zomp
    [InlineData(153, 78, 173, 287f, 0.54f, 0.67f)]   // Purpureus
    public void GetHsv_ReturnsCorrectHsvValues(int red, int green, int blue, float expectedHue, float expectedSaturation, float expectedValue)
    {
        var color = new Color(red, green, blue);
        color.GetHsv(out float hue, out float saturation, out float value);

        const float hueTolerance = 1f;
        const float saturationValueTolerance = 0.05f;

        Assert.InRange(hue, expectedHue - hueTolerance, expectedHue + hueTolerance);
        Assert.InRange(saturation, expectedSaturation - saturationValueTolerance, expectedSaturation + saturationValueTolerance);
        Assert.InRange(value, expectedValue - saturationValueTolerance, expectedValue + saturationValueTolerance);
    }

    [Theory]
    [InlineData(0, 0, 0, 0f, 0f, 0f)]               // Black
    [InlineData(0f, 0f, 0.5f, 128, 128, 128)]       // Grey
    [InlineData(0f, 0f, 1f, 255, 255, 255)]         // White
    [InlineData(0f, 1f, 1f, 255, 0, 0)]             // Red
    [InlineData(120f, 1f, 1f, 0, 255, 0)]           // Green
    [InlineData(240f, 1f, 1f, 0, 0, 255)]           // Blue
    [InlineData(60f, 1f, 1f, 255, 255, 0)]          // Yellow
    [InlineData(180f, 1f, 1f, 0, 255, 255)]         // Cyan
    [InlineData(300f, 1f, 1f, 255, 0, 255)]         // Magenta
    [InlineData(14f, 0.6f, 0.5f, 128, 70, 51)]      // Chestnut
    [InlineData(165f, 0.6f, 0.6f, 61, 153, 130)]    // Zomp
    [InlineData(287f, 0.54f, 0.67f, 153, 78, 173)]  // Purpureus
    public void FromHsv_ReturnsCorrectRgbValues(float hue, float saturation, float value, int expectedRed, int expectedGreen, int expectedBlue)
    {
        var color = ColorExtensions.FromHsv(hue, saturation, value);

        const int rgbTolerance = 2;

        Assert.InRange(color.R, expectedRed - rgbTolerance, expectedRed + rgbTolerance);
        Assert.InRange(color.G, expectedGreen - rgbTolerance, expectedGreen + rgbTolerance);
        Assert.InRange(color.B, expectedBlue - rgbTolerance, expectedBlue + rgbTolerance);
    }

    [Theory]
    [InlineData("#000000", 0, 0, 0)]         // Black
    [InlineData("#808080", 128, 128, 128)]   // Grey
    [InlineData("#FFFFFF", 255, 255, 255)]   // White
    [InlineData("#FF0000", 255, 0, 0)]       // Red
    [InlineData("#00FF00", 0, 255, 0)]       // Green
    [InlineData("#0000FF", 0, 0, 255)]       // Blue
    [InlineData("#FFFF00", 255, 255, 0)]     // Yellow
    [InlineData("#00FFFF", 0, 255, 255)]     // Cyan
    [InlineData("#FF00FF", 255, 0, 255)]     // Magenta
    [InlineData("#804633", 128, 70, 51)]     // Chestnut
    [InlineData("#3D9982", 61, 153, 130)]    // Zomp
    [InlineData("#994EAD", 153, 78, 173)]    // Purpureus
    public void FromHexCode_ReturnsCorrectRgbValues(string hex, int expectedRed, int expectedGreen, int expectedBlue)
    {
        var color = ColorExtensions.FromHexCode(hex);
        Assert.Equal(expectedRed, color.R);
        Assert.Equal(expectedGreen, color.G);
        Assert.Equal(expectedBlue, color.B);
    }
}
