namespace StudyHub.Shared.PracticeExams;

/// <param name="WithTimeLimit">Limits the attempt to the exam's duration. Ignored when an open attempt is resumed.</param>
public sealed record StartPracticeExamAttemptRequest(bool WithTimeLimit);
