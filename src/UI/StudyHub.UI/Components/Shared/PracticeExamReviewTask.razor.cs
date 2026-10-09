using Microsoft.AspNetCore.Components;
using StudyHub.Shared.PracticeExams;

namespace StudyHub.UI.Components.Shared;

/// <summary>
/// One task after submission: the solution, and for an answered open task the rubric to grade it
/// with. The ticked criteria stay local until the student saves the grading.
/// </summary>
public partial class PracticeExamReviewTask
{
    [Parameter, EditorRequired]
    public PracticeExamReviewTaskDto ExamTask { get; set; } = default!;

    [Parameter]
    public bool IsBusy { get; set; }

    [Parameter]
    public EventCallback<IReadOnlyList<Guid>> OnGrade { get; set; }

    [Parameter]
    public EventCallback OnExclude { get; set; }

    private HashSet<Guid> MetCriterionIds { get; set; } = [];

    private PracticeExamReviewTaskDto? _shownTask;

    private bool IsAnswered => !string.IsNullOrWhiteSpace(ExamTask.AnswerText);

    private int TickedPoints =>
        ExamTask.Criteria.Where(c => MetCriterionIds.Contains(c.Id)).Sum(c => c.Points);

    private bool HasChanges =>
        ExamTask.AwardedPoints is null
        || !MetCriterionIds.SetEquals(ExamTask.Criteria.Where(c => c.IsMet).Select(c => c.Id));

    private string ResultText =>
        ExamTask.AwardedPoints is { } points ? $"{points} / {ExamTask.Points}"
        : IsAnswered ? "grade below"
        : string.Empty;

    private string? ResultClass =>
        ExamTask.AwardedPoints switch
        {
            null => "pending",
            var points when points == ExamTask.Points => "full",
            0 => "none",
            _ => "partial",
        };

    protected override void OnParametersSet()
    {
        if (!ReferenceEquals(_shownTask, ExamTask))
        {
            _shownTask = ExamTask;
            MetCriterionIds = ExamTask.Criteria.Where(c => c.IsMet).Select(c => c.Id).ToHashSet();
        }
    }

    private void ToggleCriterion(Guid criterionId, bool isMet)
    {
        if (isMet)
        {
            MetCriterionIds.Add(criterionId);
        }
        else
        {
            MetCriterionIds.Remove(criterionId);
        }
    }

    private Task SaveGradingAsync() => OnGrade.InvokeAsync(MetCriterionIds.ToList());
}
