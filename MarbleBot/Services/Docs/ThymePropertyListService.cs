using Google.Apis.Docs.v1.Data;

namespace MarbleBot.Services.Docs;

public class ThymePropertyListService
{
    private readonly GoogleDocsApiService _googleDocsApiService;

    private readonly string _documentId;

    private Dictionary<string, IReadOnlySet<string>>? _propertyCategories;
    private Dictionary<string, IPropertyListInfo>? _properties;

    private DateTime _lastSyncTime = new(0);
    private readonly TimeSpan _syncInterval = TimeSpan.FromDays(1);

    private readonly SemaphoreSlim _syncGate = new(1, 1);

    public ThymePropertyListService(GoogleDocsApiService googleDocsApiService, ExternalResources externalResources)
    {
        _googleDocsApiService = googleDocsApiService;
        _documentId = externalResources.ThymePropertyListId;
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
            if (_propertyCategories != null && DateTime.UtcNow - _lastSyncTime < _syncInterval)
            {
                return;
            }

            Document? response = await _googleDocsApiService.GetDocumentAsync(_documentId);
            if (response == null)
            {
                return;
            }

            ParseDocument(response);
            _lastSyncTime = DateTime.UtcNow;
        }
        finally
        {
            _syncGate.Release();
        }
    }

    private void ParseDocument(Document response)
    {
        _propertyCategories = [];
        _properties = [];

        var tabs = response.Tabs
            .Skip(1)    // Skip the preface tab
            .Where(tab => tab != null)
            .SelectMany(tab => EnumerateTabs(tab));

        foreach (Tab? tab in tabs)
        {
            string previousHeading = "";
            foreach (StructuralElement element in tab.DocumentTab.Body.Content)
            {
                Paragraph? paragraph = element.Paragraph;
                if (paragraph?.ParagraphStyle.NamedStyleType.StartsWith("HEADING") == true
                    && paragraph.Elements[0].TextRun?.Content != null)
                {
                    previousHeading = paragraph.Elements[0].TextRun.Content.TrimEnd();
                }

                if (element.Table != null && element.Table.Rows > 1)
                {
                    IReadOnlySet<string>? columnNames = AddTableContents(element.Table);
                    if (columnNames != null)
                    {
                        _propertyCategories[previousHeading] = columnNames;
                    }
                }
            }
        }
    }

    private HashSet<string>? AddTableContents(Table table)
    {
        if (table.Columns < 2)
        {
            return null;
        }

        string[][] tableContents = GoogleDocsApiService.FormatTable(table, "Name", "Property", "Function", "Script", "Widget");

        string secondHeader = tableContents[0][1];

        // Guess the structure of the table from how many columns there are and what the second header is
        // Note that future syncs will fail if the structure of tables in the Thyme Property List doc are changed
        return table.Columns switch
        {
            2 when secondHeader == "Description" => AddFunctionDescription(tableContents),                  // Function, Description
            2 => AddFunctionCode(tableContents),                                                            // Name, Script
            3 => AddAttributeInputDescription(tableContents),                                               // Attribute, Input, Description
            4 when secondHeader == "Default" => AddPropertyDefaultTypeDescription(tableContents),           // Property, Default, Type, Description
            4 when secondHeader == "Params" => AddFunctionParameterParameterTypeDescription(tableContents), // Function, Parameter, Parameter Type, Description
            4 => AddFunctionParameterParameterTypeParameterDescription(tableContents),                      // Function, Parameter, Parameter Type, Parameter Description
            5 => AddOperationFunctionParameterParameterTypeParameterDescription(tableContents),             // Operation, Function, Property, Property Type, Property Description
            _ => null,
        };
    }

    private HashSet<string> AddFunctionDescription(string[][] tableContents)
    {
        var names = new HashSet<string>();
        for (int i = 1; i < tableContents.Length; i++)
        {
            string[] rowContent = tableContents[i];
            string name = rowContent[0];
            string description = rowContent[1];

            if (_properties!.TryGetValue(name, out IPropertyListInfo? rawInfo) && rawInfo is FunctionInfo functionInfo)
            {
                functionInfo.Description = description;
            }
            else
            {
                _properties[name] = new FunctionInfo(name, description, null, null);
            }

            names.Add(name);
        }

        return names;
    }

    private HashSet<string> AddFunctionCode(string[][] tableContents)
    {
        var names = new HashSet<string>();
        string previousCode = "";
        for (int i = 1; i < tableContents.Length; i++)
        {
            string[] rowContent = tableContents[i];
            string name = rowContent[0];
            string code = rowContent[1];

            if (string.IsNullOrEmpty(code))
            {
                code = previousCode;
            }
            else
            {
                previousCode = code;
            }

            if (_properties!.TryGetValue(name, out IPropertyListInfo? rawInfo) && rawInfo is FunctionInfo functionInfo)
            {
                functionInfo.Code = $"```fs\n{code}\n```";
            }
            else
            {
                _properties[name] = new FunctionInfo(name, null, null, code);
            }

            names.Add(name);
        }

        return names;
    }

    private HashSet<string> AddAttributeInputDescription(string[][] tableContents)
    {
        var names = new HashSet<string>();
        for (int i = 1; i < tableContents.Length; i++)
        {
            string[] rowContent = tableContents[i];
            string name = rowContent[0];
            string input = rowContent[1];
            string description = rowContent[2];

            if (name.Contains('\n'))
            {
                string[] subnames = name.Split('\n')
                    .Select(subname => subname.Trim())
                    .ToArray();

                foreach (string subname in subnames)
                {
                    var attributeInfo = new TextAttributeInfo(subname, input, description);
                    _properties![subname] = attributeInfo;
                }
            }
            else
            {
                var attributeInfo = new TextAttributeInfo(name, input, description);
                _properties![name] = attributeInfo;
            }
        }

        return names;
    }

    private HashSet<string> AddPropertyDefaultTypeDescription(string[][] tableContents)
    {
        var names = new HashSet<string>();
        for (int i = 1; i < tableContents.Length; i++)
        {
            string[] rowContent = tableContents[i];
            string name = rowContent[0];
            string defaultValue = rowContent[1];
            string type = rowContent[2];
            string description = rowContent[3];

            var propertyInfo = new PropertyInfo(name, defaultValue, type, description);
            _properties![name] = propertyInfo;
            names.Add(name);
        }

        return names;
    }

    private HashSet<string> AddFunctionParameterParameterTypeDescription(string[][] tableContents)
    {
        var names = new HashSet<string>();
        string previousName = "";
        string previousDescription = "";
        for (int i = 1; i < tableContents.Length; i++)
        {
            string[] rowContent = tableContents[i];
            string name = rowContent[0];
            string parameterName = rowContent[1];
            string parameterType = rowContent[2];
            string description = rowContent[3];

            if (string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(previousName) && _properties![previousName] is FunctionInfo previousFunctionInfo)
            {
                var parameterInfo = new ParameterInfo(parameterName, parameterType, null);
                if (previousFunctionInfo.Parameters == null)
                {
                    previousFunctionInfo.Parameters = [parameterInfo];
                }
                else
                {
                    previousFunctionInfo.Parameters.Add(parameterInfo);
                }
            }
            else
            {
                if (string.IsNullOrEmpty(description))
                {
                    description = previousDescription;
                }
                else
                {
                    previousDescription = description;
                }

                FunctionInfo functionInfo;
                if (string.IsNullOrEmpty(parameterName))
                {
                    functionInfo = new FunctionInfo(name, description, null, null);
                }
                else
                {
                    var parameterInfo = new ParameterInfo(parameterName, parameterType, null);
                    functionInfo = new FunctionInfo(name, description, [parameterInfo], null);
                }

                _properties![name] = functionInfo;
                names.Add(name);
                previousName = name;
            }
        }

        return names;
    }

    private HashSet<string> AddFunctionParameterParameterTypeParameterDescription(string[][] tableContents)
    {
        var names = new HashSet<string>();
        string previousName = "";
        for (int i = 1; i < tableContents.Length; i++)
        {
            string[] rowContent = tableContents[i];
            string name = rowContent[0];
            string parameterName = rowContent[1];
            string parameterType = rowContent[2];
            string parameterDescription = rowContent[3];

            if (string.IsNullOrEmpty(name))
            {
                if (previousName.Contains("and"))
                {
                    string[] subnames = previousName.Split("and")
                        .Select(subname => subname.Trim())
                        .ToArray();

                    foreach (string subname in subnames)
                    {
                        if (_properties![subname] is FunctionInfo previousFunctionInfo)
                        {
                            var parameterInfo = new ParameterInfo(parameterName, parameterType, parameterDescription);
                            if (previousFunctionInfo.Parameters == null)
                            {
                                previousFunctionInfo.Parameters = [parameterInfo];
                            }
                            else
                            {
                                previousFunctionInfo.Parameters.Add(parameterInfo);
                            }
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(previousName) && _properties![previousName] is FunctionInfo previousFunctionInfo)
                {
                    var parameterInfo = new ParameterInfo(parameterName, parameterType, parameterDescription);
                    if (previousFunctionInfo.Parameters == null)
                    {
                        previousFunctionInfo.Parameters = [parameterInfo];
                    }
                    else
                    {
                        previousFunctionInfo.Parameters.Add(parameterInfo);
                    }
                }
            }
            else
            {
                if (name.Contains("and"))
                {
                    string[] subnames = name.Split("and")
                        .Select(subname => subname.Trim())
                        .ToArray();

                    foreach (string subname in subnames)
                    {
                        FunctionInfo functionInfo;
                        if (string.IsNullOrEmpty(parameterName))
                        {
                            functionInfo = new FunctionInfo(subname, null, null, null);
                        }
                        else
                        {
                            var parameterInfo = new ParameterInfo(parameterName, parameterType, parameterDescription);
                            functionInfo = new FunctionInfo(subname, parameterDescription, [parameterInfo], null);
                        }
                        _properties![subname] = functionInfo;
                        names.Add(subname);
                    }

                    previousName = name;
                }
                else
                {
                    FunctionInfo functionInfo;
                    if (string.IsNullOrEmpty(parameterName))
                    {
                        functionInfo = new FunctionInfo(name, null, null, null);
                    }
                    else
                    {
                        var parameterInfo = new ParameterInfo(parameterName, parameterType, parameterDescription);
                        functionInfo = new FunctionInfo(name, parameterDescription, [parameterInfo], null);
                    }

                    _properties![name] = functionInfo;
                    names.Add(name);
                    previousName = name;
                }
            }
        }

        return names;
    }

    private HashSet<string> AddOperationFunctionParameterParameterTypeParameterDescription(string[][] tableContents)
    {
        var names = new HashSet<string>();
        string previousName = "";
        for (int i = 1; i < tableContents.Length; i++)
        {
            string[] rowContent = tableContents[i];
            string operation = rowContent[0];
            string name = rowContent[1];
            string parameterName = rowContent[2];
            string parameterType = rowContent[3];
            string parameterDescription = rowContent[4];

            if (string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(previousName) && _properties![previousName] is OperationInfo previousOperationInfo)
            {
                var parameterInfo = new ParameterInfo(parameterName, parameterType, parameterDescription);
                if (previousOperationInfo.Parameters == null)
                {
                    previousOperationInfo.Parameters = [parameterInfo];
                }
                else
                {
                    previousOperationInfo.Parameters.Add(parameterInfo);
                }
            }
            else
            {
                OperationInfo operationInfo;
                if (string.IsNullOrEmpty(parameterName))
                {
                    operationInfo = new OperationInfo(operation, name, null, null, null);
                }
                else
                {
                    var parameterInfo = new ParameterInfo(parameterName, parameterType, parameterDescription);
                    operationInfo = new OperationInfo(operation, name, parameterDescription, [parameterInfo], null);
                }

                _properties![name] = operationInfo;
                names.Add(name);
                previousName = name;
            }
        }

        return names;
    }

    private static IEnumerable<Tab> EnumerateTabs(Tab tab)
    {
        yield return tab;

        if (tab.ChildTabs == null)
        {
            yield break;
        }

        foreach (Tab childTab in tab.ChildTabs)
        {
            foreach (Tab descendantTab in EnumerateTabs(childTab))
            {
                yield return descendantTab;
            }
        }
    }

    public async Task<IPropertyListInfo?> GetPropertyInfoAsync(string name)
    {
        await SyncWithDocumentAsync();
        if (_properties == null)
        {
            return null;
        }

        if (_properties.TryGetValue(name, out IPropertyListInfo? propertyInfo))
        {
            return propertyInfo;
        }

        return null;
    }

    public async Task<IReadOnlySet<string>?> GetAllPropertiesInCategoryAsync(string categoryName)
    {
        await SyncWithDocumentAsync();
        if (_propertyCategories == null)
        {
            return null;
        }

        if (_propertyCategories.TryGetValue(categoryName, out IReadOnlySet<string>? categoryProperties))
        {
            return categoryProperties;
        }

        return null;
    }

    public async Task<IReadOnlyList<string>?> GetCategoryNamesAsync()
    {
        await SyncWithDocumentAsync();
        if (_propertyCategories == null)
        {
            return null;
        }

        return _propertyCategories.Keys.ToList();
    }

    public async Task<IReadOnlyList<string>?> GetCategoryNamesAsync(int page)
    {
        await SyncWithDocumentAsync();
        if (_propertyCategories == null)
        {
            return null;
        }

        const int pageSize = 20;
        return _propertyCategories.Keys
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }

    public async Task<int?> GetCategoryCountAsync()
    {
        await SyncWithDocumentAsync();
        if (_propertyCategories == null)
        {
            return null;
        }

        return _propertyCategories.Count;
    }
}
