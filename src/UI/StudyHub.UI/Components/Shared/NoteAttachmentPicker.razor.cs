using Microsoft.AspNetCore.Components;
using StudyHub.Logic.Integration.Documents;
using StudyHub.Logic.Integration.Notes;
using StudyHub.Shared.Documents;
using StudyHub.Shared.Notes;

namespace StudyHub.UI.Components.Shared;

public partial class NoteAttachmentPicker
{
    [Inject]
    private IDocumentAccessor DocumentAccessor { get; set; } = default!;

    [Inject]
    private INoteAccessor NoteAccessor { get; set; } = default!;

    [Parameter]
    public Guid NoteId { get; set; }

    [Parameter]
    public IReadOnlyList<Guid> AttachedDocumentIds { get; set; } = [];

    [Parameter]
    public EventCallback<NoteDto> OnChanged { get; set; }

    private IReadOnlyList<DocumentDto> AllDocuments { get; set; } = [];

    private bool IsUploadDialogOpen { get; set; }

    private Guid SelectedDocumentId { get; set; }

    private IReadOnlyList<DocumentDto> AttachedDocuments =>
        AllDocuments.Where(d => AttachedDocumentIds.Contains(d.Id)).ToList();

    private IReadOnlyList<DocumentDto> AvailableDocuments =>
        AllDocuments.Where(d => !d.IsArchived && !AttachedDocumentIds.Contains(d.Id)).ToList();

    protected override async Task OnInitializedAsync() => AllDocuments = await DocumentAccessor.GetAllAsync();

    private async Task AttachAsync()
    {
        if (SelectedDocumentId == Guid.Empty)
        {
            return;
        }

        var updated = await NoteAccessor.AttachDocumentAsync(NoteId, SelectedDocumentId);
        SelectedDocumentId = Guid.Empty;
        await OnChanged.InvokeAsync(updated);
    }

    private async Task DetachAsync(Guid documentId)
    {
        var updated = await NoteAccessor.DetachDocumentAsync(NoteId, documentId);
        await OnChanged.InvokeAsync(updated);
    }

    private void OpenUploadDialog() => IsUploadDialogOpen = true;

    private void CloseUploadDialog() => IsUploadDialogOpen = false;

    private async Task HandleUploadedAsync(DocumentDto uploaded)
    {
        AllDocuments = await DocumentAccessor.GetAllAsync();
        var updated = await NoteAccessor.AttachDocumentAsync(NoteId, uploaded.Id);
        await OnChanged.InvokeAsync(updated);
    }
}
