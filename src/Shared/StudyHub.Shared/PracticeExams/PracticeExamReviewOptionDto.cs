namespace StudyHub.Shared.PracticeExams;

public sealed record PracticeExamReviewOptionDto(
    Guid Id,
    string Text,
    bool IsCorrect,
    string Rationale
);
