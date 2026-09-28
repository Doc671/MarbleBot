using Google.Apis.Docs.v1.Data;
using System.Text;

namespace MarbleBot.Services.Docs;

public class ThymeAdditionalFunctionsService
{
    private readonly GoogleDocsApiService _googleDocsApiService;

    private readonly string _documentId;

    private Dictionary<string, Dictionary<string, AdditionalFunctionInfo>>? _functionCategories;

    private DateTime _lastSyncTime = new(0);
    private readonly TimeSpan _syncInterval = TimeSpan.FromDays(1);

    private readonly SemaphoreSlim _syncGate = new(1, 1);

    public ThymeAdditionalFunctionsService(GoogleDocsApiService googleDocsApiService, ExternalResources externalResources)
    {
        _googleDocsApiService = googleDocsApiService;
        _documentId = externalResources.ThymeAdditionalFunctionsId;
    }

    private async Task SyncWithDocumentAsync()
    {
        if (!_googleDocsApiService.IsAvailable)
        {
            return;
        }

        await _syncGate.WaitAsync();
        try
        {
            if (_functionCategories != null && DateTime.UtcNow - _lastSyncTime < _syncInterval)
            {
                return;
            }

            Document? response = await _googleDocsApiService.GetDocumentAsync(_documentId);
            if (response == null)
            {
                return;
            }

            _functionCategories ??= ParseDocument(response);
            _lastSyncTime = DateTime.UtcNow;
        }
        finally
        {
            _syncGate.Release();
        }
    }

    private static Dictionary<string, Dictionary<string, AdditionalFunctionInfo>> ParseDocument(Document response)
    {
        var outputDictionary = new Dictionary<string, Dictionary<string, AdditionalFunctionInfo>>();

        const string functionTabEnding = " functions";
        IEnumerable<Tab> tabs = response.Tabs.Where(tab => tab.TabProperties.Title.EndsWith(functionTabEnding));
        foreach (Tab? tab in tabs)
        {
            if (tab != null)
            {
                var currentTabFunctions = new List<AdditionalFunctionInfo>();

                string name = "";
                string? credit = null;
                string description = "";
                string usage = "";
                string example = "";
                string code = "";
                bool exampleIsCodeBlock = false;
                var currentFunctionParameters = new List<ParameterInfo>();
                foreach (StructuralElement? element in tab.DocumentTab.Body.Content)
                {
                    if (element.Paragraph != null)
                    {
                        var paragraph = element.Paragraph;

                        // Assume all function titles (and nothing else) have the heading style Heading 2 within the function tabs
                        TextRun textRun = paragraph.Elements[0].TextRun;
                        if (paragraph.ParagraphStyle.NamedStyleType == "HEADING_2" && !string.IsNullOrWhiteSpace(textRun.Content))
                        {
                            if (!string.IsNullOrWhiteSpace(name))
                            {
                                var function = new AdditionalFunctionInfo(name, credit, description, usage, example, exampleIsCodeBlock, currentFunctionParameters, code);
                                currentTabFunctions.Add(function);
                            }

                            name = textRun.Content.Trim();
                            description = usage = example = code = "";
                            credit = null;
                            exampleIsCodeBlock = false;
                            currentFunctionParameters = [];
                        }
                        else if (textRun != null)
                        {
                            string textContent = textRun.Content;
                            if (textRun.TextStyle.Italic == true)  // Assume descriptions are in italics
                            {
                                description = GoogleDocsApiService.FormatParagraph(paragraph);
                            }
                            else if (textContent.StartsWith("Usage"))
                            {
                                usage = GoogleDocsApiService.FormatParagraph(paragraph, 1);
                            }
                            else if (textContent.StartsWith("Example"))
                            {
                                // If the line only contains "Example:" then the example is a code block instead of just being on 1 line
                                if (paragraph.Elements.Count(element => element.TextRun != null) == 1)
                                {
                                    exampleIsCodeBlock = true;
                                }
                                else
                                {
                                    example = GoogleDocsApiService.FormatParagraph(paragraph, 1);
                                }
                            }
                            else if (textContent.Contains("by"))
                            {
                                credit = GoogleDocsApiService.FormatParagraph(paragraph);
                            }
                        }
                    }
                    else if (element.Table != null)
                    {
                        if (element.Table.Rows == 1)    // Assume this is a code block (table with 1 row and 1 column)
                        {
                            string codeBlockContent = string.Concat(
                                element.Table.TableRows[0].TableCells[0].Content
                                .Where(element => element.Paragraph != null)
                                .SelectMany(element => element.Paragraph.Elements
                                    .Where(element => element?.TextRun != null)
                                    .Select(element => element.TextRun.Content)))
                                .Trim()
                                .Replace('\v', '\n');

                            codeBlockContent = $"```fs\n{codeBlockContent}\n```";

                            if (exampleIsCodeBlock && string.IsNullOrEmpty(example))
                            {
                                example = codeBlockContent;
                            }
                            else
                            {
                                code = codeBlockContent;
                            }
                        }
                        else
                        {
                            List<ParameterInfo> parameters = GetParametersFromTable(element);
                            if (parameters.Count != 0)
                            {
                                currentFunctionParameters = parameters;
                            }
                        }
                    }
                }

                var lastFunction = new AdditionalFunctionInfo(name, credit, description, usage, example, exampleIsCodeBlock, currentFunctionParameters, code);
                currentTabFunctions.Add(lastFunction);

                string categoryName = tab.TabProperties.Title[..^functionTabEnding.Length];
                var category = currentTabFunctions.ToDictionary(function => function.Name, function => function);
                outputDictionary[categoryName] = category;
            }
        }

        return outputDictionary;
    }

