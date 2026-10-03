namespace MarbleBot.Services.Docs;

public class FunctionInfo : IPropertyListInfo
{
    public string Name { get; }
    public string? Description { get; set; }
    public List<ParameterInfo>? Parameters { get; set; }
    public string? Code { get; set; }

    public FunctionInfo(string name, string? description, List<ParameterInfo>? parameters, string? code)
    {
        Name = name;
        Description = description;
        Parameters = parameters;
        Code = code;
    }
}
