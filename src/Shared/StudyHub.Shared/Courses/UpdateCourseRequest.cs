namespace StudyHub.Shared.Courses;

public sealed record UpdateCourseRequest(Guid Id, string Name, string? Description, string Color, Guid SemesterId);
