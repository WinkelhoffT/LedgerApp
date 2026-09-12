namespace StudyHub.Shared.Courses;

public sealed record CourseDto(
    Guid Id,
    string Name,
    string? Description,
    string Color,
    Guid SemesterId,
    bool IsArchived,
    DateTime CreatedAt,
    DateTime UpdatedAt);
