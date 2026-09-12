namespace StudyHub.UI.Services;

public interface IThemeStateHolder
{
    string Theme { get; }

    event Action? Changed;

    Task InitializeAsync();

    Task ToggleAsync();
}
