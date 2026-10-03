using Google.Apis.Auth.OAuth2;
using Google.Apis.Docs.v1;
using Google.Apis.Docs.v1.Data;
using Google.Apis.Services;
using System.Reflection;
using System.Text;

namespace MarbleBot.Services.Docs;

public class GoogleDocsApiService : IDisposable
{
    public bool IsAvailable => _docsService != null;

    private readonly DocsService? _docsService;

    public GoogleDocsApiService(BotCredentials botCredentials)
    {
        if (string.IsNullOrEmpty(botCredentials.GoogleServiceAccountKeyPath))
        {
            return;
        }

        string keyPath = Path.Combine(AppContext.BaseDirectory, botCredentials.GoogleServiceAccountKeyPath);
        var credential = CredentialFactory.FromFile(keyPath, JsonCredentialParameters.ServiceAccountCredentialType)
            .CreateScoped(DocsService.Scope.DocumentsReadonly);
        
        _docsService = new DocsService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = Assembly.GetExecutingAssembly().GetName().Name
        });
    }

    public async Task<Document?> GetDocumentAsync(string documentId)
    {
        if (_docsService == null)
        {
            return null;
        }

        var request = _docsService.Documents.Get(documentId);
        request.IncludeTabsContent = true;
        return await request.ExecuteAsync();
    }

    public static string[][] FormatTable(Table table, params string[] ignoreColumns)
    {
        string[] headers = table.TableRows[0].TableCells
            .Select(cell => string.Concat(cell.Content
                .Where(element => element.Paragraph != null)
                .SelectMany(element => element.Paragraph.Elements
                    .Where(element => element.TextRun != null)
                    .Select(element => element.TextRun.Content.TrimEnd()))
                ))
            .ToArray();

        string[][] tableStrings = new string[table.Rows!.Value][];
        tableStrings[0] = headers;
        for (int rowIndex = 1; rowIndex < table.Rows; rowIndex++)
        {
            TableRow? row = table.TableRows[rowIndex];
            if (row != null)
            {
                int rowLength = row.TableCells.Count;
                tableStrings[rowIndex] = new string[rowLength];
                for (int columnIndex = 0; columnIndex < rowLength; columnIndex++)
                {
                    TableCell cell = row.TableCells[columnIndex];
                    tableStrings[rowIndex][columnIndex] = FormatCell(cell, headers[columnIndex], ignoreColumns);
                }
            }
        }

        return tableStrings;
    }

    public static string FormatCell(TableCell cell, string columnName, params string[] ignoreColumns)
    {
        if (columnName is "Script")
        {
            return string.Concat(cell.Content
                .Where(element => element.Paragraph != null)
                .SelectMany(element => element.Paragraph.Elements
                    .Where(element => element.TextRun != null)
                    .Select(element => element.TextRun.Content)));
        }

        var stringBuilder = new StringBuilder();
        bool ignore = ignoreColumns.Contains(columnName);
        for (int i = 0; i < cell.Content.Count; i++)
        {
            StructuralElement? element = cell.Content[i];
            if (element?.Paragraph != null)
            {
                if (ignore)
                {
                    stringBuilder.Append(string.Concat(element.Paragraph.Elements
                        .Where(element => element.TextRun != null)
                        .Select(element => element.TextRun.Content.TrimEnd())));
                }
                else
                {
                    FormatParagraph(stringBuilder, element.Paragraph);
                }

                stringBuilder.Append(' ');
            }
        }

        stringBuilder.TrimEnd(' ', '\n');
        return stringBuilder.ToString();
    }

    public static string FormatParagraph(Paragraph paragraph, int skipCount = 0)
    {
        var stringBuilder = new StringBuilder();
        FormatParagraph(stringBuilder, paragraph, skipCount);
        return stringBuilder.ToString();
    }

    private static void FormatParagraph(StringBuilder stringBuilder, Paragraph paragraph, int skipCount = 0)
    {
        bool alreadyBold = false;
        bool alreadyInlineCode = false;

        const string boldDelimiter = "**";
        const string inlineCodeDelimiter = "`";

        foreach (var element in paragraph.Elements.Skip(skipCount))
        {
            if (element.TextRun?.Content != null)
            {
                TextRun textRun = element.TextRun;
                string text = textRun.Content;

                bool isBold = textRun.TextStyle.Bold == true;
                if (isBold)
                {
                    if (!alreadyBold)
                    {
                        text = $"{boldDelimiter}{text}";
                    }
                    alreadyBold = true;
                }

                if (textRun.TextStyle.BackgroundColor?.Color == null)
                {
                    if (alreadyInlineCode)
                    {
                        text = $"{inlineCodeDelimiter}{text}";
                    }
                    alreadyInlineCode = false;
                }
                else
                {
                    if (!alreadyInlineCode)
                    {
                        text = $"{inlineCodeDelimiter}{text}";
                    }
                    alreadyInlineCode = true;
                }

                if (!isBold)
                {
                    if (alreadyBold)
                    {
                        text = $"{boldDelimiter}{text}";
                    }
                    alreadyBold = false;
                }

                stringBuilder.Append(text);
            }
        }

        stringBuilder.TrimEnd(' ', '\n');

        if (alreadyInlineCode)
        {
            stringBuilder.Append(inlineCodeDelimiter[0]);
        }
    }

    public void Dispose()
    {
        _docsService?.Dispose();
        GC.SuppressFinalize(this);
    }
}
