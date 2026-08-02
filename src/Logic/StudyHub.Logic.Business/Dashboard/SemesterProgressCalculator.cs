using StudyHub.Logic.Business.Semesters;

namespace StudyHub.Logic.Business.Dashboard;

public static class SemesterProgressCalculator
{
    public static SemesterProgressDto Calculate(SemesterDto semester, DateOnly today)
    {
        var totalDays = semester.EndDate.DayNumber - semester.StartDate.DayNumber + 1;
        var elapsedDays = Math.Clamp(today.DayNumber - semester.StartDate.DayNumber + 1, 0, totalDays);
        var remainingDays = totalDays - elapsedDays;
        var percentComplete = Math.Clamp(elapsedDays / (double)totalDays * 100, 0, 100);

        return new SemesterProgressDto(
            HasActiveSemester: true,
            SemesterId: semester.Id,
            SemesterName: semester.Name,
            StartDate: semester.StartDate,
            EndDate: semester.EndDate,
            TotalDays: totalDays,
            ElapsedDays: elapsedDays,
            RemainingDays: remainingDays,
            PercentComplete: percentComplete);
    }
}
