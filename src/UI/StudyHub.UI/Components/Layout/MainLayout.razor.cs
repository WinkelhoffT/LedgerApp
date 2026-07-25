using Microsoft.AspNetCore.Components;
using StudyHub.UI.Services;

namespace StudyHub.UI.Components.Layout;

public partial class MainLayout : IDisposable
{
    [Inject]
    private IPageHeaderService PageHeader { get; set; } = default!;

    protected override void OnInitialized()
    {
        PageHeader.Changed += HandleHeaderChanged;
    }

    private void HandleHeaderChanged() => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        PageHeader.Changed -= HandleHeaderChanged;
    }
}
