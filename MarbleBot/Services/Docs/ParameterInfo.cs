namespace MarbleBot.Services.Docs;

public class ParameterInfo
{
    public string Name { get; }
    public string Type { get; }
    public string? Description { get; }

    public ParameterInfo(string name, string type, string? description)
    {
        Name = name;
        Type = type;
        Description = description;
    }
}
