namespace StudyHub.Logic.Business.Courses;

public sealed record UpdateCourseRequest(Guid Id, string Name, string? Description, string Color);
