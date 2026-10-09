using StudyHub.Shared.PracticeExams;

namespace StudyHub.Logic.Domain.Contract;

/// <param name="IsNew"><c>false</c> when the exam's open attempt was resumed.</param>
public sealed record PracticeExamAttemptStartOutcome(StoredPracticeExamAttempt Attempt, bool IsNew);
