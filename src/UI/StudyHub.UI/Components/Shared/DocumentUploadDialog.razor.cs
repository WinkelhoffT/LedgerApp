using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using StudyHub.Logic.Integration.Contract.Courses;
using StudyHub.Logic.Integration.Contract.Documents;
using StudyHub.Logic.Integration.Contract.Semesters;
using StudyHub.Shared.Courses;
using StudyHub.Shared.Documents;
using StudyHub.Shared.Semesters;
using StudyHub.UI.Documents;

namespace StudyHub.UI.Components.Shared;

public partial class DocumentUploadDialog
{
    private enum AssignmentTarget { Course, Semester }

    private const long MaxFileSizeBytes = 25 * 1024 * 1024;

    [Inject]
    private IDocumentAccessor DocumentAccessor { get; set; } = default!;

    [Inject]
    private ICourseAccessor CourseAccessor { get; set; } = default!;

    [Inject]
    private ISemesterAccessor SemesterAccessor { get; set; } = default!;

    [Parameter]
    public bool IsOpen { get; set; }

    [Parameter]
    public EventCallback OnClose { get; set; }

    [Parameter]
    public EventCallback<DocumentDto> OnSaved { get; set; }

    private AssignmentTarget AssignmentKind { get; set; } = AssignmentTarget.Course;

    private Guid CourseId { get; set; }

    private Guid SemesterId { get; set; }

    private IBrowserFile? SelectedFile { get; set; }

    private bool IsSaving { get; set; }

    private string? ErrorMessage { get; set; }

    private bool _optionsLoaded;

    private IReadOnlyList<CourseDto> Courses { get; set; } = [];

    private IReadOnlyList<SemesterDto> Semesters { get; set; } = [];

    private IEnumerable<CourseDto> SelectableCourses => Courses.OrderByDescending(c => c.UpdatedAt);

    private IEnumerable<SemesterDto> SelectableSemesters => Semesters.OrderByDescending(s => s.UpdatedAt);

    protected override async Task OnParametersSetAsync()
    {
        if (!IsOpen || _optionsLoaded)
        {
            return;
        }

        _optionsLoaded = true;
        Courses = await CourseAccessor.GetAllAsync();
        Semesters = await SemesterAccessor.GetAllAsync();
    }

    private void HandleFileSelected(InputFileChangeEventArgs e)
    {
        ErrorMessage = null;
        SelectedFile = e.File;
    }

    private async Task SubmitAsync()
    {
        if (SelectedFile is null)
        {
            ErrorMessage = "Choose a file to upload.";
            return;
        }

        IsSaving = true;
        ErrorMessage = null;

        try
        {
            await using var stream = SelectedFile.OpenReadStream(MaxFileSizeBytes);
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);

            var courseId = AssignmentKind == AssignmentTarget.Course && CourseId != Guid.Empty ? CourseId : (Guid?)null;
            var semesterId = AssignmentKind == AssignmentTarget.Semester && SemesterId != Guid.Empty ? SemesterId : (Guid?)null;

            var request = new UploadDocumentRequest(SelectedFile.Name, SelectedFile.ContentType, buffer.ToArray(), courseId, semesterId);

            var saved = await DocumentAccessor.UploadAsync(request);

            await OnSaved.InvokeAsync(saved);
            await Close();
        }
        catch (IOException)
        {
            ErrorMessage = $"The selected file exceeds the maximum allowed size of {FileSizeFormatter.Format(MaxFileSizeBytes)}.";
        }
        catch (UnsupportedDocumentTypeException)
        {
            ErrorMessage = "Only PDF and DOCX files can be uploaded.";
        }
        catch (DocumentTooLargeException)
        {
            ErrorMessage = $"The selected file exceeds the maximum allowed size of {FileSizeFormatter.Format(MaxFileSizeBytes)}.";
        }
        catch (DocumentValidationException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (CourseNotFoundException)
        {
            ErrorMessage = "Select a course.";
        }
        catch (CourseArchivedException)
        {
            ErrorMessage = "The selected course is archived. Choose an active course.";
        }
        catch (SemesterNotFoundException)
        {
            ErrorMessage = "Select a semester.";
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
        SelectedFile = null;
        CourseId = Guid.Empty;
        SemesterId = Guid.Empty;
        AssignmentKind = AssignmentTarget.Course;
        _optionsLoaded = false;
        await OnClose.InvokeAsync();
    }
}
