using Microsoft.AspNetCore.Components;
using StudyHub.Logic.Business.Documents;
using StudyHub.UI.Documents;

namespace StudyHub.UI.Components.Shared;

public partial class DocumentDetailsDialog
{
    [Inject]
    private IDocumentAccessor DocumentAccessor { get; set; } = default!;

    [Parameter]
    public bool IsOpen { get; set; }

    [Parameter]
    public DocumentDto? Document { get; set; }

    [Parameter]
    public string ParentName { get; set; } = string.Empty;

    [Parameter]
    public EventCallback OnClose { get; set; }

    [Parameter]
    public EventCallback<DocumentDto> OnChanged { get; set; }

    private bool IsProcessing { get; set; }

    private async Task ArchiveAsync()
    {
        if (Document is null)
        {
            return;
        }

        IsProcessing = true;
        var updated = await DocumentAccessor.ArchiveAsync(Document.Id);
        IsProcessing = false;

        await OnChanged.InvokeAsync(updated);
    }

    private async Task RestoreAsync()
    {
        if (Document is null)
        {
            return;
        }

        IsProcessing = true;
        var updated = await DocumentAccessor.RestoreAsync(Document.Id);
        IsProcessing = false;

        await OnChanged.InvokeAsync(updated);
    }

    private Task Close() => OnClose.InvokeAsync();
}
