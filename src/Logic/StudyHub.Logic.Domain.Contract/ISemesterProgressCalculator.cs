namespace StudyHub.Logic.Domain.Contract;

public interface ISemesterProgressCalculator
{
    Shared.Semesters.SemesterProgress Calculate(
        DateOnly startDate,
        DateOnly endDate,
        DateOnly today
    );
}
