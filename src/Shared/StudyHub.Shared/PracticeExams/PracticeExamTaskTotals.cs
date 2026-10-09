namespace StudyHub.Shared.PracticeExams;

/// <summary>Number and points of the tasks of one exam that are not excluded.</summary>
public sealed record PracticeExamTaskTotals(Guid ExamId, int TaskCount, int TotalPoints);
