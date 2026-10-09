using StudyHub.Logic.Domain.Contract;
using StudyHub.Shared.PracticeExams;

namespace StudyHub.Logic.Domain;

public sealed class PracticeExamValidator : IPracticeExamValidator
{
    public void ValidateGenerationOptions(
        PracticeExamLevel level,
        int durationMinutes,
        string? focusHint
    )
    {
        if (!Enum.IsDefined(level))
        {
            throw new PracticeExamValidationException("The level is not supported.");
        }

        if (!GeneratePracticeExamRequest.AllowedDurations.Contains(durationMinutes))
        {
            throw new PracticeExamValidationException(
                $"Duration must be one of {string.Join(", ", GeneratePracticeExamRequest.AllowedDurations)} minutes."
            );
        }

        if (focusHint is { Length: > GeneratePracticeExamRequest.FocusHintMaxLength })
        {
            throw new PracticeExamValidationException(
                $"Focus hint must not exceed {GeneratePracticeExamRequest.FocusHintMaxLength} characters."
            );
        }
    }

    public IReadOnlyList<GeneratedPracticeExamTask> FilterGeneratedTasks(
        IReadOnlyList<GeneratedPracticeExamTask> tasks,
        IReadOnlyCollection<int> sourceIds
    ) =>
        tasks
            .Select(task => Normalize(task, sourceIds))
            .OfType<GeneratedPracticeExamTask>()
            .Take(PracticeExam.MaxTaskCount)
            .ToList();

    private static GeneratedPracticeExamTask? Normalize(
        GeneratedPracticeExamTask? task,
        IReadOnlyCollection<int> sourceIds
    )
    {
        if (task?.Kind is not { } kind)
        {
            return null;
        }

        var text = task.Text?.Trim() ?? string.Empty;
        var solution = task.Solution?.Trim() ?? string.Empty;
        if (
            !IsWithin(text, PracticeExamTask.TextMaxLength)
            || !IsWithin(solution, PracticeExamTask.SolutionMaxLength)
        )
        {
            return null;
        }

        var sourceId = task.SourceId is { } id && sourceIds.Contains(id) ? id : (int?)null;

        return kind switch
        {
            PracticeExamTaskKind.SingleChoice => NormalizeSingleChoice(
                task,
                text,
                solution,
                sourceId
            ),
            PracticeExamTaskKind.Open => NormalizeOpen(task, text, solution, sourceId),
            _ => null,
        };
    }

    private static GeneratedPracticeExamTask? NormalizeSingleChoice(
        GeneratedPracticeExamTask task,
        string text,
        string solution,
        int? sourceId
    )
    {
        if (
            task.Points
            is < PracticeExamTask.MinSingleChoicePoints
                or > PracticeExamTask.MaxSingleChoicePoints
        )
        {
            return null;
        }

        var options = (task.Options ?? [])
            .Select(option => new GeneratedPracticeExamOption(
                option?.Text?.Trim() ?? string.Empty,
                option?.IsCorrect ?? false,
                option?.Rationale?.Trim() ?? string.Empty
            ))
            .ToList();

        if (
            options.Count != PracticeExamTask.SingleChoiceOptionCount
            || options.Count(option => option.IsCorrect) != 1
            || options.Any(option =>
                !IsWithin(option.Text, PracticeExamOption.TextMaxLength)
                || !IsWithin(option.Rationale, PracticeExamOption.RationaleMaxLength)
            )
            || options.DistinctBy(option => option.Text, StringComparer.OrdinalIgnoreCase).Count()
                != options.Count
        )
        {
            return null;
        }

        return new GeneratedPracticeExamTask(
            PracticeExamTaskKind.SingleChoice,
            text,
            task.Points,
            options,
            [],
            solution,
            sourceId
        );
    }

    private static GeneratedPracticeExamTask? NormalizeOpen(
        GeneratedPracticeExamTask task,
        string text,
        string solution,
        int? sourceId
    )
    {
        var criteria = (task.Criteria ?? [])
            .Select(criterion => new GeneratedPracticeExamCriterion(
                criterion?.Description?.Trim() ?? string.Empty,
                criterion?.Points ?? 0
            ))
            .ToList();

        if (
            criteria.Count is < PracticeExamTask.MinCriteria or > PracticeExamTask.MaxCriteria
            || criteria.Any(criterion =>
                !IsWithin(criterion.Description, PracticeExamCriterion.DescriptionMaxLength)
                || criterion.Points < 1
            )
        )
        {
            return null;
        }

        // The task's points always follow from its rubric, so the two cannot disagree.
        var points = criteria.Sum(criterion => criterion.Points);
        if (points > PracticeExamTask.MaxPoints)
        {
            return null;
        }

        return new GeneratedPracticeExamTask(
            PracticeExamTaskKind.Open,
            text,
            points,
            [],
            criteria,
            solution,
            sourceId
        );
    }

    private static bool IsWithin(string value, int maxLength) =>
        value.Length > 0 && value.Length <= maxLength;
}
