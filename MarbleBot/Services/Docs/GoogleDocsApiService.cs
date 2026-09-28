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

    public static string FormatParagraph(Paragraph paragraph, int skipCount = 0)
    {
        var stringBuilder = new StringBuilder();
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

        string outputText = stringBuilder.ToString().Trim();

        if (alreadyInlineCode)
        {
            outputText = $"{outputText}{inlineCodeDelimiter}";
        }

        return outputText;
    }

    public void Dispose()
    {
        _docsService?.Dispose();
        GC.SuppressFinalize(this);
    }
}
