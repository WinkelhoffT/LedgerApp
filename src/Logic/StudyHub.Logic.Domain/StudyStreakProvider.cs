using StudyHub.Logic.Domain.Contract;

namespace StudyHub.Logic.Domain;

public sealed class StudyStreakProvider : IStudyStreakProvider
{
    public StudyStreak GetStreak(IEnumerable<DateOnly> studyDays, DateOnly today)
    {
        var days = studyDays.Where(d => d <= today).ToHashSet();

        var current = 0;
        for (
            var day = days.Contains(today) ? today : today.AddDays(-1);
            days.Contains(day);
            day = day.AddDays(-1)
        )
        {
            current++;
        }

        var longest = 0;
        var run = 0;
        DateOnly? previous = null;
        foreach (var day in days.Order())
        {
            run = previous == day.AddDays(-1) ? run + 1 : 1;
            longest = Math.Max(longest, run);
            previous = day;
        }

        return new StudyStreak(current, longest);
    }
}
