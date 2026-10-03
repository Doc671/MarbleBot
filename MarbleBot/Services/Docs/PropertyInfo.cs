namespace MarbleBot.Services.Docs;

public class PropertyInfo : IPropertyListInfo
{
    public string Name { get; }
    public string? Default { get; }
    public string Type { get; }
    public string Description { get; }

    public PropertyInfo(string name, string? @default, string type, string description)
    {
        Name = name;
        Default = @default;
        Type = type;
        Description = description;
    }
}
