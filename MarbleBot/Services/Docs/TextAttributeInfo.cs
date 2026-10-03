namespace MarbleBot.Services.Docs;

public class TextAttributeInfo : IPropertyListInfo
{
    public string Name { get; }
    public string? Type { get; }
    public string Description { get; }

    public TextAttributeInfo(string name, string? type, string description)
    {
        Name = name;
        Type = type;
        Description = description;
    }
}
