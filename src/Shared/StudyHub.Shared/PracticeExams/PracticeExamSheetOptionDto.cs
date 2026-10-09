namespace StudyHub.Shared.PracticeExams;

/// <summary>A single-choice option on the exam sheet, without its correctness.</summary>
public sealed record PracticeExamSheetOptionDto(Guid Id, string Text);
