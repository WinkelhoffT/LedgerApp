using Microsoft.AspNetCore.Components;
using StudyHub.Logic.Integration.Courses;
using StudyHub.Logic.Integration.Flashcards;
using StudyHub.Logic.Integration.Semesters;
using StudyHub.Shared.Courses;
using StudyHub.Shared.Flashcards;
using StudyHub.Shared.Semesters;

namespace StudyHub.UI.Components.Shared;

public partial class FlashcardDeckFormDialog
{
    // One dropdown offers courses and semesters; the option value says which kind was picked.
    private const string CoursePrefix = "course:";
    private const string SemesterPrefix = "semester:";

    [Inject]
    private IFlashcardDeckAccessor DeckAccessor { get; set; } = default!;

    [Inject]
    private ICourseAccessor CourseAccessor { get; set; } = default!;

    [Inject]
    private ISemesterAccessor SemesterAccessor { get; set; } = default!;

    [Parameter]
    public bool IsOpen { get; set; }

    [Parameter]
    public FlashcardDeckDto? EditingDeck { get; set; }

    [Parameter]
    public EventCallback OnClose { get; set; }

    [Parameter]
    public EventCallback<FlashcardDeckDto> OnSaved { get; set; }

    private string Name { get; set; } = string.Empty;

    private string OwnerKey { get; set; } = string.Empty;

    private int NewCardsPerDay { get; set; } = FlashcardDeck.DefaultNewCardsPerDay;

    private int ReviewsPerDay { get; set; } = FlashcardDeck.DefaultReviewsPerDay;

    private bool IsSaving { get; set; }

    private string? ErrorMessage { get; set; }

    private IReadOnlyList<CourseDto> Courses { get; set; } = [];

    private IReadOnlyList<SemesterDto> Semesters { get; set; } = [];

    private FlashcardDeckDto? _lastLoadedDeck;

    private bool _loaded;

    // Archived courses and semesters are offered only when the deck is already linked to one.
    private IEnumerable<CourseDto> SelectableCourses =>
        Courses.Where(c => !c.IsArchived || c.Id == EditingDeck?.CourseId).OrderBy(c => c.Name);

    private IEnumerable<SemesterDto> SelectableSemesters =>
        Semesters.Where(s => !s.IsArchived || s.Id == EditingDeck?.SemesterId).OrderByDescending(s => s.StartDate);

    protected override async Task OnParametersSetAsync()
    {
        if (!IsOpen || (_loaded && EditingDeck == _lastLoadedDeck))
        {
            return;
        }

        _lastLoadedDeck = EditingDeck;
        _loaded = true;
        ErrorMessage = null;
        Name = EditingDeck?.Name ?? string.Empty;
        OwnerKey = EditingDeck switch
        {
            { CourseId: { } courseId } => CoursePrefix + courseId,
            { SemesterId: { } semesterId } => SemesterPrefix + semesterId,
            _ => string.Empty,
        };
        NewCardsPerDay = EditingDeck?.NewCardsPerDay ?? FlashcardDeck.DefaultNewCardsPerDay;
        ReviewsPerDay = EditingDeck?.ReviewsPerDay ?? FlashcardDeck.DefaultReviewsPerDay;

        Courses = await CourseAccessor.GetAllAsync();
        Semesters = await SemesterAccessor.GetAllAsync();
    }

    private async Task SubmitAsync()
    {
        IsSaving = true;
        ErrorMessage = null;
        var courseId = GetOwnerId(CoursePrefix);
        var semesterId = GetOwnerId(SemesterPrefix);

        try
        {
            var saved = EditingDeck is null
                ? await DeckAccessor.CreateAsync(new CreateFlashcardDeckRequest(Name, courseId, semesterId, NewCardsPerDay, ReviewsPerDay))
                : await DeckAccessor.UpdateAsync(new UpdateFlashcardDeckRequest(EditingDeck.Id, Name, courseId, semesterId, NewCardsPerDay, ReviewsPerDay));

            await OnSaved.InvokeAsync(saved);
            await Close();
        }
        catch (Exception ex) when (ex is FlashcardValidationException or DuplicateFlashcardDeckNameException or FlashcardDeckArchivedException)
        {
            ErrorMessage = ex.Message;
        }
        catch (CourseNotFoundException)
        {
            ErrorMessage = "The selected course could not be found.";
        }
        catch (CourseArchivedException)
        {
            ErrorMessage = "The selected course is archived. Choose an active course.";
        }
        catch (SemesterNotFoundException)
        {
            ErrorMessage = "The selected semester could not be found.";
        }
        catch (SemesterArchivedException)
        {
            ErrorMessage = "The selected semester is archived. Choose an active semester.";
        }
        finally
        {
            IsSaving = false;
        }
    }

    private Guid? GetOwnerId(string prefix) =>
        OwnerKey.StartsWith(prefix, StringComparison.Ordinal) && Guid.TryParse(OwnerKey[prefix.Length..], out var id)
            ? id
            : null;

    private async Task Close()
    {
        ErrorMessage = null;
        _lastLoadedDeck = null;
        _loaded = false;
        await OnClose.InvokeAsync();
    }
}
