using StudyHub.Shared.Courses;

namespace StudyHub.Logic.Domain.Contract;

/// <summary>
/// Domain rules for creating and changing a <see cref="Course"/>. Courses are immutable
/// records, so every operation returns a new instance instead of mutating the given one.
/// </summary>
public interface ICourseLifecycle
{
    Course Create(string name, string? description, string color, Guid semesterId);

    /// <exception cref="CourseArchivedException">The course is archived.</exception>
    Course Update(Course course, string name, string? description, string color, Guid semesterId);

    Course Archive(Course course);

    Course Restore(Course course);
}
