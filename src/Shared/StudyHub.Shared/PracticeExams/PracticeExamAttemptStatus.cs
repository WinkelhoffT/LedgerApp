namespace StudyHub.Shared.PracticeExams;

/// <summary>Derived from an attempt's timestamps; not stored on its own.</summary>
public enum PracticeExamAttemptStatus
{
    InProgress,

    /// <summary>Submitted, but at least one open task is not self-graded yet.</summary>
    Submitted,

    Graded,
}
