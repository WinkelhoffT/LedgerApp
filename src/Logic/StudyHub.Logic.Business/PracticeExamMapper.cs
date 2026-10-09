using StudyHub.Shared.Flashcards;
using StudyHub.Shared.Notes;
using StudyHub.Shared.PracticeExams;

namespace StudyHub.Logic.Business;

/// <summary>Maps stored practice exams and attempts to the DTOs the practice exam orchestrators return.</summary>
internal static class PracticeExamMapper
{
    public static PracticeExamDto ToDto(
        PracticeExam exam,
        string sourceName,
        PracticeExamTaskTotals? totals,
        int attemptCount,
        PracticeExamAttemptSummaryDto? bestAttempt,
        Guid? openAttemptId
    ) =>
        new(
            exam.Id,
            exam.Title,
            exam.Level,
            exam.DurationMinutes,
            exam.SourceKind,
            exam.CourseId,
            exam.DeckId,
            sourceName,
            exam.Model,
            exam.FocusHint,
            totals?.TaskCount ?? 0,
            totals?.TotalPoints ?? 0,
            totals?.ExcludedTaskCount ?? 0,
            attemptCount,
            bestAttempt,
            openAttemptId,
            exam.IsArchived,
            exam.CreatedAt
        );

    public static PracticeExamTaskTotals ToTotals(StoredPracticeExam exam)
    {
        var included = exam.Tasks.Where(t => !t.Task.IsExcluded).ToList();
        return new PracticeExamTaskTotals(
            exam.Exam.Id,
            included.Count,
            included.Sum(t => t.Task.Points),
            exam.Tasks.Count - included.Count
        );
    }

    public static PracticeExamAttemptSummaryDto ToSummaryDto(
        PracticeExamAttempt attempt,
        PracticeExamAttemptStatus status,
        int? percent
    ) =>
        new(
            attempt.Id,
            attempt.ExamId,
            status,
            attempt.StartedAt,
            attempt.DueAt,
            attempt.SubmittedAt,
            attempt.GradedAt,
            attempt.MaxPoints,
            attempt.AwardedPoints,
            percent
        );

    public static PracticeExamSheetDto ToSheetDto(
        StoredPracticeExam exam,
        StoredPracticeExamAttempt attempt
    ) =>
        new(
            attempt.Attempt.Id,
            exam.Exam.Id,
            exam.Exam.Title,
            exam.Exam.Level,
            exam.Exam.DurationMinutes,
            attempt.Attempt.StartedAt,
            attempt.Attempt.DueAt,
            attempt.Attempt.MaxPoints,
            GetAnsweredTasks(exam, attempt)
                .Select(
                    (pair, index) =>
                        new PracticeExamSheetTaskDto(
                            pair.Task.Task.Id,
                            index + 1,
                            pair.Task.Task.Kind,
                            pair.Task.Task.Text,
                            pair.Task.Task.Points,
                            pair.Task.Options.Select(o => new PracticeExamSheetOptionDto(
                                    o.Id,
                                    o.Text
                                ))
                                .ToList(),
                            pair.Answer.SelectedOptionId,
                            pair.Answer.AnswerText
                        )
                )
                .ToList()
        );

    public static PracticeExamReviewDto ToReviewDto(
        StoredPracticeExam exam,
        StoredPracticeExamAttempt attempt,
        PracticeExamAttemptStatus status,
        int? percent,
        IReadOnlyDictionary<Guid, Note> sourceNotes,
        IReadOnlyDictionary<Guid, Flashcard> sourceCards
    )
    {
        var tasks = GetAnsweredTasks(exam, attempt);
        var selfGraded = tasks
            .Where(pair =>
                pair.Task.Task.Kind == PracticeExamTaskKind.Open
                && !string.IsNullOrWhiteSpace(pair.Answer.AnswerText)
            )
            .ToList();
        var metCriterionIds = attempt.MetCriteria.Select(c => c.CriterionId).ToHashSet();

        return new PracticeExamReviewDto(
            attempt.Attempt.Id,
            exam.Exam.Id,
            exam.Exam.Title,
            exam.Exam.Level,
            status,
            attempt.Attempt.StartedAt,
            attempt.Attempt.SubmittedAt,
            attempt.Attempt.GradedAt,
            attempt.Attempt.MaxPoints,
            attempt.Attempt.AwardedPoints,
            percent,
            selfGraded.Count,
            selfGraded.Count(pair => pair.Answer.AwardedPoints is not null),
            tasks
                .Select(
                    (pair, index) =>
                        new PracticeExamReviewTaskDto(
                            pair.Task.Task.Id,
                            index + 1,
                            pair.Task.Task.Kind,
                            pair.Task.Task.Text,
                            pair.Task.Task.Points,
                            pair.Task.Task.Solution,
                            pair.Task.Options.Select(o => new PracticeExamReviewOptionDto(
                                    o.Id,
                                    o.Text,
                                    o.IsCorrect,
                                    o.Rationale
                                ))
                                .ToList(),
                            pair.Answer.SelectedOptionId,
                            pair.Task.Criteria.Select(c => new PracticeExamReviewCriterionDto(
                                    c.Id,
                                    c.Description,
                                    c.Points,
                                    metCriterionIds.Contains(c.Id)
                                ))
                                .ToList(),
                            pair.Answer.AnswerText,
                            pair.Answer.AwardedPoints,
                            ToSourceReference(pair.Task.Task, sourceNotes, sourceCards),
                            pair.Task.Task.IsExcluded
                        )
                )
                .ToList()
        );
    }

    private static PracticeExamSourceReferenceDto? ToSourceReference(
        PracticeExamTask task,
        IReadOnlyDictionary<Guid, Note> sourceNotes,
        IReadOnlyDictionary<Guid, Flashcard> sourceCards
    )
    {
        if (task.SourceNoteId is { } noteId && sourceNotes.TryGetValue(noteId, out var note))
        {
            return new PracticeExamSourceReferenceDto(note.Id, null, null, note.Title);
        }

        if (task.SourceFlashcardId is { } cardId && sourceCards.TryGetValue(cardId, out var card))
        {
            return new PracticeExamSourceReferenceDto(null, card.Id, card.DeckId, card.Front);
        }

        return null;
    }

    // The attempt's answer rows fix its task set; they are shown in the exam's task order.
    private static List<(StoredPracticeExamTask Task, PracticeExamAnswer Answer)> GetAnsweredTasks(
        StoredPracticeExam exam,
        StoredPracticeExamAttempt attempt
    )
    {
        var answersByTaskId = attempt.Answers.ToDictionary(a => a.TaskId);
        return exam
            .Tasks.Where(t => answersByTaskId.ContainsKey(t.Task.Id))
            .Select(t => (t, answersByTaskId[t.Task.Id]))
            .ToList();
    }
}
