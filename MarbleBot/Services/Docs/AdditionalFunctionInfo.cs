namespace MarbleBot.Services.Docs;

public class AdditionalFunctionInfo
{
    public string Name { get; }
    public string? Credit { get; }
    public string Description { get; }
    public string Usage { get; }
    public string Example { get; }
    public bool ExampleIsMultiLine { get; }
    public IReadOnlyList<ParameterInfo> Parameters { get; }
    public string Code { get; }

    public AdditionalFunctionInfo(string name, string? credit, string description, string usage, string example, bool exampleIsMultiLine, IReadOnlyList<ParameterInfo> parameters, string code)
    {
        Name = name;
        Credit = credit;
        Description = description;
        Usage = usage;
        Example = example;
        ExampleIsMultiLine = exampleIsMultiLine;
        Parameters = parameters;
        Code = code;
    }
}
