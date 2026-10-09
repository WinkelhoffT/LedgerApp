using Microsoft.AspNetCore.Components;
using StudyHub.Logic.Integration.PracticeExams;
using StudyHub.Shared.PracticeExams;
using StudyHub.UI.Services;

namespace StudyHub.UI.Components.Pages;

/// <summary>
/// The exam sheet while an attempt is in progress, the review after it is submitted. Answers are
/// saved as they change: a choice at once, text after a pause in typing and when the field loses
/// focus. The server checks the time limit on every save.
/// </summary>
public partial class PracticeExamAttempt : IAsyncDisposable
{
    private static readonly TimeSpan TypingPause = TimeSpan.FromSeconds(2);

    [Inject]
    private IPracticeExamAttemptAccessor AttemptAccessor { get; set; } = default!;

    [Inject]
    private IPracticeExamAccessor PracticeExamAccessor { get; set; } = default!;

    [Inject]
    private IPageHeaderStateHolder PageHeader { get; set; } = default!;

    [Parameter]
    public Guid AttemptId { get; set; }

    private PracticeExamSheetDto? Sheet { get; set; }

    private PracticeExamReviewDto? Review { get; set; }

    private Dictionary<Guid, Guid?> Selections { get; } = [];

    private Dictionary<Guid, string?> Texts { get; } = [];

    private TimeSpan? Remaining => Sheet?.DueAt is { } dueAt ? dueAt - DateTime.UtcNow : null;

    private int UnansweredCount =>
        Sheet?.Tasks.Count(t =>
            Selections.GetValueOrDefault(t.TaskId) is null
            && string.IsNullOrWhiteSpace(Texts.GetValueOrDefault(t.TaskId))
        )
        ?? 0;

    private string? SaveStatus { get; set; }

    private bool NotFound { get; set; }

    private bool IsSubmitting { get; set; }

    private bool IsConfirmOpen { get; set; }

    private bool IsBusy { get; set; }

    private string? ErrorMessage { get; set; }

    private readonly Dictionary<Guid, DateTime> _dirtySince = [];
    private readonly HashSet<Guid> _saving = [];
    private readonly CancellationTokenSource _timerCancellation = new();
    private Task? _timerTask;

