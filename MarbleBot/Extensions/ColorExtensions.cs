using System.Globalization;

namespace MarbleBot.Extensions;

public static class ColorExtensions
{
    public static void GetHsv(this Color color, out float hue, out float saturation, out float value)
    {
        float redFloat = color.R / 255f;
        float greenFloat = color.G / 255f;
        float blueFloat = color.B / 255f;

        float min = MathF.Min(redFloat, MathF.Min(greenFloat, blueFloat));
        float max = MathF.Max(redFloat, MathF.Max(greenFloat, blueFloat));

        bool redLargerThanGreen = redFloat > greenFloat;
        bool redLargerThanBlue = redFloat > blueFloat;
        bool greenLargerThanBlue = greenFloat > blueFloat;

        value = max;

        if (max < float.Epsilon)
        {
            hue = saturation = 0;
            return;
        }

        saturation = 1f - min / max;

        if (MathF.Abs(max - min) < float.Epsilon)
        {
            hue = 0;
            return;
        }

        if (redLargerThanGreen && redLargerThanBlue)
        {
            hue = 60f * (greenFloat - blueFloat) / (max - min);
        }
        else if (!redLargerThanGreen && greenLargerThanBlue)
        {
            hue = 60f * (blueFloat - redFloat) / (max - min) + 120f;
        }
        else
        {
            hue = 60f * (redFloat - greenFloat) / (max - min) + 240f;
        }
    }

    public static Color FromHsv(float hue, float saturation, float value)
    {
        float chroma = value * saturation;
        float secondary = chroma * (1 - MathF.Abs(hue / 60 % 2 - 1));
        float brightnessShift = value - chroma;

        float red, green, blue;
        switch (hue)
        {
            case >= 0 and < 60:
                red = chroma;
                green = secondary;
                blue = 0;
                break;
            case >= 60 and < 120:
                red = secondary;
                green = chroma;
                blue = 0;
                break;
            case >= 120 and < 180:
                red = 0;
                green = chroma;
                blue = secondary;
                break;
            case >= 180 and < 240:
                red = 0;
                green = secondary;
                blue = chroma;
                break;
            case >= 240 and < 300:
                red = secondary;
                green = 0;
                blue = chroma;
                break;
            default:
                red = chroma;
                green = 0;
                blue = secondary;
                break;
        }

        red += brightnessShift;
        green += brightnessShift;
        blue += brightnessShift;

        byte redByte = Convert.ToByte(MathF.Round(red * 255, MidpointRounding.AwayFromZero));
        byte greenByte = Convert.ToByte(MathF.Round(green * 255, MidpointRounding.AwayFromZero));
        byte blueByte = Convert.ToByte(MathF.Round(blue * 255, MidpointRounding.AwayFromZero));

        return new Color(redByte, greenByte, blueByte);
    }

    public static Color FromHexCode(string hexCode)
    {
        if (string.IsNullOrWhiteSpace(hexCode))
        {
            throw new ArgumentException("Hex code cannot be null or whitespace.", nameof(hexCode));
        }

        hexCode = hexCode.RemoveChar('#');

        if (hexCode.Length != 6)
        {
            throw new ArgumentException("Hex code must be 6 characters long.", nameof(hexCode));
        }

        if (!uint.TryParse(hexCode, NumberStyles.HexNumber, null, out uint rawColor))
        {
            throw new ArgumentException("Hex code must be a valid hexadecimal number.", nameof(hexCode));
        }

        return new Color(rawColor);
    }
}
