using Microsoft.AspNetCore.Components;
using StudyHub.Logic.Integration.Courses;
using StudyHub.Logic.Integration.Flashcards;
using StudyHub.Shared.Courses;
using StudyHub.Shared.Flashcards;

namespace StudyHub.UI.Components.Shared;

public partial class FlashcardDeckFormDialog
{
    [Inject]
    private IFlashcardDeckAccessor DeckAccessor { get; set; } = default!;

    [Inject]
    private ICourseAccessor CourseAccessor { get; set; } = default!;

    [Parameter]
    public bool IsOpen { get; set; }

    [Parameter]
    public FlashcardDeckDto? EditingDeck { get; set; }

    [Parameter]
    public EventCallback OnClose { get; set; }

    [Parameter]
    public EventCallback<FlashcardDeckDto> OnSaved { get; set; }

    private string Name { get; set; } = string.Empty;

    private Guid CourseId { get; set; }

    private int NewCardsPerDay { get; set; } = FlashcardDeck.DefaultNewCardsPerDay;

    private int ReviewsPerDay { get; set; } = FlashcardDeck.DefaultReviewsPerDay;

    private bool IsSaving { get; set; }

    private string? ErrorMessage { get; set; }

    private IReadOnlyList<CourseDto> Courses { get; set; } = [];

    private FlashcardDeckDto? _lastLoadedDeck;

    private bool _loaded;

    // Archived courses are offered only when the deck is already linked to one.
    private IEnumerable<CourseDto> SelectableCourses =>
        Courses.Where(c => !c.IsArchived || c.Id == EditingDeck?.CourseId).OrderBy(c => c.Name);

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
        CourseId = EditingDeck?.CourseId ?? Guid.Empty;
        NewCardsPerDay = EditingDeck?.NewCardsPerDay ?? FlashcardDeck.DefaultNewCardsPerDay;
        ReviewsPerDay = EditingDeck?.ReviewsPerDay ?? FlashcardDeck.DefaultReviewsPerDay;

        Courses = await CourseAccessor.GetAllAsync();
    }

    private async Task SubmitAsync()
    {
        IsSaving = true;
        ErrorMessage = null;
        var courseId = CourseId == Guid.Empty ? (Guid?)null : CourseId;

        try
        {
            var saved = EditingDeck is null
                ? await DeckAccessor.CreateAsync(new CreateFlashcardDeckRequest(Name, courseId, NewCardsPerDay, ReviewsPerDay))
                : await DeckAccessor.UpdateAsync(new UpdateFlashcardDeckRequest(EditingDeck.Id, Name, courseId, NewCardsPerDay, ReviewsPerDay));

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
        finally
        {
            IsSaving = false;
        }
    }

    private async Task Close()
    {
        ErrorMessage = null;
        _lastLoadedDeck = null;
        _loaded = false;
        await OnClose.InvokeAsync();
    }
}
