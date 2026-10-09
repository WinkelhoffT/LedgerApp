namespace StudyHub.Shared.PracticeExams;

/// <summary>A practice exam together with its tasks, ordered by position.</summary>
public sealed record StoredPracticeExam(
    PracticeExam Exam,
    IReadOnlyList<StoredPracticeExamTask> Tasks
);
