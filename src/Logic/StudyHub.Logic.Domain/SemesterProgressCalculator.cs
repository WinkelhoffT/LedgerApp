using StudyHub.Logic.Domain.Contract;
using StudyHub.Shared.Semesters;

namespace StudyHub.Logic.Domain;

public sealed class SemesterProgressCalculator : ISemesterProgressCalculator
{
    public SemesterProgress Calculate(DateOnly startDate, DateOnly endDate, DateOnly today)
    {
        var totalDays = endDate.DayNumber - startDate.DayNumber + 1;
        var elapsedDays = Math.Clamp(today.DayNumber - startDate.DayNumber + 1, 0, totalDays);
        var remainingDays = totalDays - elapsedDays;
        var percentComplete = Math.Clamp(elapsedDays / (double)totalDays * 100, 0, 100);

        return new Shared.Semesters.SemesterProgress(totalDays, elapsedDays, remainingDays, percentComplete);
    }
}
