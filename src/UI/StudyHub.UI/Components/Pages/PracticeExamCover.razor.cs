using Microsoft.AspNetCore.Components;
using StudyHub.Logic.Integration.PracticeExams;
using StudyHub.Shared.PracticeExams;
using StudyHub.UI.Services;

namespace StudyHub.UI.Components.Pages;

public partial class PracticeExamCover
{
    // Below this share of "one point per minute", the cover page explains why the exam is short.
    private const double ShortExamRatio = 0.75;

    [Inject]
    private IPracticeExamAccessor PracticeExamAccessor { get; set; } = default!;

    [Inject]
    private IPracticeExamAttemptAccessor AttemptAccessor { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private IPageHeaderStateHolder PageHeader { get; set; } = default!;

    [Parameter]
    public Guid ExamId { get; set; }

    private PracticeExamDto? Exam { get; set; }

    private IReadOnlyList<PracticeExamAttemptSummaryDto> Attempts { get; set; } = [];

    private bool NotFound { get; set; }

    private bool IsBusy { get; set; }

    private string? ErrorMessage { get; set; }

    private bool IsShort =>
        Exam is not null && Exam.TotalPoints < Exam.DurationMinutes * ShortExamRatio;

    protected override async Task OnParametersSetAsync()
    {
        PageHeader.SetHeader("Practice Exam", "Cover page");
        NotFound = false;
        ErrorMessage = null;

        try
        {
            Exam = await PracticeExamAccessor.GetByIdAsync(ExamId);
            Attempts = await PracticeExamAccessor.GetAttemptsAsync(ExamId);
        }
        catch (PracticeExamNotFoundException)
        {
            NotFound = true;
        }
        catch (HttpRequestException)
        {
            NotFound = true;
            ErrorMessage = "StudyHub.Api could not be reached. Try again in a moment.";
        }
    }

    private async Task StartAsync(bool withTimeLimit)
    {
        await RunAsync(async () =>
        {
            var sheet = await AttemptAccessor.StartAsync(
                ExamId,
                new StartPracticeExamAttemptRequest(withTimeLimit)
            );
            NavigationManager.NavigateTo($"practice-exams/attempts/{sheet.AttemptId}");
        });
    }

    private Task ArchiveAsync() =>
        RunAsync(async () => Exam = await PracticeExamAccessor.ArchiveAsync(ExamId));

    private Task RestoreAsync() =>
        RunAsync(async () => Exam = await PracticeExamAccessor.RestoreAsync(ExamId));

    private async Task RunAsync(Func<Task> action)
    {
        ErrorMessage = null;
        IsBusy = true;

        try
        {
            await action();
        }
        catch (Exception ex)
            when (ex
                    is PracticeExamArchivedException
                        or PracticeExamValidationException
                        or PracticeExamNotFoundException
            )
        {
            ErrorMessage = ex.Message;
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "StudyHub.Api could not be reached. Try again in a moment.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
