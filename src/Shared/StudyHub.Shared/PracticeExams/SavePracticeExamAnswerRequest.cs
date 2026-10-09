namespace StudyHub.Shared.PracticeExams;

/// <param name="SelectedOptionId">For a single-choice task; <c>null</c> clears the selection.</param>
/// <param name="AnswerText">For an open task; saved as typed.</param>
public sealed record SavePracticeExamAnswerRequest(Guid? SelectedOptionId, string? AnswerText);
