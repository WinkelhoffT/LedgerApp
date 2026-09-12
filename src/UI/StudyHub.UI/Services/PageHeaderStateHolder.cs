namespace StudyHub.UI.Services;

public sealed class PageHeaderStateHolder : IPageHeaderStateHolder
{
    public string? Title { get; private set; } = "StudyHub";

    public string? Subtitle { get; private set; }

    public event Action? Changed;

    public void SetHeader(string? title, string? subtitle = null)
    {
        Title = title;
        Subtitle = subtitle;
        Changed?.Invoke();
    }
}
