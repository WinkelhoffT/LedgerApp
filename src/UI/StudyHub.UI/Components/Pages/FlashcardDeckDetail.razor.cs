using Microsoft.AspNetCore.Components;
using StudyHub.Logic.Integration.Courses;
using StudyHub.Logic.Integration.Flashcards;
using StudyHub.Logic.Integration.Semesters;
using StudyHub.Shared.Flashcards;
using StudyHub.UI.Services;

namespace StudyHub.UI.Components.Pages;

public partial class FlashcardDeckDetail
{
    [Inject]
    private IFlashcardDeckAccessor DeckAccessor { get; set; } = default!;

    [Inject]
    private IFlashcardTransferAccessor TransferAccessor { get; set; } = default!;

    [Inject]
    private ICourseAccessor CourseAccessor { get; set; } = default!;

    [Inject]
    private ISemesterAccessor SemesterAccessor { get; set; } = default!;

    [Inject]
    private IFileDownloadAccessor FileDownloadAccessor { get; set; } = default!;

    [Inject]
    private IPageHeaderStateHolder PageHeader { get; set; } = default!;

    [Parameter]
    public Guid DeckId { get; set; }

    private FlashcardDeckDto? Deck { get; set; }

    /// <summary>Name of the course or semester the deck belongs to.</summary>
    private string? OwnerName { get; set; }

    private List<DeckCardDto>? Cards { get; set; }

    private string? Search { get; set; }

    private string? AppliedSearch { get; set; }

    private bool NotFound { get; set; }

    private bool IsBusy { get; set; }

    private bool IsSettingsOpen { get; set; }

    private bool IsCardDialogOpen { get; set; }

    private DeckCardDto? EditingCard { get; set; }

    private Guid? PendingDeleteId { get; set; }

    private string? ErrorMessage { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        PageHeader.SetHeader("Flashcards", "Deck");
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        try
        {
            NotFound = false;
            Deck = await DeckAccessor.GetByIdAsync(DeckId);
            Cards = (await DeckAccessor.GetCardsAsync(DeckId, AppliedSearch)).ToList();
            OwnerName = Deck switch
            {
                { CourseId: { } courseId } => (await CourseAccessor.GetAllAsync()).FirstOrDefault(c => c.Id == courseId)?.Name,
                { SemesterId: { } semesterId } => (await SemesterAccessor.GetAllAsync()).FirstOrDefault(s => s.Id == semesterId)?.Name,
                _ => null,
            };
            PageHeader.SetHeader(Deck.Name, "Flashcard deck");
        }
        catch (FlashcardDeckNotFoundException)
        {
            NotFound = true;
        }
    }

    private async Task SearchAsync()
    {
        AppliedSearch = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim();
        Cards = (await DeckAccessor.GetCardsAsync(DeckId, AppliedSearch)).ToList();
    }

    private void OpenAddCard()
    {
        EditingCard = null;
        IsCardDialogOpen = true;
    }

    private void OpenEditCard(DeckCardDto card)
    {
        EditingCard = card;
        IsCardDialogOpen = true;
    }

    private void CloseCardDialog()
    {
        IsCardDialogOpen = false;
        EditingCard = null;
    }

    private async Task HandleSettingsSavedAsync(FlashcardDeckDto _) => await ReloadAsync();

    private Task ArchiveAsync() => RunAsync(() => DeckAccessor.ArchiveAsync(DeckId));

    private Task RestoreAsync() => RunAsync(() => DeckAccessor.RestoreAsync(DeckId));

    private Task DeleteCardAsync(DeckCardDto card) => RunAsync(async () =>
    {
        PendingDeleteId = null;
        await DeckAccessor.DeleteCardAsync(card.Id);
    });

    private async Task ExportAsync()
    {
        ErrorMessage = null;
        IsBusy = true;

        try
        {
            var export = await TransferAccessor.ExportAsync(DeckId);
            await FileDownloadAccessor.DownloadAsync(export.FileName, export.ContentType, export.Content);
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "StudyHub.Api could not be reached. Try again in a moment.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RunAsync(Func<Task> action)
    {
        ErrorMessage = null;
        IsBusy = true;

        try
        {
            await action();
            await ReloadAsync();
        }
        catch (Exception ex) when (ex is FlashcardDeckArchivedException or FlashcardNotFoundException)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
