namespace MarbleBot.Services.Docs;

public class OperationInfo : FunctionInfo
{
    public string Operation { get; }

    public OperationInfo(string operation, string name, string? description, List<ParameterInfo>? parameters, string? code) : base(name, description, parameters, code)
    {
        Operation = operation;
    }
}
