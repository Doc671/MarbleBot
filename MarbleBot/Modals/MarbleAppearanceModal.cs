using MarbleBot.Entities;

namespace MarbleBot.Modals;

public class MarbleAppearanceModal : IModal
{
    public string Title => "Marble Appearance";

    [InputLabel("Appearance Type", "The type of appearance for your marble.")]
    [ModalSelectMenu("appearance_type")]
    public MarbleAppearanceType MarbleAppearanceType { get; set; }

    [InputLabel("Hex Codes", "Up to 8 hex code(s) for your marble, separated by new lines. Only 1 required for solid appearance.")]
    [ModalTextInput("hex_codes", TextInputStyle.Paragraph, "#000000", 6, 79, "#000000")]
    public string? HexCodes { get; set; }

    [InputLabel("Angle offset (degrees)", "The angle in degrees. This only affects sector and linear gradient marbles.")]
    [ModalTextInput("angle_offset", TextInputStyle.Short, "0", 1, 3, "0")]
    [RequiredInput(false)]
    public string? AngleOffset { get; set; }
}
