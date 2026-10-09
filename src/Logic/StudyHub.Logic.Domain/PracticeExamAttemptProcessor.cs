using StudyHub.Logic.Domain.Contract;
using StudyHub.Shared.PracticeExams;

namespace StudyHub.Logic.Domain;

public sealed class PracticeExamAttemptProcessor(TimeProvider timeProvider)
    : IPracticeExamAttemptProcessor
{
    /// <summary>
    /// How long after the time limit answers are still accepted, so the last autosave before the
    /// automatic submission is not lost to network latency.
    /// </summary>
    public static readonly TimeSpan SaveGracePeriod = TimeSpan.FromMinutes(1);

    public PracticeExamAttemptStartOutcome Start(
        StoredPracticeExam exam,
        StoredPracticeExamAttempt? openAttempt,
        bool withTimeLimit
    )
    {
        if (openAttempt is not null)
        {
            return new PracticeExamAttemptStartOutcome(openAttempt, IsNew: false);
        }

        if (exam.Exam.IsArchived)
        {
            throw new PracticeExamArchivedException(exam.Exam.Id);
        }

        var tasks = exam.Tasks.Select(t => t.Task).Where(t => !t.IsExcluded).ToList();
        if (tasks.Count == 0)
        {
            throw new PracticeExamValidationException(
                "Every task of this practice exam is excluded."
            );
        }

        var now = Now();
        var attempt = new PracticeExamAttempt(
            Id: Guid.CreateVersion7(),
            ExamId: exam.Exam.Id,
            StartedAt: now,
            DueAt: withTimeLimit ? now.AddMinutes(exam.Exam.DurationMinutes) : null,
            SubmittedAt: null,
            GradedAt: null,
            MaxPoints: tasks.Sum(t => t.Points),
            AwardedPoints: null
        );
        var answers = tasks
            .Select(task => new PracticeExamAnswer(
                Guid.CreateVersion7(),
                attempt.Id,
                task.Id,
                SelectedOptionId: null,
                AnswerText: null,
                AwardedPoints: null,
                UpdatedAt: now
            ))
            .ToList();

        return new PracticeExamAttemptStartOutcome(
            new StoredPracticeExamAttempt(attempt, answers, []),
            IsNew: true
        );
    }

    public PracticeExamAnswer SaveAnswer(
        StoredPracticeExam exam,
        StoredPracticeExamAttempt attempt,
        Guid taskId,
        Guid? selectedOptionId,
        string? answerText
    )
    {
        EnsureInProgress(attempt.Attempt);

        var now = Now();
        if (attempt.Attempt.DueAt is { } dueAt && now > dueAt + SaveGracePeriod)
        {
            throw new PracticeExamTimeOverException(attempt.Attempt.Id);
        }

        var answer = GetAnswer(attempt, taskId);
        var task = GetTask(exam, taskId);
        var text = string.IsNullOrWhiteSpace(answerText) ? null : answerText;

        if (task.Task.Kind == PracticeExamTaskKind.SingleChoice)
        {
            if (text is not null)
            {
                throw new PracticeExamValidationException(
                    "A single-choice task is answered by choosing an option."
                );
            }

            if (selectedOptionId is { } optionId && task.Options.All(o => o.Id != optionId))
            {
                throw new PracticeExamValidationException(
                    "The selected option does not belong to this task."
                );
            }
        }
        else
        {
            if (selectedOptionId is not null)
            {
                throw new PracticeExamValidationException(
                    "An open task is answered with text, not an option."
                );
            }

            if (text is { Length: > PracticeExamAnswer.AnswerTextMaxLength })
            {
                throw new PracticeExamValidationException(
                    $"An answer must not exceed {PracticeExamAnswer.AnswerTextMaxLength} characters."
                );
            }
        }

        return answer with
        {
            SelectedOptionId = selectedOptionId,
            AnswerText = text,
            UpdatedAt = now,
        };
    }

    public StoredPracticeExamAttempt Submit(
        StoredPracticeExam exam,
        StoredPracticeExamAttempt attempt
    )
    {
        if (attempt.Attempt.SubmittedAt is not null)
        {
            return attempt;
        }

        var now = Now();
        var answers = attempt
            .Answers.Select(answer =>
                answer with
                {
                    AwardedPoints = GradeOnSubmit(GetTask(exam, answer.TaskId), answer),
                    UpdatedAt = now,
                }
            )
            .ToList();

        return Complete(
            attempt with
            {
                Attempt = attempt.Attempt with { SubmittedAt = now },
                Answers = answers,
            },
            now
        );
    }

    public StoredPracticeExamAttempt Grade(
        StoredPracticeExam exam,
        StoredPracticeExamAttempt attempt,
        Guid taskId,
        IReadOnlyCollection<Guid> metCriterionIds
    )
    {
        EnsureSubmitted(attempt.Attempt);

        var answer = GetAnswer(attempt, taskId);
        var task = GetTask(exam, taskId);
        if (task.Task.Kind != PracticeExamTaskKind.Open)
        {
            throw new PracticeExamValidationException(
                "Single-choice tasks are graded automatically."
            );
        }

        if (string.IsNullOrWhiteSpace(answer.AnswerText))
        {
            throw new PracticeExamValidationException(
                "An unanswered task gets 0 points and is not graded."
            );
        }

        var criterionIds = metCriterionIds.Distinct().ToList();
        var criteria = criterionIds
            .Select(id =>
                task.Criteria.FirstOrDefault(c => c.Id == id)
                ?? throw new PracticeExamValidationException(
                    "A ticked criterion does not belong to this task."
                )
            )
            .ToList();

        var now = Now();
        var graded = answer with { AwardedPoints = criteria.Sum(c => c.Points), UpdatedAt = now };

        return Complete(
            attempt with
            {
                Answers = attempt.Answers.Select(a => a.Id == answer.Id ? graded : a).ToList(),
                MetCriteria =
                [
                    .. attempt.MetCriteria.Where(c => c.AnswerId != answer.Id),
                    .. criterionIds.Select(id => new PracticeExamAnswerCriterion(answer.Id, id)),
                ],
            },
            now
        );
    }

    public void EnsureInProgress(PracticeExamAttempt attempt)
    {
        if (attempt.SubmittedAt is not null)
        {
            throw new PracticeExamAttemptSubmittedException(attempt.Id);
        }
    }

    public void EnsureSubmitted(PracticeExamAttempt attempt)
    {
        if (attempt.SubmittedAt is null)
        {
            throw new PracticeExamAttemptNotSubmittedException(attempt.Id);
        }
    }

    public PracticeExamAttemptStatus GetStatus(PracticeExamAttempt attempt) =>
        attempt switch
        {
            { GradedAt: not null } => PracticeExamAttemptStatus.Graded,
            { SubmittedAt: not null } => PracticeExamAttemptStatus.Submitted,
            _ => PracticeExamAttemptStatus.InProgress,
        };

    public int? GetPercent(PracticeExamAttempt attempt) =>
        attempt.AwardedPoints is not { } awarded ? null
        : attempt.MaxPoints == 0 ? 0
        : (int)Math.Round(100.0 * awarded / attempt.MaxPoints, MidpointRounding.AwayFromZero);

    public PracticeExamAttempt? SelectBest(IEnumerable<PracticeExamAttempt> attempts) =>
        attempts
            .Where(a => a.AwardedPoints is not null && a.MaxPoints > 0)
            .OrderByDescending(a => (double)a.AwardedPoints!.Value / a.MaxPoints)
            .ThenByDescending(a => a.StartedAt)
            .FirstOrDefault();

    private static int? GradeOnSubmit(StoredPracticeExamTask task, PracticeExamAnswer answer) =>
        task.Task.Kind switch
        {
            PracticeExamTaskKind.SingleChoice => task.Options.Any(o =>
                o.Id == answer.SelectedOptionId && o.IsCorrect
            )
                ? task.Task.Points
                : 0,
            _ => string.IsNullOrWhiteSpace(answer.AnswerText) ? 0 : null,
        };

    // Once every task has points the attempt counts as graded; a later change keeps the first
    // grading time but updates the total.
    private static StoredPracticeExamAttempt Complete(
        StoredPracticeExamAttempt attempt,
        DateTime now
    )
    {
        if (attempt.Answers.Any(a => a.AwardedPoints is null))
        {
            return attempt;
        }

        return attempt with
        {
            Attempt = attempt.Attempt with
            {
                GradedAt = attempt.Attempt.GradedAt ?? now,
                AwardedPoints = attempt.Answers.Sum(a => a.AwardedPoints!.Value),
            },
        };
    }

    private static PracticeExamAnswer GetAnswer(StoredPracticeExamAttempt attempt, Guid taskId) =>
        attempt.Answers.FirstOrDefault(a => a.TaskId == taskId)
        ?? throw new PracticeExamTaskNotFoundException(taskId);

    private static StoredPracticeExamTask GetTask(StoredPracticeExam exam, Guid taskId) =>
        exam.Tasks.FirstOrDefault(t => t.Task.Id == taskId)
        ?? throw new PracticeExamTaskNotFoundException(taskId);

    private DateTime Now() => timeProvider.GetUtcNow().UtcDateTime;
}
