namespace StudyHub.Shared.Courses;

public sealed record CreateCourseRequest(string Name, string? Description, string Color, Guid SemesterId)
{
    public const int NameMaxLength = 100;

    public const int DescriptionMaxLength = 1000;
}
