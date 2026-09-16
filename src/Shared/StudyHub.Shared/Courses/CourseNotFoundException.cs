namespace StudyHub.Shared.Courses;

public sealed class CourseNotFoundException(Guid courseId) : Exception($"Course '{courseId}' was not found.")
{
    public Guid CourseId { get; } = courseId;
}
