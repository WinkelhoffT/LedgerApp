namespace StudyHub.Logic.Domain.Courses;

public sealed class Course
{
    public const int NameMaxLength = 100;
    public const int DescriptionMaxLength = 1000;

    private Course()
    {
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public string Color { get; private set; } = string.Empty;

    public Guid SemesterId { get; private set; }

    public bool IsArchived { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public static Course Create(string name, string? description, string color, Guid semesterId)
    {
        var course = new Course
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
        };

        course.SetDetails(name, description, color, semesterId);

        return course;
    }

    public void Update(string name, string? description, string color, Guid semesterId)
    {
        if (IsArchived)
        {
            throw new CourseArchivedException(Id);
        }

        SetDetails(name, description, color, semesterId);
    }

    public void Archive()
    {
        if (IsArchived)
        {
            return;
        }

        IsArchived = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Restore()
    {
        if (!IsArchived)
        {
            return;
        }

        IsArchived = false;
        UpdatedAt = DateTime.UtcNow;
    }

    private void SetDetails(string name, string? description, string color, Guid semesterId)
    {
        var trimmedName = name?.Trim() ?? string.Empty;
        if (trimmedName.Length == 0)
        {
            throw new CourseValidationException("Course name is required.");
        }

        if (trimmedName.Length > NameMaxLength)
        {
            throw new CourseValidationException($"Course name must not exceed {NameMaxLength} characters.");
        }

        var trimmedDescription = description?.Trim();
        if (trimmedDescription is { Length: > DescriptionMaxLength })
        {
            throw new CourseValidationException($"Course description must not exceed {DescriptionMaxLength} characters.");
        }

        var trimmedColor = color?.Trim() ?? string.Empty;
        if (trimmedColor.Length == 0)
        {
            throw new CourseValidationException("Course color is required.");
        }

        if (semesterId == Guid.Empty)
        {
            throw new CourseValidationException("Course must be assigned to a semester.");
        }

        Name = trimmedName;
        Description = string.IsNullOrEmpty(trimmedDescription) ? null : trimmedDescription;
        Color = trimmedColor;
        SemesterId = semesterId;
        UpdatedAt = DateTime.UtcNow;
    }
}
