namespace StudyHub.Shared.Semesters;

public sealed record Semester(
    Guid Id,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsArchived,
    DateTime CreatedAt,
    DateTime UpdatedAt);
