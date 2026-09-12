using Microsoft.AspNetCore.Components;
using StudyHub.UI.Services;

namespace StudyHub.UI.Components.Layout;

public partial class MainLayout : IDisposable
{
    [Inject]
    private IPageHeaderStateHolder PageHeader { get; set; } = default!;

    [Inject]
    private ISidebarStateHolder SidebarState { get; set; } = default!;

    [Inject]
    private IThemeStateHolder Theme { get; set; } = default!;

    protected override void OnInitialized()
    {
        PageHeader.Changed += HandleStateChanged;
        SidebarState.Changed += HandleStateChanged;
        Theme.Changed += HandleStateChanged;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await Theme.InitializeAsync();
        }
    }

    private void HandleStateChanged() => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        PageHeader.Changed -= HandleStateChanged;
        SidebarState.Changed -= HandleStateChanged;
        Theme.Changed -= HandleStateChanged;
    }
}
