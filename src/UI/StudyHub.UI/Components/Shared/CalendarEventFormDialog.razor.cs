using Microsoft.AspNetCore.Components;
using StudyHub.Logic.Integration.CalendarEvents;
using StudyHub.Logic.Integration.Courses;
using StudyHub.Logic.Integration.Semesters;
using StudyHub.Shared.CalendarEvents;
using StudyHub.Shared.Courses;
using StudyHub.Shared.Semesters;

namespace StudyHub.UI.Components.Shared;

public partial class CalendarEventFormDialog
{
    // One dropdown offers courses and semesters; the option value says which kind was picked.
    private const string CoursePrefix = "course:";
    private const string SemesterPrefix = "semester:";

    private static readonly CalendarEventKind[] Kinds = [CalendarEventKind.Exam, CalendarEventKind.Deadline];

    [Inject]
    private ICalendarEventAccessor EventAccessor { get; set; } = default!;

    [Inject]
    private ICourseAccessor CourseAccessor { get; set; } = default!;

    [Inject]
    private ISemesterAccessor SemesterAccessor { get; set; } = default!;

    [Parameter]
    public bool IsOpen { get; set; }

    /// <summary>The exam or deadline to edit; <c>null</c> adds a new one.</summary>
    [Parameter]
    public CalendarEventDto? EditingEvent { get; set; }

    /// <summary>Prefilled date of a new exam or deadline.</summary>
    [Parameter]
    public DateOnly NewEventDate { get; set; }

    [Parameter]
    public EventCallback OnClose { get; set; }

    [Parameter]
    public EventCallback<CalendarEventDto> OnSaved { get; set; }

    [Parameter]
    public EventCallback<CalendarEventDto> OnDeleted { get; set; }

    private CalendarEventKind Kind { get; set; } = CalendarEventKind.Exam;

    private string Title { get; set; } = string.Empty;

    private string OwnerKey { get; set; } = string.Empty;

    private DateOnly Date { get; set; }

    private TimeOnly? StartTime { get; set; }

    private int? DurationMinutes { get; set; }

    private string? Location { get; set; }

    private bool IsSaving { get; set; }

    private bool IsConfirmingDelete { get; set; }

    private string? ErrorMessage { get; set; }

    private IReadOnlyList<CourseDto> Courses { get; set; } = [];

    private IReadOnlyList<SemesterDto> Semesters { get; set; } = [];

    private bool _loaded;

    // Only a timed exam has a duration.
    private bool HasDuration => Kind == CalendarEventKind.Exam && StartTime is not null;

    private string TimeHint => Kind == CalendarEventKind.Exam
        ? "Without a start time the exam is shown as all-day."
        : "Without a due time the deadline is shown as all-day.";

    // Archived courses and semesters are offered only when the event is already linked to one.
    private IEnumerable<CourseDto> SelectableCourses =>
        Courses.Where(c => !c.IsArchived || c.Id == EditingEvent?.CourseId).OrderBy(c => c.Name);

    private IEnumerable<SemesterDto> SelectableSemesters =>
        Semesters.Where(s => !s.IsArchived || s.Id == EditingEvent?.SemesterId).OrderByDescending(s => s.StartDate);

    protected override async Task OnParametersSetAsync()
    {
        // Fill the form once per opening, so re-renders of the parent don't reset what the user typed.
        if (!IsOpen)
        {
            _loaded = false;
            return;
        }

        if (_loaded)
        {
            return;
        }

        _loaded = true;
        ErrorMessage = null;
        IsConfirmingDelete = false;
        Kind = EditingEvent?.Kind ?? CalendarEventKind.Exam;
        Title = EditingEvent?.Title ?? string.Empty;
        OwnerKey = EditingEvent switch
        {
            { CourseId: { } courseId } => CoursePrefix + courseId,
            { SemesterId: { } semesterId } => SemesterPrefix + semesterId,
            _ => string.Empty,
        };
        Date = EditingEvent?.Date ?? NewEventDate;
        StartTime = EditingEvent?.StartTime;
        DurationMinutes = EditingEvent?.DurationMinutes ?? CalendarEvent.DefaultExamDurationMinutes;
        Location = EditingEvent?.Location;

        Courses = await CourseAccessor.GetAllAsync();
        Semesters = await SemesterAccessor.GetAllAsync();
    }

    private Task SubmitAsync() => RunAsync(async () =>
    {
        var courseId = GetOwnerId(CoursePrefix);
        var semesterId = GetOwnerId(SemesterPrefix);
        var durationMinutes = HasDuration ? DurationMinutes : null;
        var saved = EditingEvent is null
            ? await EventAccessor.CreateAsync(new CreateCalendarEventRequest(Kind, Title, courseId, semesterId, Date, StartTime, durationMinutes, Location))
            : await EventAccessor.UpdateAsync(new UpdateCalendarEventRequest(EditingEvent.Id, Kind, Title, courseId, semesterId, Date, StartTime, durationMinutes, Location));

        await OnSaved.InvokeAsync(saved);
    });

    private Task DeleteAsync() => RunAsync(async () =>
    {
        var calendarEvent = EditingEvent!;
        await EventAccessor.DeleteAsync(calendarEvent.Id);
        await OnDeleted.InvokeAsync(calendarEvent);
    });

    // Runs a save or delete, closing the dialog on success and showing the Api's message otherwise.
    private async Task RunAsync(Func<Task> action)
    {
        IsSaving = true;
        ErrorMessage = null;

        try
        {
            await action();
            await Close();
        }
        catch (Exception ex) when (ex is CalendarEventValidationException or CalendarEventNotFoundException)
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
        catch (HttpRequestException)
        {
            ErrorMessage = "StudyHub.Api could not be reached. Try again in a moment.";
        }
        finally
        {
            IsSaving = false;
            IsConfirmingDelete = false;
        }
    }

    private Guid? GetOwnerId(string prefix) =>
        OwnerKey.StartsWith(prefix, StringComparison.Ordinal) && Guid.TryParse(OwnerKey[prefix.Length..], out var id)
            ? id
            : null;

    private async Task Close()
    {
        ErrorMessage = null;
        _loaded = false;
        await OnClose.InvokeAsync();
    }
}
