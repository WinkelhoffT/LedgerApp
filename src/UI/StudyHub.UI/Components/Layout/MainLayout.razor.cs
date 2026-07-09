using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace StudyHub.UI.Components.Layout;

public partial class MainLayout
{
    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    private bool _isDarkMode;
    private readonly MudTheme _theme = new();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _isDarkMode = await JS.InvokeAsync<bool>("studyHubTheme.getIsDarkMode");
            StateHasChanged();
        }
    }

    private async Task ToggleDarkModeAsync()
    {
        _isDarkMode = !_isDarkMode;
        await JS.InvokeVoidAsync("studyHubTheme.setIsDarkMode", _isDarkMode);
    }
}