    protected override async Task OnParametersSetAsync()
    {
        PageHeader.SetHeader("Practice Exam", "Write, submit, grade");
        await LoadAsync();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            _timerTask = RunTimerAsync(_timerCancellation.Token);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _timerCancellation.CancelAsync();
        if (_timerTask is not null)
        {
            await _timerTask;
        }

        // Typed text that is not saved yet would be lost when the student leaves the page.
        foreach (var taskId in _dirtySince.Keys.ToList())
        {
            await SaveAsync(taskId);
        }

        _timerCancellation.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task LoadAsync()
    {
        NotFound = false;
        ErrorMessage = null;

        try
        {
            ShowSheet(await AttemptAccessor.GetSheetAsync(AttemptId));
        }
        catch (PracticeExamAttemptSubmittedException)
        {
            await LoadReviewAsync();
        }
        catch (Exception ex)
            when (ex is PracticeExamAttemptNotFoundException or PracticeExamNotFoundException)
        {
            NotFound = true;
        }
        catch (HttpRequestException)
        {
            NotFound = true;
            ErrorMessage = "StudyHub.Api could not be reached. Try again in a moment.";
        }
    }

    private void ShowSheet(PracticeExamSheetDto sheet)
    {
        Sheet = sheet;
        Review = null;
        Selections.Clear();
        Texts.Clear();
        _dirtySince.Clear();

        foreach (var task in sheet.Tasks)
        {
            Selections[task.TaskId] = task.SelectedOptionId;
            Texts[task.TaskId] = task.AnswerText;
        }
    }

    private async Task LoadReviewAsync()
    {
        Review = await AttemptAccessor.GetReviewAsync(AttemptId);
        Sheet = null;
        _dirtySince.Clear();
    }

    private async Task RunTimerAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                await InvokeAsync(TickAsync);
            }
        }
        catch (OperationCanceledException) { }
    }

    private async Task TickAsync()
    {
        if (Sheet is null || IsSubmitting)
        {
            return;
        }

        if (Remaining <= TimeSpan.Zero)
        {
            await SubmitAsync();
            return;
        }

        var due = _dirtySince
            .Where(entry => DateTime.UtcNow - entry.Value >= TypingPause)
            .Select(entry => entry.Key)
            .ToList();
        foreach (var taskId in due)
        {
            await SaveAsync(taskId);
        }

        StateHasChanged();
    }

    private async Task SelectOptionAsync(Guid taskId, Guid optionId)
    {
        Selections[taskId] = optionId;
        await SaveAsync(taskId);
    }

    private void ChangeText(Guid taskId, string text)
    {
        Texts[taskId] = text;
        _dirtySince.TryAdd(taskId, DateTime.UtcNow);
        SaveStatus = "Unsaved changes";
    }

    private Task SaveIfDirtyAsync(Guid taskId) =>
        _dirtySince.ContainsKey(taskId) ? SaveAsync(taskId) : Task.CompletedTask;

    private async Task SaveAsync(Guid taskId)
    {
        if (Sheet is null)
        {
            return;
        }

        if (!_saving.Add(taskId))
        {
            // A save of this task is still running; the next tick saves the newer answer.
            _dirtySince.TryAdd(taskId, DateTime.MinValue);
            return;
        }

        _dirtySince.Remove(taskId);
        SaveStatus = "Saving…";

        try
        {
            await AttemptAccessor.SaveAnswerAsync(
                AttemptId,
                taskId,
                new SavePracticeExamAnswerRequest(
                    Selections.GetValueOrDefault(taskId),
                    Texts.GetValueOrDefault(taskId)
                )
            );
            SaveStatus = _dirtySince.Count == 0 ? "Saved" : "Unsaved changes";
        }
        catch (PracticeExamTimeOverException ex)
        {
            SaveStatus = null;
            ErrorMessage = ex.Message;
        }
        catch (PracticeExamAttemptSubmittedException)
        {
            await LoadReviewAsync();
        }
        catch (Exception ex)
            when (ex
                    is HttpRequestException
                        or TaskCanceledException
                        or PracticeExamValidationException
            )
        {
            // Kept dirty, so the next tick tries again.
            _dirtySince.TryAdd(taskId, DateTime.UtcNow);
            SaveStatus = "Not saved — retrying";
            ErrorMessage = ex is PracticeExamValidationException ? ex.Message : null;
        }
        finally
        {
            _saving.Remove(taskId);
        }
    }

    private async Task SubmitAsync()
    {
        if (IsSubmitting)
        {
            return;
        }

        IsSubmitting = true;
        IsConfirmOpen = false;
        ErrorMessage = null;

        try
        {
            foreach (var taskId in _dirtySince.Keys.ToList())
            {
                await SaveAsync(taskId);
            }

            Review = await AttemptAccessor.SubmitAsync(AttemptId);
            Sheet = null;
            _dirtySince.Clear();
        }
        catch (HttpRequestException)
        {
            ErrorMessage =
                "StudyHub.Api could not be reached. Your answers are kept — try again in a moment.";
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    private Task GradeAsync(Guid taskId, IReadOnlyList<Guid> metCriterionIds) =>
        RunAsync(async () =>
            Review = await AttemptAccessor.GradeAsync(
                AttemptId,
                taskId,
                new GradePracticeExamAnswerRequest(metCriterionIds)
            )
        );

    private Task ExcludeAsync(Guid taskId) =>
        RunAsync(async () =>
        {
            await PracticeExamAccessor.ExcludeTaskAsync(taskId);
            Review = await AttemptAccessor.GetReviewAsync(AttemptId);
        });

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
                    is PracticeExamValidationException
                        or PracticeExamArchivedException
                        or PracticeExamTaskNotFoundException
                        or PracticeExamAttemptNotSubmittedException
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
