using Microsoft.AspNetCore.Components;
using StudyHub.Logic.Integration.Courses;
using StudyHub.Logic.Integration.Semesters;
using StudyHub.Logic.Integration.StudySessions;
using StudyHub.Shared.Courses;
using StudyHub.Shared.Semesters;
using StudyHub.Shared.StudySessions;

namespace StudyHub.UI.Components.Shared;

public partial class StudySessionFormDialog
{
    // One dropdown offers courses and semesters; the option value says which kind was picked.
    private const string CoursePrefix = "course:";
    private const string SemesterPrefix = "semester:";

    [Inject]
    private IStudySessionAccessor SessionAccessor { get; set; } = default!;

    [Inject]
    private ICourseAccessor CourseAccessor { get; set; } = default!;

    [Inject]
    private ISemesterAccessor SemesterAccessor { get; set; } = default!;

    [Parameter]
    public bool IsOpen { get; set; }

    /// <summary>The session to edit; <c>null</c> adds a new one.</summary>
    [Parameter]
    public StudySessionDto? EditingSession { get; set; }

    /// <summary>Prefilled date of a new session.</summary>
    [Parameter]
    public DateOnly NewSessionDate { get; set; }

    /// <summary>Prefilled start of a new session.</summary>
    [Parameter]
    public TimeOnly NewSessionStartTime { get; set; }

    [Parameter]
    public EventCallback OnClose { get; set; }

    [Parameter]
    public EventCallback<StudySessionDto> OnSaved { get; set; }

    [Parameter]
    public EventCallback<StudySessionDto> OnDeleted { get; set; }

    private string Title { get; set; } = string.Empty;

    private string OwnerKey { get; set; } = string.Empty;

    private DateOnly Date { get; set; }

    private TimeOnly StartTime { get; set; }

    private int DurationMinutes { get; set; } = StudySession.DefaultDurationMinutes;

    private string? Location { get; set; }

    private bool IsSaving { get; set; }

    private bool IsConfirmingDelete { get; set; }

    private string? ErrorMessage { get; set; }

    private IReadOnlyList<CourseDto> Courses { get; set; } = [];

    private IReadOnlyList<SemesterDto> Semesters { get; set; } = [];

    private bool _loaded;

    // Archived courses and semesters are offered only when the session is already linked to one.
    private IEnumerable<CourseDto> SelectableCourses =>
        Courses.Where(c => !c.IsArchived || c.Id == EditingSession?.CourseId).OrderBy(c => c.Name);

    private IEnumerable<SemesterDto> SelectableSemesters =>
        Semesters.Where(s => !s.IsArchived || s.Id == EditingSession?.SemesterId).OrderByDescending(s => s.StartDate);

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
        Title = EditingSession?.Title ?? string.Empty;
        OwnerKey = EditingSession switch
        {
            { CourseId: { } courseId } => CoursePrefix + courseId,
            { SemesterId: { } semesterId } => SemesterPrefix + semesterId,
            _ => string.Empty,
        };
        Date = EditingSession?.Date ?? NewSessionDate;
        StartTime = EditingSession?.StartTime ?? NewSessionStartTime;
        DurationMinutes = EditingSession?.DurationMinutes ?? StudySession.DefaultDurationMinutes;
        Location = EditingSession?.Location;

        Courses = await CourseAccessor.GetAllAsync();
        Semesters = await SemesterAccessor.GetAllAsync();
    }

    private Task SubmitAsync() => RunAsync(async () =>
    {
        var courseId = GetOwnerId(CoursePrefix);
        var semesterId = GetOwnerId(SemesterPrefix);
        var saved = EditingSession is null
            ? await SessionAccessor.CreateAsync(new CreateStudySessionRequest(Title, courseId, semesterId, Date, StartTime, DurationMinutes, Location))
            : await SessionAccessor.UpdateAsync(new UpdateStudySessionRequest(EditingSession.Id, Title, courseId, semesterId, Date, StartTime, DurationMinutes, Location));

        await OnSaved.InvokeAsync(saved);
    });

    private Task DeleteAsync() => RunAsync(async () =>
    {
        var session = EditingSession!;
        await SessionAccessor.DeleteAsync(session.Id);
        await OnDeleted.InvokeAsync(session);
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
        catch (Exception ex) when (ex is StudySessionValidationException or StudySessionNotFoundException)
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
