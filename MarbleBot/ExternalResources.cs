namespace MarbleBot;

public class ExternalResources
{
    public string AlgodooScriptingGuideId { get; }
    public string ThymeAdditionalFunctionsId { get; }
    public string ThymePropertyListId { get; }

    public ExternalResources(string algodooScriptingGuideId, string thymeAdditionalFunctionsId, string thymePropertyListId)
    {
        AlgodooScriptingGuideId = algodooScriptingGuideId;
        ThymeAdditionalFunctionsId = thymeAdditionalFunctionsId;
        ThymePropertyListId = thymePropertyListId;
    }
}
