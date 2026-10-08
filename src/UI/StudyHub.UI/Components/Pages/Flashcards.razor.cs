using Microsoft.AspNetCore.Components;
using StudyHub.Logic.Integration.Courses;
using StudyHub.Logic.Integration.Flashcards;
using StudyHub.Logic.Integration.Semesters;
using StudyHub.Shared.Flashcards;
using StudyHub.UI.Services;

namespace StudyHub.UI.Components.Pages;

public partial class Flashcards
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
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private IPageHeaderStateHolder PageHeader { get; set; } = default!;

    private IReadOnlyList<FlashcardDeckDto>? Decks { get; set; }

    private IReadOnlyList<FlashcardDeckDto> ActiveDecks => Decks?.Where(d => !d.IsArchived).ToList() ?? [];

    private Dictionary<Guid, string> CourseNamesById { get; set; } = [];

    private Dictionary<Guid, string> SemesterNamesById { get; set; } = [];

    private bool ShowArchived { get; set; }

    private bool IsDeckDialogOpen { get; set; }

    private bool IsImportOpen { get; set; }

    private Guid? ExportingDeckId { get; set; }

    private string? ErrorMessage { get; set; }

    protected override async Task OnInitializedAsync()
    {
        PageHeader.SetHeader("Flashcards", "Spaced repetition");

        var courses = await CourseAccessor.GetAllAsync();
        CourseNamesById = courses.ToDictionary(c => c.Id, c => c.Name);
        var semesters = await SemesterAccessor.GetAllAsync();
        SemesterNamesById = semesters.ToDictionary(s => s.Id, s => s.Name);
        await LoadDecksAsync();
    }

    private async Task LoadDecksAsync()
    {
        Decks = await DeckAccessor.GetAllAsync(ShowArchived);
    }

    private string? GetOwnerName(FlashcardDeckDto deck) => deck switch
    {
        { CourseId: { } courseId } => CourseNamesById.GetValueOrDefault(courseId, "Unknown course"),
        { SemesterId: { } semesterId } => SemesterNamesById.GetValueOrDefault(semesterId, "Unknown semester"),
        _ => null,
    };

    private void HandleDeckCreated(FlashcardDeckDto deck) =>
        NavigationManager.NavigateTo($"flashcards/decks/{deck.Id}");

    private async Task ExportAsync(FlashcardDeckDto deck)
    {
        ErrorMessage = null;
        ExportingDeckId = deck.Id;

        try
        {
            var export = await TransferAccessor.ExportAsync(deck.Id);
            await FileDownloadAccessor.DownloadAsync(export.FileName, export.ContentType, export.Content);
        }
        catch (FlashcardDeckNotFoundException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "StudyHub.Api could not be reached. Try again in a moment.";
        }
        finally
        {
            ExportingDeckId = null;
        }
    }
}
