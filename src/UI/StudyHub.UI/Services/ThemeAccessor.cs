using Microsoft.JSInterop;

namespace StudyHub.UI.Services;

public sealed class ThemeAccessor(IJSRuntime jsRuntime) : IThemeAccessor
{
    public Task<string> GetThemeAsync() =>
        jsRuntime.InvokeAsync<string>("studyHubTheme.get").AsTask();

    public Task SetThemeAsync(string theme) =>
        jsRuntime.InvokeVoidAsync("studyHubTheme.set", theme).AsTask();
}
