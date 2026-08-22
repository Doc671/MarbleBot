namespace MarbleBot.Entities;

public class LegacyGuild
{
    public ulong Id { get; set; }
    public ulong AnnouncementChannel { get; set; }
    public string? AppealFormLink { get; set; }
    public ulong AutoresponseChannel { get; set; }
    public string Color { get; set; } = "607D8B";
    public string Prefix { get; set; } = "mb/";
    public List<ulong> Roles { get; set; } = [];
    public List<ulong> UsableChannels { get; set; } = [];
    public string? WarningSheetLink { get; set; }
}
