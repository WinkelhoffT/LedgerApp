namespace StudyHub.Logic.Business.Semesters;

public sealed record SemesterDto(
    Guid Id,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsArchived,
    DateTime CreatedAt,
    DateTime UpdatedAt);