    private static List<ParameterInfo> GetParametersFromTable(StructuralElement element)
    {
        const int parameterInfoCellCount = 3;
        return element.Table.TableRows
            .Skip(1)    // Skip the table header row
            .Where(row => row.TableCells.Count >= parameterInfoCellCount)
            .Select(row =>
            {
                string?[] parameterDetails = row.TableCells
                    .Select(cell => cell.Content[0].Paragraph)
                    .Where(paragraph => paragraph != null)
                    .Select(paragraph => GoogleDocsApiService.FormatParagraph(paragraph))
                    .ToArray();

                if (parameterDetails.Length < parameterInfoCellCount)
                {
                    return null;
                }

                if (string.IsNullOrEmpty(parameterDetails[1]))
                {
                    parameterDetails[1] = "\""; // Add a ditto, assuming the type is the same as the previous parameter
                }

                if (string.IsNullOrEmpty(parameterDetails[2]))
                {
                    parameterDetails[2] = "\"";
                }

                return new ParameterInfo(parameterDetails[0]!, parameterDetails[1]!, parameterDetails[2]!);
            })
           .Where(parameterInfo => parameterInfo != null)
           .ToList()!;
    }

    public async Task<string?> ListHeadersAsync(string category)
    {
        await SyncWithDocumentAsync();
        if (_functionCategories == null)
        {
            return null;
        }

        var stringBuilder = new StringBuilder();
        foreach ((string functionName, AdditionalFunctionInfo function) in _functionCategories[category])
        {
            stringBuilder.AppendLine($"- `{functionName}`: {function.Description}");
        }

        const int characterLimit = 2000;
        if (stringBuilder.Length > characterLimit)
        {
            stringBuilder.Length = characterLimit;
        }

        return stringBuilder.ToString();
    }

    public async Task<AdditionalFunctionInfo?> GetFunctionInfoAsync(string name)
    {
        await SyncWithDocumentAsync();
        if (_functionCategories == null)
        {
            return null;
        }

        foreach ((_, Dictionary<string, AdditionalFunctionInfo> category) in _functionCategories)
        {
            if (category.TryGetValue(name, out AdditionalFunctionInfo? function))
            {
                return function;
            }
        }

        return null;
    }

    public IReadOnlyList<string>? GetTabNames()
    {
        if (_functionCategories == null)
        {
            return null;
        }

        return _functionCategories.Keys.ToList();
    }
}
