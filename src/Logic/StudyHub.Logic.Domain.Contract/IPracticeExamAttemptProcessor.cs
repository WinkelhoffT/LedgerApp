using StudyHub.Shared.PracticeExams;

namespace StudyHub.Logic.Domain.Contract;

/// <summary>
/// Rules for writing a practice exam: starting, saving answers within the time limit, submitting
/// with automatic single-choice grading, and self-grading open answers against the rubric.
/// </summary>
public interface IPracticeExamAttemptProcessor
{
    /// <summary>Resumes <paramref name="openAttempt"/> if there is one, otherwise starts a new attempt over the tasks that are not excluded.</summary>
    /// <exception cref="PracticeExamArchivedException">A new attempt would start on an archived exam.</exception>
    /// <exception cref="PracticeExamValidationException">Every task is excluded.</exception>
    PracticeExamAttemptStartOutcome Start(
        StoredPracticeExam exam,
        StoredPracticeExamAttempt? openAttempt,
        bool withTimeLimit
    );

    /// <returns>The updated answer.</returns>
    /// <exception cref="PracticeExamAttemptSubmittedException">The attempt was submitted.</exception>
    /// <exception cref="PracticeExamTimeOverException">The time limit (plus a short grace period) is over.</exception>
    /// <exception cref="PracticeExamTaskNotFoundException">The task is not part of the attempt.</exception>
    /// <exception cref="PracticeExamValidationException">The answer does not fit the task.</exception>
    PracticeExamAnswer SaveAnswer(
        StoredPracticeExam exam,
        StoredPracticeExamAttempt attempt,
        Guid taskId,
        Guid? selectedOptionId,
        string? answerText
    );

    /// <summary>
    /// Freezes the answers, grades single-choice tasks and gives unanswered open tasks 0 points.
    /// Submitting again changes nothing.
    /// </summary>
    StoredPracticeExamAttempt Submit(StoredPracticeExam exam, StoredPracticeExamAttempt attempt);

    /// <summary>
    /// Grades an answered open task with the criteria its answer meets. The attempt is graded once
    /// every task has points; a grading can still be changed afterwards.
    /// </summary>
    /// <exception cref="PracticeExamAttemptNotSubmittedException">The attempt was not submitted yet.</exception>
    /// <exception cref="PracticeExamTaskNotFoundException">The task is not part of the attempt.</exception>
    /// <exception cref="PracticeExamValidationException">Not an answered open task, or a criterion of another task.</exception>
    StoredPracticeExamAttempt Grade(
        StoredPracticeExam exam,
        StoredPracticeExamAttempt attempt,
        Guid taskId,
        IReadOnlyCollection<Guid> metCriterionIds
    );

    /// <summary>The exam sheet is only shown while the attempt is in progress.</summary>
    /// <exception cref="PracticeExamAttemptSubmittedException">The attempt was submitted.</exception>
    void EnsureInProgress(PracticeExamAttempt attempt);

    /// <summary>Solutions, rationales and rubrics are only shown after submission.</summary>
    /// <exception cref="PracticeExamAttemptNotSubmittedException">The attempt was not submitted yet.</exception>
    void EnsureSubmitted(PracticeExamAttempt attempt);

    PracticeExamAttemptStatus GetStatus(PracticeExamAttempt attempt);

    /// <summary>Rounded percentage of the maximum points; <c>null</c> until the attempt is graded.</summary>
    int? GetPercent(PracticeExamAttempt attempt);

    /// <summary>The graded attempt with the highest percentage; the latest one on a tie.</summary>
    PracticeExamAttempt? SelectBest(IEnumerable<PracticeExamAttempt> attempts);
}
