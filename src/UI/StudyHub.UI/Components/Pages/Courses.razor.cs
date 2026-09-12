using Microsoft.AspNetCore.Components;
using StudyHub.Logic.Business.Courses;
using StudyHub.UI.Courses;
using StudyHub.UI.Semesters;
using StudyHub.UI.Services;

namespace StudyHub.UI.Components.Pages;

public partial class Courses
{
    [Inject]
    private ICourseAccessor CourseAccessor { get; set; } = default!;

    [Inject]
    private ISemesterAccessor SemesterAccessor { get; set; } = default!;

    [Inject]
    private IPageHeaderStateHolder PageHeader { get; set; } = default!;

    private IReadOnlyList<CourseDto>? CourseList { get; set; }

    private Dictionary<Guid, string> SemesterNamesById { get; set; } = [];

    private bool ShowArchived { get; set; }

    private bool IsDialogOpen { get; set; }

    private CourseDto? EditingCourse { get; set; }

    private IReadOnlyList<CourseDto> VisibleCourses =>
        CourseList is null
            ? []
            : CourseList.Where(c => ShowArchived || !c.IsArchived).ToList();

    protected override async Task OnInitializedAsync()
    {
        PageHeader.SetHeader("Courses", "Manage the courses you're studying");
        await LoadCoursesAsync();
    }

    private async Task LoadCoursesAsync()
    {
        CourseList = await CourseAccessor.GetAllAsync();

        var semesters = await SemesterAccessor.GetAllAsync();
        SemesterNamesById = semesters.ToDictionary(s => s.Id, s => s.Name);
    }

    private string GetSemesterName(Guid semesterId) =>
        SemesterNamesById.GetValueOrDefault(semesterId, "Unknown semester");

    private void OpenCreateDialog()
    {
        EditingCourse = null;
        IsDialogOpen = true;
    }

    private void OpenEditDialog(CourseDto course)
    {
        EditingCourse = course;
        IsDialogOpen = true;
    }

    private void CloseDialog()
    {
        IsDialogOpen = false;
        EditingCourse = null;
    }

    private async Task HandleSavedAsync(CourseDto _)
    {
        await LoadCoursesAsync();
    }

    private async Task ArchiveAsync(CourseDto course)
    {
        await CourseAccessor.ArchiveAsync(course.Id);
        await LoadCoursesAsync();
    }

    private async Task RestoreAsync(CourseDto course)
    {
        await CourseAccessor.RestoreAsync(course.Id);
        await LoadCoursesAsync();
    }
}
