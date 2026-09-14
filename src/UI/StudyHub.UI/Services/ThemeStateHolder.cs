namespace StudyHub.UI.Services;

public sealed class ThemeStateHolder(IThemeAccessor themeAccessor) : IThemeStateHolder
{
    private const string DefaultTheme = "dark";

    public string Theme { get; private set; } = DefaultTheme;

    public event Action? Changed;

    public async Task InitializeAsync()
    {
        var current = await themeAccessor.GetThemeAsync();
        SetTheme(current);
    }

    public async Task ToggleAsync()
    {
        var next = Theme == "dark" ? "light" : "dark";
        await themeAccessor.SetThemeAsync(next);
        SetTheme(next);
    }

    private void SetTheme(string theme)
    {
        if (theme is not ("light" or "dark") || theme == Theme)
        {
            return;
        }

        Theme = theme;
        Changed?.Invoke();
    }
}
