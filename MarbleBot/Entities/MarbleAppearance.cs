namespace MarbleBot.Entities;

public class MarbleAppearance
{
    public ulong UserId { get; init; }
    public User User { get; init; } = null!;

    public MarbleAppearanceType AppearanceType { get; set; }

    public List<MarbleAppearanceColor> Colors { get; set; } = new();

    public float? GradientAngleDegrees { get; set; }
}
