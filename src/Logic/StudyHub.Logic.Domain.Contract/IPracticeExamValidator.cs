using StudyHub.Shared.PracticeExams;

namespace StudyHub.Logic.Domain.Contract;

/// <summary>
/// Domain rules for practice exam generation: allowed options and what makes a generated task
/// usable. Returns normalized copies instead of mutating input.
/// </summary>
public interface IPracticeExamValidator
{
    /// <exception cref="PracticeExamValidationException">Level, duration or focus hint is not allowed.</exception>
    void ValidateGenerationOptions(PracticeExamLevel level, int durationMinutes, string? focusHint);

    /// <summary>
    /// Normalizes AI-generated tasks, drops the ones that break a task rule, and caps the result at
    /// <see cref="PracticeExam.MaxTaskCount"/>. AI output is untrusted, so an invalid task is skipped rather than
    /// failing the whole exam. An open task's points become the sum of its criteria; a
    /// <c>sourceId</c> that is not in <paramref name="sourceIds"/> is removed, the task kept.
    /// </summary>
    IReadOnlyList<GeneratedPracticeExamTask> FilterGeneratedTasks(
        IReadOnlyList<GeneratedPracticeExamTask> tasks,
        IReadOnlyCollection<int> sourceIds
    );
}
