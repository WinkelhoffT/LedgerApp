using Microsoft.AspNetCore.Components;
using StudyHub.Logic.Integration.Contract.Documents;
using StudyHub.Logic.Integration.Contract.Notes;
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

    private bool IsSaving { get; set; }

    private string? ErrorMessage { get; set; }

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

        IsSaving = true;
        ErrorMessage = null;

        try
        {
            var updated = await NoteAccessor.AttachDocumentAsync(NoteId, SelectedDocumentId);
            SelectedDocumentId = Guid.Empty;
            await OnChanged.InvokeAsync(updated);
        }
        catch (NoteNotFoundException)
        {
            ErrorMessage = "This note no longer exists. It may have been deleted in another tab.";
        }
        catch (DocumentNotFoundException)
        {
            ErrorMessage = "This document no longer exists.";
            AllDocuments = await DocumentAccessor.GetAllAsync();
        }
        finally
        {
            IsSaving = false;
        }
    }

    private async Task DetachAsync(Guid documentId)
    {
        IsSaving = true;
        ErrorMessage = null;

        try
        {
            var updated = await NoteAccessor.DetachDocumentAsync(NoteId, documentId);
            await OnChanged.InvokeAsync(updated);
        }
        catch (NoteNotFoundException)
        {
            ErrorMessage = "This note no longer exists. It may have been deleted in another tab.";
        }
        finally
        {
            IsSaving = false;
        }
    }

    private void OpenUploadDialog() => IsUploadDialogOpen = true;

    private void CloseUploadDialog() => IsUploadDialogOpen = false;

    private async Task HandleUploadedAsync(DocumentDto uploaded)
    {
        IsSaving = true;
        ErrorMessage = null;

        try
        {
            AllDocuments = await DocumentAccessor.GetAllAsync();
            var updated = await NoteAccessor.AttachDocumentAsync(NoteId, uploaded.Id);
            await OnChanged.InvokeAsync(updated);
        }
        catch (NoteNotFoundException)
        {
            ErrorMessage = "This note no longer exists. It may have been deleted in another tab.";
        }
        finally
        {
            IsSaving = false;
        }
    }
}
