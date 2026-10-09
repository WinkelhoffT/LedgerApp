using Microsoft.AspNetCore.Components;
using StudyHub.Logic.Integration.PracticeExams;
using StudyHub.Shared.PracticeExams;
using StudyHub.UI.Services;

namespace StudyHub.UI.Components.Pages;

public partial class PracticeExams
{
    [Inject]
    private IPracticeExamAccessor PracticeExamAccessor { get; set; } = default!;

    [Inject]
    private IPageHeaderStateHolder PageHeader { get; set; } = default!;

    private IReadOnlyList<PracticeExamDto>? Exams { get; set; }

    private bool ShowArchived { get; set; }

    private string? ErrorMessage { get; set; }

    protected override async Task OnInitializedAsync()
    {
        PageHeader.SetHeader("Practice Exams", "Write an exam, then see where you stand");
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        ErrorMessage = null;

        try
        {
            Exams = await PracticeExamAccessor.GetAllAsync(ShowArchived);
        }
        catch (HttpRequestException)
        {
            Exams = [];
            ErrorMessage = "StudyHub.Api could not be reached. Try again in a moment.";
        }
    }
}
