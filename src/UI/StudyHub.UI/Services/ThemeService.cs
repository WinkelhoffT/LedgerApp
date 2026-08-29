using Microsoft.JSInterop;

namespace StudyHub.UI.Services;

public sealed class ThemeService(IJSRuntime jsRuntime) : IThemeService
{
    private const string DefaultTheme = "dark";

    public string Theme { get; private set; } = DefaultTheme;

    public event Action? Changed;

    public async Task InitializeAsync()
    {
        var current = await jsRuntime.InvokeAsync<string>("studyHubTheme.get");
        SetTheme(current);
    }

    public async Task ToggleAsync()
    {
        var next = Theme == "dark" ? "light" : "dark";
        await jsRuntime.InvokeVoidAsync("studyHubTheme.set", next);
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
