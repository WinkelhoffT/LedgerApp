namespace StudyHub.Logic.Domain.Contract;

/// <summary>
/// The learning streak: a study day counts when it has study time. The current streak only breaks
/// when a whole day is missed, so it still holds in the morning before today's first study time.
/// </summary>
public interface IStudyStreakProvider
{
    /// <param name="studyDays">Study days with study time; days after <paramref name="today"/> are ignored.</param>
    StudyStreak GetStreak(IEnumerable<DateOnly> studyDays, DateOnly today);
}
