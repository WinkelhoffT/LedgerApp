namespace StudyHub.Shared.Semesters;

public sealed class DuplicateSemesterNameException(string name) : Exception($"A semester named '{name}' already exists.")
{
    public string Name { get; } = name;
}
