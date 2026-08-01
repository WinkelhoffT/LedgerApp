namespace StudyHub.Logic.Business.Courses;

public sealed class DuplicateCourseNameException(string name) : Exception($"A course named '{name}' already exists.")
{
    public string Name { get; } = name;
}
