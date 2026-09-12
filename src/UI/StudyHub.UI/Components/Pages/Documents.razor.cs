using Microsoft.AspNetCore.Components;
using StudyHub.Logic.Business.Courses;
using StudyHub.Logic.Business.Documents;
using StudyHub.Logic.Business.Semesters;
using StudyHub.UI.Courses;
using StudyHub.UI.Documents;
using StudyHub.UI.Semesters;
using StudyHub.UI.Services;

namespace StudyHub.UI.Components.Pages;

public partial class Documents
{
    [Inject]
    private IDocumentAccessor DocumentAccessor { get; set; } = default!;

    [Inject]
    private ICourseAccessor CourseAccessor { get; set; } = default!;

    [Inject]
    private ISemesterAccessor SemesterAccessor { get; set; } = default!;

    [Inject]
    private IPageHeaderStateHolder PageHeader { get; set; } = default!;

    private IReadOnlyList<DocumentDto>? DocumentList { get; set; }

    private IReadOnlyList<CourseDto> Courses { get; set; } = [];

    private Dictionary<Guid, string> CourseNamesById { get; set; } = [];

    private IReadOnlyList<SemesterDto> Semesters { get; set; } = [];

    private Dictionary<Guid, string> SemesterNamesById { get; set; } = [];

    private string SelectedScope { get; set; } = "all";

    private bool ShowArchived { get; set; }

    private bool IsUploadDialogOpen { get; set; }

    private bool IsDetailsDialogOpen { get; set; }

    private DocumentDto? SelectedDocument { get; set; }

    private IReadOnlyList<DocumentDto> VisibleDocuments =>
        DocumentList is null
            ? []
            : DocumentList.Where(d => ShowArchived || !d.IsArchived).ToList();

    protected override async Task OnInitializedAsync()
    {
        PageHeader.SetHeader("Documents", "Course materials, scripts, and notes you've uploaded");

        Courses = await CourseAccessor.GetAllAsync();
        CourseNamesById = Courses.ToDictionary(c => c.Id, c => c.Name);

        Semesters = await SemesterAccessor.GetAllAsync();
        SemesterNamesById = Semesters.ToDictionary(s => s.Id, s => s.Name);

        await LoadDocumentsAsync();
    }

    private async Task LoadDocumentsAsync()
    {
        DocumentList = SelectedScope switch
        {
            "all" => await DocumentAccessor.GetAllAsync(),
            var scope when scope.StartsWith("course:", StringComparison.Ordinal)
                => await DocumentAccessor.GetByCourseIdAsync(Guid.Parse(scope["course:".Length..])),
            var scope when scope.StartsWith("semester:", StringComparison.Ordinal)
                => await DocumentAccessor.GetBySemesterIdAsync(Guid.Parse(scope["semester:".Length..])),
            _ => await DocumentAccessor.GetAllAsync(),
        };
    }

    private string GetParentName(DocumentDto document) =>
        document.CourseId is { } courseId
            ? CourseNamesById.GetValueOrDefault(courseId, "Unknown course")
            : SemesterNamesById.GetValueOrDefault(document.SemesterId ?? Guid.Empty, "Unknown semester");

    private void OpenUploadDialog() => IsUploadDialogOpen = true;

    private void CloseUploadDialog() => IsUploadDialogOpen = false;

    private async Task HandleUploadedAsync(DocumentDto _) => await LoadDocumentsAsync();

    private void OpenDetailsDialog(DocumentDto document)
    {
        SelectedDocument = document;
        IsDetailsDialogOpen = true;
    }

    private void CloseDetailsDialog()
    {
        IsDetailsDialogOpen = false;
        SelectedDocument = null;
    }

    private async Task HandleChangedAsync(DocumentDto updated)
    {
        SelectedDocument = updated;
        await LoadDocumentsAsync();
    }
}
