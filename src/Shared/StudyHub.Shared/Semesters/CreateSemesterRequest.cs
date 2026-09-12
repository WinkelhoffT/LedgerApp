namespace StudyHub.Shared.Semesters;

public sealed record CreateSemesterRequest(string Name, DateOnly StartDate, DateOnly EndDate)
{
    public const int NameMaxLength = 100;
}
