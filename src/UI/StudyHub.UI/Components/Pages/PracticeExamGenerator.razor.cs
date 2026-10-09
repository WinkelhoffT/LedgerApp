using System.Globalization;
using Microsoft.AspNetCore.Components;
using StudyHub.Logic.Integration.Courses;
using StudyHub.Logic.Integration.Flashcards;
using StudyHub.Logic.Integration.Notes;
using StudyHub.Logic.Integration.PracticeExams;
using StudyHub.Shared.Ai;
using StudyHub.Shared.Courses;
using StudyHub.Shared.Flashcards;
using StudyHub.Shared.Notes;
using StudyHub.Shared.PracticeExams;
using StudyHub.UI.Services;

namespace StudyHub.UI.Components.Pages;

public partial class PracticeExamGenerator
{
    [Inject]
    private IPracticeExamGenerationAccessor GenerationAccessor { get; set; } = default!;

    [Inject]
    private ICourseAccessor CourseAccessor { get; set; } = default!;

    [Inject]
    private INoteAccessor NoteAccessor { get; set; } = default!;

    [Inject]
    private IFlashcardDeckAccessor DeckAccessor { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private IPageHeaderStateHolder PageHeader { get; set; } = default!;

    /// <summary>Preselects a course, e.g. when coming from the course list.</summary>
    [SupplyParameterFromQuery]
    public Guid? CourseId { get; set; }

    /// <summary>Preselects a deck, e.g. when coming from a deck page.</summary>
    [SupplyParameterFromQuery]
    public Guid? DeckId { get; set; }

    private IReadOnlyList<CourseDto>? Courses { get; set; }

    private IReadOnlyList<FlashcardDeckDto> Decks { get; set; } = [];

    private IReadOnlyList<AiModelDto> Models { get; set; } = [];

    private PracticeExamSourceKind SourceKind { get; set; } = PracticeExamSourceKind.Course;

    private Guid SelectedCourseId { get; set; }

    private IReadOnlyList<NoteDto> CourseNotes { get; set; } = [];

    private HashSet<Guid> SelectedNoteIds { get; set; } = [];

    private Guid SelectedDeckId { get; set; }

    private int? DeckLength { get; set; }

    private PracticeExamLevel Level { get; set; } = PracticeExamLevel.University;

    private int DurationMinutes { get; set; } = GeneratePracticeExamRequest.DefaultDurationMinutes;

    private string? FocusHint { get; set; }

    private string? SelectedModel { get; set; }

    private bool IsGenerating { get; set; }

    private string? ErrorMessage { get; set; }

    private int SelectedLength =>
        CourseNotes.Where(n => SelectedNoteIds.Contains(n.Id)).Sum(GetLength);

    private bool IsMaterialOverLimit =>
        SourceKind == PracticeExamSourceKind.Course
        && SelectedLength > GeneratePracticeExamRequest.MaxMaterialLength;

    private bool CanGenerate =>
        !IsGenerating
        && (
            SourceKind == PracticeExamSourceKind.Course
                ? SelectedCourseId != Guid.Empty
                    && SelectedNoteIds.Count > 0
                    && !IsMaterialOverLimit
                : SelectedDeckId != Guid.Empty
        );

    protected override async Task OnInitializedAsync()
    {
        PageHeader.SetHeader("New Practice Exam", "Let Claude write an exam from your material");

        try
        {
            var courses = await CourseAccessor.GetAllAsync();
            Decks = await DeckAccessor.GetAllAsync(includeArchived: false);
            Models = await GenerationAccessor.GetModelsAsync();
            SelectedModel =
                Models.FirstOrDefault(m => m.IsDefault)?.Id ?? Models.FirstOrDefault()?.Id;
            Courses = courses.Where(c => !c.IsArchived).OrderBy(c => c.Name).ToList();
        }
        catch (HttpRequestException)
        {
            Courses = [];
            ErrorMessage = "StudyHub.Api could not be reached. Try again in a moment.";
            return;
        }

        if (DeckId is { } deckId && Decks.Any(d => d.Id == deckId))
        {
            SourceKind = PracticeExamSourceKind.Deck;
            SelectedDeckId = deckId;
            await LoadDeckCardsAsync();
        }
        else if (CourseId is { } courseId && Courses.Any(c => c.Id == courseId))
        {
            SelectedCourseId = courseId;
            await LoadNotesAsync();
        }
    }

    // Every note with content is selected when a course is chosen; the student can deselect some.
    private async Task LoadNotesAsync()
    {
        ErrorMessage = null;
        CourseNotes = [];
        SelectedNoteIds = [];
        if (SelectedCourseId == Guid.Empty)
        {
            return;
        }

        try
        {
            CourseNotes = (await NoteAccessor.GetByCourseIdAsync(SelectedCourseId))
                .Where(n => !n.IsArchived)
                .OrderBy(n => n.Title, StringComparer.OrdinalIgnoreCase)
                .ToList();
            SelectedNoteIds = CourseNotes
                .Where(n => !string.IsNullOrWhiteSpace(n.Content))
                .Select(n => n.Id)
                .ToHashSet();
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "StudyHub.Api could not be reached. Try again in a moment.";
        }
    }

    private async Task LoadDeckCardsAsync()
    {
        ErrorMessage = null;
        DeckLength = null;
        if (SelectedDeckId == Guid.Empty)
        {
            return;
        }

        try
        {
            var cards = await DeckAccessor.GetCardsAsync(SelectedDeckId, search: null);
            DeckLength = cards.Sum(c =>
                c.Front.Length + c.Back.Length + string.Join(' ', c.Tags).Length
            );
        }
        catch (Exception ex) when (ex is FlashcardDeckNotFoundException or HttpRequestException)
        {
            ErrorMessage = "The deck's cards could not be loaded.";
        }
    }

    private void ToggleNote(Guid noteId, bool isSelected)
    {
        if (isSelected)
        {
            SelectedNoteIds.Add(noteId);
        }
        else
        {
            SelectedNoteIds.Remove(noteId);
        }
    }

    private async Task GenerateAsync()
    {
        ErrorMessage = null;
        IsGenerating = true;

        try
        {
            var isCourse = SourceKind == PracticeExamSourceKind.Course;
            var exam = await GenerationAccessor.GenerateAsync(
                new GeneratePracticeExamRequest(
                    SourceKind,
                    isCourse ? SelectedCourseId : null,
                    isCourse ? SelectedNoteIds.ToList() : null,
                    isCourse ? null : SelectedDeckId,
                    Level,
                    DurationMinutes,
                    string.IsNullOrWhiteSpace(FocusHint) ? null : FocusHint,
                    SelectedModel
                )
            );
            NavigationManager.NavigateTo($"practice-exams/{exam.Id}");
        }
        catch (Exception ex)
            when (ex
                    is PracticeExamValidationException
                        or AiGenerationFailedException
                        or AiNotConfiguredException
                        or CourseNotFoundException
                        or CourseArchivedException
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
        catch (TaskCanceledException)
        {
            ErrorMessage =
                "Writing the practice exam took too long. Try a shorter duration or less material.";
        }
        finally
        {
            IsGenerating = false;
        }
    }

    private static int GetLength(NoteDto note) => note.Title.Length + note.Content.Length;

    private static string FormatCharacters(int characters) =>
        characters.ToString("N0", CultureInfo.InvariantCulture);
}
