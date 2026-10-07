using StudyHub.Logic.Domain.Contract;
using StudyHub.Shared.Courses;

namespace StudyHub.Logic.Domain;

public sealed class CourseLifecycle : ICourseLifecycle
{
    public Course Create(string name, string? description, string color, Guid semesterId)
    {
        var now = DateTime.UtcNow;

        return new Course(
            Id: Guid.NewGuid(),
            Name: ValidateName(name),
            Description: ValidateDescription(description),
            Color: ValidateColor(color),
            SemesterId: ValidateSemesterId(semesterId),
            IsArchived: false,
            CreatedAt: now,
            UpdatedAt: now
        );
    }

    public Course Update(
        Course course,
        string name,
        string? description,
        string color,
        Guid semesterId
    )
    {
        if (course.IsArchived)
        {
            throw new CourseArchivedException(course.Id);
        }

        return course with
        {
            Name = ValidateName(name),
            Description = ValidateDescription(description),
            Color = ValidateColor(color),
            SemesterId = ValidateSemesterId(semesterId),
            UpdatedAt = DateTime.UtcNow,
        };
    }

    public Course Archive(Course course) =>
        course.IsArchived ? course : course with { IsArchived = true, UpdatedAt = DateTime.UtcNow };

    public Course Restore(Course course) =>
        !course.IsArchived
            ? course
            : course with
            {
                IsArchived = false,
                UpdatedAt = DateTime.UtcNow,
            };

    private static string ValidateName(string name)
    {
        var trimmedName = name?.Trim() ?? string.Empty;
        if (trimmedName.Length == 0)
        {
            throw new CourseValidationException("Course name is required.");
        }

        if (trimmedName.Length > CreateCourseRequest.NameMaxLength)
        {
            throw new CourseValidationException(
                $"Course name must not exceed {CreateCourseRequest.NameMaxLength} characters."
            );
        }

        return trimmedName;
    }

    private static string? ValidateDescription(string? description)
    {
        var trimmedDescription = description?.Trim();
        if (trimmedDescription is { Length: > CreateCourseRequest.DescriptionMaxLength })
        {
            throw new CourseValidationException(
                $"Course description must not exceed {CreateCourseRequest.DescriptionMaxLength} characters."
            );
        }

        return string.IsNullOrEmpty(trimmedDescription) ? null : trimmedDescription;
    }

    private static string ValidateColor(string color)
    {
        var trimmedColor = color?.Trim() ?? string.Empty;
        if (trimmedColor.Length == 0)
        {
            throw new CourseValidationException("Course color is required.");
        }

        return trimmedColor;
    }

    private static Guid ValidateSemesterId(Guid semesterId)
    {
        if (semesterId == Guid.Empty)
        {
            throw new CourseValidationException("Course must be assigned to a semester.");
        }

        return semesterId;
    }
}
