namespace StudyHub.UI.Services;

public interface IThemeService
{
    string Theme { get; }

    event Action? Changed;

    Task InitializeAsync();

    Task ToggleAsync();
}
