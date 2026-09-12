using Microsoft.AspNetCore.Components;
using StudyHub.Logic.Business.Courses;
using StudyHub.Logic.Business.Semesters;
using StudyHub.Logic.Domain.Courses;
using StudyHub.Logic.Domain.Semesters;
using StudyHub.UI.Courses;
using StudyHub.UI.Semesters;

namespace StudyHub.UI.Components.Shared;

public partial class CourseFormDialog
{
    [Inject]
    private ICourseAccessor CourseAccessor { get; set; } = default!;

    [Inject]
    private ISemesterAccessor SemesterAccessor { get; set; } = default!;

    [Parameter]
    public bool IsOpen { get; set; }

    [Parameter]
    public CourseDto? EditingCourse { get; set; }

    [Parameter]
    public EventCallback OnClose { get; set; }

    [Parameter]
    public EventCallback<CourseDto> OnSaved { get; set; }

    private string Name { get; set; } = string.Empty;

    private string? Description { get; set; }

    private string Color { get; set; } = "#2563eb";

    private Guid SemesterId { get; set; }

    private bool IsSaving { get; set; }

    private string? ErrorMessage { get; set; }

    private CourseDto? _lastLoadedCourse;

    private bool _semestersLoaded;

    private IReadOnlyList<SemesterDto> Semesters { get; set; } = [];

    private IEnumerable<SemesterDto> SelectableSemesters =>
        Semesters.OrderByDescending(s => s.UpdatedAt);

    protected override async Task OnParametersSetAsync()
    {
        if (!IsOpen)
        {
            return;
        }

        if (_semestersLoaded && EditingCourse == _lastLoadedCourse)
        {
            return;
        }

        _lastLoadedCourse = EditingCourse;
        _semestersLoaded = true;
        ErrorMessage = null;
        Name = EditingCourse?.Name ?? string.Empty;
        Description = EditingCourse?.Description;
        Color = EditingCourse?.Color ?? "#2563eb";
        SemesterId = EditingCourse?.SemesterId ?? Guid.Empty;

        Semesters = await SemesterAccessor.GetAllAsync();
    }

    private async Task SubmitAsync()
    {
        IsSaving = true;
        ErrorMessage = null;

        try
        {
            var saved = EditingCourse is null
                ? await CourseAccessor.CreateAsync(new CreateCourseRequest(Name, Description, Color, SemesterId))
                : await CourseAccessor.UpdateAsync(new UpdateCourseRequest(EditingCourse.Id, Name, Description, Color, SemesterId));

            await OnSaved.InvokeAsync(saved);
            await Close();
        }
        catch (CourseValidationException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (DuplicateCourseNameException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (CourseArchivedException ex)
        {
            ErrorMessage = ex.Message;
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

    private async Task Close()
    {
        ErrorMessage = null;
        _lastLoadedCourse = null;
        _semestersLoaded = false;
        await OnClose.InvokeAsync();
    }
}
