using Microsoft.AspNetCore.Components;
using StudyHub.Logic.Integration.Courses;
using StudyHub.Logic.Integration.Flashcards;
using StudyHub.Logic.Integration.Notes;
using StudyHub.Logic.Integration.Semesters;
using StudyHub.Shared.Ai;
using StudyHub.Shared.Courses;
using StudyHub.Shared.Flashcards;
using StudyHub.Shared.Notes;
using StudyHub.UI.Flashcards;
using StudyHub.UI.Services;

namespace StudyHub.UI.Components.Pages;

public partial class FlashcardGenerator
{
    [Inject]
    private IFlashcardAccessor FlashcardAccessor { get; set; } = default!;

    [Inject]
    private IFlashcardDeckAccessor DeckAccessor { get; set; } = default!;

    [Inject]
    private INoteAccessor NoteAccessor { get; set; } = default!;

    [Inject]
    private ICourseAccessor CourseAccessor { get; set; } = default!;

    [Inject]
    private ISemesterAccessor SemesterAccessor { get; set; } = default!;

    [Inject]
    private IPageHeaderStateHolder PageHeader { get; set; } = default!;

    /// <summary>Preselects a note, e.g. when coming from the Notes page.</summary>
    [SupplyParameterFromQuery]
    public Guid? NoteId { get; set; }

    private IReadOnlyList<NoteGroup>? NoteGroups { get; set; }

    private Dictionary<Guid, NoteDto> NotesById { get; set; } = [];

    private Guid SelectedNoteId { get; set; }

    private IReadOnlyList<AiModelDto> Models { get; set; } = [];

    private string? SelectedModel { get; set; }

    private int CardCount { get; set; } = GenerateFlashcardsRequest.DefaultCardCount;

    private string? FocusHint { get; set; }

    private FlashcardSetDto? CurrentSet { get; set; }

    private List<EditableFlashcard> Cards { get; set; } = [];

    private IReadOnlyList<FlashcardDeckDto> Decks { get; set; } = [];

    private Guid SaveDeckId { get; set; }

    private string NewDeckName { get; set; } = string.Empty;

    private FlashcardDeckDto? SavedDeck { get; set; }

    private int SavedCardCount { get; set; }

    private bool IsGenerating { get; set; }

    private bool IsSaving { get; set; }

    private bool IsBusy => IsGenerating || IsSaving;

    private string? ErrorMessage { get; set; }

    private string? CurrentNoteTitle =>
        CurrentSet is not null && NotesById.TryGetValue(CurrentSet.NoteId, out var note) ? note.Title : null;

    protected override async Task OnInitializedAsync()
    {
        PageHeader.SetHeader("Generate Flashcards", "Turn your notes into review cards");

        var notes = (await NoteAccessor.GetAllAsync()).Where(n => !n.IsArchived).ToList();
        var courses = await CourseAccessor.GetAllAsync();
        var semesters = await SemesterAccessor.GetAllAsync();
        NotesById = notes.ToDictionary(n => n.Id);
        Decks = await DeckAccessor.GetAllAsync(includeArchived: false);

        Models = await FlashcardAccessor.GetModelsAsync();
        SelectedModel = Models.FirstOrDefault(m => m.IsDefault)?.Id ?? Models.FirstOrDefault()?.Id;

        var courseGroups = courses
            .OrderBy(c => c.Name)
            .Select(c => new NoteGroup(c.Name, notes.Where(n => n.CourseId == c.Id).OrderBy(n => n.Title).ToList()));
        var semesterGroups = semesters
            .OrderBy(s => s.Name)
            .Select(s => new NoteGroup(s.Name, notes.Where(n => n.SemesterId == s.Id).OrderBy(n => n.Title).ToList()));

        NoteGroups = courseGroups.Concat(semesterGroups).Where(g => g.Notes.Count > 0).ToList();

        if (NoteId is { } noteId && notes.Any(n => n.Id == noteId))
        {
            SelectedNoteId = noteId;
        }
    }

    private async Task GenerateAsync()
    {
        ErrorMessage = null;
        SavedDeck = null;
        IsGenerating = true;

        try
        {
            CurrentSet = await FlashcardAccessor.GenerateAsync(
                new GenerateFlashcardsRequest(
                    SelectedNoteId,
                    CardCount,
                    string.IsNullOrWhiteSpace(FocusHint) ? null : FocusHint,
                    SelectedModel));
            Cards = CurrentSet.Cards.Select(EditableFlashcard.FromDto).ToList();
            NewDeckName = CurrentNoteTitle ?? string.Empty;
        }
        catch (Exception ex) when (ex is FlashcardValidationException or FlashcardGenerationFailedException
                                       or AiNotConfiguredException or NoteNotFoundException or NoteArchivedException)
        {
            ErrorMessage = ex.Message;
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "StudyHub.Api could not be reached. Try again in a moment.";
        }
        catch (TaskCanceledException)
        {
            ErrorMessage = "Generating the flashcards took too long. Try fewer cards.";
        }
        finally
        {
            IsGenerating = false;
        }
    }

    private async Task SaveToDeckAsync()
    {
        if (CurrentSet is null)
        {
            return;
        }

        ErrorMessage = null;
        IsSaving = true;

        try
        {
            foreach (var card in Cards)
            {
                card.IsEditing = false;
            }

            var deck = SaveDeckId == Guid.Empty
                ? await CreateDeckForNoteAsync(CurrentSet.NoteId)
                : Decks.First(d => d.Id == SaveDeckId);

            var saved = await DeckAccessor.AddCardsAsync(
                new AddFlashcardsRequest(deck.Id, Cards.Select(c => c.ToDto()).ToList(), CurrentSet.NoteId));

            SavedDeck = deck;
            SavedCardCount = saved.Count;
            CurrentSet = null;
            Cards = [];
            Decks = await DeckAccessor.GetAllAsync(includeArchived: false);
            SaveDeckId = deck.Id;
        }
        catch (Exception ex) when (ex is FlashcardValidationException or DuplicateFlashcardDeckNameException
                                       or FlashcardDeckArchivedException or FlashcardDeckNotFoundException
                                       or NoteNotFoundException or CourseArchivedException)
        {
            ErrorMessage = ex is CourseArchivedException
                ? "The note's course is archived. Choose an existing deck instead."
                : ex.Message;
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "StudyHub.Api could not be reached. Try again in a moment.";
        }
        finally
        {
            IsSaving = false;
        }
    }

    // A new deck takes the note's course, so it shows up next to the course's other material.
    private Task<FlashcardDeckDto> CreateDeckForNoteAsync(Guid noteId) =>
        DeckAccessor.CreateAsync(new CreateFlashcardDeckRequest(
            NewDeckName,
            NotesById.GetValueOrDefault(noteId)?.CourseId,
            FlashcardDeck.DefaultNewCardsPerDay,
            FlashcardDeck.DefaultReviewsPerDay));

    private void RemoveCard(EditableFlashcard card) => Cards.Remove(card);

    private string GetModelName(string modelId) =>
        Models.FirstOrDefault(m => m.Id == modelId)?.DisplayName ?? modelId;

    private sealed record NoteGroup(string Label, IReadOnlyList<NoteDto> Notes);
}
