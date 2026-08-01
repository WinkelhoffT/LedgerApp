namespace StudyHub.Logic.Domain.Courses;

public sealed class CourseArchivedException(Guid courseId)
    : Exception($"Course '{courseId}' is archived and cannot be edited until it is restored.")
{
    public Guid CourseId { get; } = courseId;
}
