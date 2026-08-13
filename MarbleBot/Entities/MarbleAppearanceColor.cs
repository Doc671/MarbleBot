namespace MarbleBot.Entities;

public class MarbleAppearanceColor
{
    public int Id { get; init; }

    public ulong UserId { get; init; }
    public MarbleAppearance Appearance { get; init; } = null!;

    public uint RawColor { get; init; }
    public int Order { get; init; }
}
