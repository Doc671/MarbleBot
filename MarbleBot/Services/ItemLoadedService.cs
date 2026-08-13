namespace MarbleBot.Services;

public class ItemLoadedService
{
    private readonly TaskCompletionSource _loadedTaskCompletionSource = new();

    public Task Loaded => _loadedTaskCompletionSource.Task;

    public void SetLoaded()
    {
        _loadedTaskCompletionSource.SetResult();
    }
}
