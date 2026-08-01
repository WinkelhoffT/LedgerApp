namespace StudyHub.Logic.Business.Semesters;

public sealed class SemesterNotFoundException(Guid semesterId) : Exception($"Semester '{semesterId}' was not found.")
{
    public Guid SemesterId { get; } = semesterId;
}
