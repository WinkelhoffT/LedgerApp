using Microsoft.AspNetCore.Components;
using StudyHub.UI.Services;

namespace StudyHub.UI.Components.Layout;

public partial class MainLayout : IDisposable
{
    [Inject]
    private IPageHeaderService PageHeader { get; set; } = default!;

    [Inject]
    private ISidebarStateService SidebarState { get; set; } = default!;

    protected override void OnInitialized()
    {
        PageHeader.Changed += HandleStateChanged;
        SidebarState.Changed += HandleStateChanged;
    }

    private void HandleStateChanged() => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        PageHeader.Changed -= HandleStateChanged;
        SidebarState.Changed -= HandleStateChanged;
    }
}
