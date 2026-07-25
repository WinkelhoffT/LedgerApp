namespace StudyHub.UI.Services;

public interface IPageHeaderService
{
    string? Title { get; }

    string? Subtitle { get; }

    event Action? Changed;

    void SetHeader(string? title, string? subtitle = null);
}
