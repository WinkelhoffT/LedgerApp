namespace StudyHub.Shared.PracticeExams;

/// <param name="IsMet">Ticked by the student while self-grading.</param>
public sealed record PracticeExamReviewCriterionDto(
    Guid Id,
    string Description,
    int Points,
    bool IsMet
);
