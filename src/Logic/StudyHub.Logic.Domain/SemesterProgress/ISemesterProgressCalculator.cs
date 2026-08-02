namespace StudyHub.Logic.Domain.SemesterProgress;

public interface ISemesterProgressCalculator
{
    SemesterProgress Calculate(DateOnly startDate, DateOnly endDate, DateOnly today);
}
