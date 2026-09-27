using Google.Apis.Auth.OAuth2;
using Google.Apis.Docs.v1;
using Google.Apis.Docs.v1.Data;
using Google.Apis.Services;
using System.Reflection;

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

    public void Dispose()
    {
        _docsService?.Dispose();
        GC.SuppressFinalize(this);
    }
}
