namespace StudyHub.Shared.Semesters;

public sealed class SemesterArchivedException(Guid semesterId)
    : Exception($"Semester '{semesterId}' is archived and cannot be edited until it is restored.")
{
    public Guid SemesterId { get; } = semesterId;
}
