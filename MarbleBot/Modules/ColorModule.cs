namespace MarbleBot.Modules;

public abstract class ColorModuleBase : MarbleBotSlashCommandModule
{
    [SlashCommand("hex", "Shows RGB and HSV values for the given hex code.")]
    public async Task HexCommand([Summary("hex", "6 digit hex code."), MinLength(6)] string hexCode)
    {
        Color colour = ColorExtensions.FromHexCode(hexCode);
        colour.GetHsv(out float hue, out float saturation, out float value);
        var embedBuilder = new EmbedBuilder()
            .AddField("RGB", $"Red: **{colour.R}**\nGreen: **{colour.G}**\nBlue: **{colour.B}**", true)
            .AddField("HSV", $"Hue: **{hue:0.##}°**\nSaturation: **{saturation:n2}**\nValue: **{value:n2}**", true)
            .AddField("Hex Code", $"#**{colour.R:X2}{colour.G:X2}{colour.B:X2}**")
            .WithColor(colour);

        await RespondAsync(embed: embedBuilder.Build());
    }

    [SlashCommand("hsv", "Shows RGB values and the hex code for the given HSV values.")]
    public async Task HsvCommand(
        [Summary("hue", "Value from 0 to 360.")] float hue,
        [Summary("saturation", "Value from 0 to 1."), MinValue(0), MaxValue(1)] float saturation,
        [Summary("value", "Value from 0 to 1."), MinValue(0), MaxValue(1)] float value)
    {
        if (hue is < 0f or > 360f)
        {
            hue %= 360f;
        }

        if (saturation > 1f + float.Epsilon)
        {
            if (saturation > 100f + float.Epsilon)
            {
                saturation = 1f;
            }
            else
            {
                saturation /= 100f;
            }
        }

        if (value > 1f + float.Epsilon)
        {
            if (value > 100f + float.Epsilon)
            {
                value = 1f;
            }
            else
            {
                value /= 100f;
            }
        }

        Color colour = ColorExtensions.FromHsv(hue, saturation, value);
        var embedBuilder = new EmbedBuilder()
            .AddField("RGB", $"Red: **{colour.R}**\nGreen: **{colour.G}**\nBlue: **{colour.B}**", true)
            .AddField("HSV", $"Hue: **{hue:0.##}°**\nSaturation: **{saturation:n2}**\nValue: **{value:n2}**", true)
            .AddField("Hex Code", $"#**{colour.R:X2}{colour.G:X2}{colour.B:X2}**")
            .WithColor(colour);

        await RespondAsync(embed: embedBuilder.Build());
    }

    [SlashCommand("rgb", "Shows HSV values and the hex code for the given RGB values.")]
    public async Task RgbCommand(
        [Summary("red", "Value from 0 to 255."), MinValue(0), MaxValue(255)] int red,
        [Summary("green", "Value from 0 to 255."), MinValue(0), MaxValue(255)] int green,
        [Summary("blue", "Value from 0 to 255."), MinValue(0), MaxValue(255)] int blue)
    {
        var colour = new Color(red, green, blue);
        colour.GetHsv(out float hue, out float saturation, out float value);
        var embedBuilder = new EmbedBuilder()
            .AddField("RGB", $"Red: **{colour.R}**\nGreen: **{colour.G}**\nBlue: **{colour.B}**", true)
            .AddField("HSV", $"Hue: **{hue:0.##}°**\nSaturation: **{saturation:n2}**\nValue: **{value:n2}**", true)
            .AddField("Hex Code", $"#**{colour.R:X2}{colour.G:X2}{colour.B:X2}**")
            .WithColor(colour);

        await RespondAsync(embed: embedBuilder.Build());
    }
}

[Group("color", "Displays color information.")]
public class ColorModule : ColorModuleBase;

[Group("colour", "Displays colour information.")]
public class ColourModule : ColorModuleBase;
