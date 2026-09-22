namespace StudyHub.Logic.Domain.Contract.SemesterProgress;

public interface ISemesterProgressCalculator
{
    SemesterProgress Calculate(DateOnly startDate, DateOnly endDate, DateOnly today);
}
