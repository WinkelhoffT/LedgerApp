using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using StudyHub.Logic.Integration.Flashcards;
using StudyHub.Shared.Flashcards;

namespace StudyHub.UI.Components.Shared;

public partial class FlashcardImportDialog
{
    [Inject]
    private IFlashcardTransferAccessor TransferAccessor { get; set; } = default!;

    [Parameter]
    public bool IsOpen { get; set; }

    /// <summary>The active decks the user may import into.</summary>
    [Parameter]
    public IReadOnlyList<FlashcardDeckDto> Decks { get; set; } = [];

    [Parameter]
    public EventCallback OnClose { get; set; }

    /// <summary>Raised after a successful import, while the result is still shown.</summary>
    [Parameter]
    public EventCallback OnImported { get; set; }

    private IBrowserFile? SelectedFile { get; set; }

    private Guid TargetDeckId { get; set; }

    private ImportDuplicateMode DuplicateMode { get; set; } = ImportDuplicateMode.UpdateCurrent;

    private FlashcardImportResultDto? Result { get; set; }

    private bool IsImporting { get; set; }

    private string? ErrorMessage { get; set; }

    private void HandleFileSelected(InputFileChangeEventArgs e)
    {
        ErrorMessage = null;
        SelectedFile = e.File;
    }

    private async Task ImportAsync()
    {
        if (SelectedFile is null)
        {
            ErrorMessage = "Choose a file to import.";
            return;
        }

        IsImporting = true;
        ErrorMessage = null;

        try
        {
            await using var stream = SelectedFile.OpenReadStream(
                ImportFlashcardsRequest.MaxFileSizeBytes
            );
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);

            Result = await TransferAccessor.ImportAsync(
                new ImportFlashcardsRequest(
                    SelectedFile.Name,
                    buffer.ToArray(),
                    TargetDeckId == Guid.Empty ? null : TargetDeckId,
                    DuplicateMode
                )
            );

            await OnImported.InvokeAsync();
        }
        catch (IOException)
        {
            ErrorMessage = "The file is larger than 5 MB.";
        }
        catch (Exception ex)
            when (ex
                    is FlashcardImportException
                        or FlashcardDeckNotFoundException
                        or FlashcardDeckArchivedException
            )
        {
            ErrorMessage = ex.Message;
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "StudyHub.Api could not be reached. Try again in a moment.";
        }
        finally
        {
            IsImporting = false;
        }
    }

    private async Task Close()
    {
        ErrorMessage = null;
        SelectedFile = null;
        Result = null;
        TargetDeckId = Guid.Empty;
        DuplicateMode = ImportDuplicateMode.UpdateCurrent;
        await OnClose.InvokeAsync();
    }
}
