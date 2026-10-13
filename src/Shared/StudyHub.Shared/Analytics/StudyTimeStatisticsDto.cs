namespace StudyHub.Shared.Analytics;

/// <summary>
/// Study time of the last 365 study days, ready to render. Days are study days (they start at the
/// configured hour, like the flashcard limits) and weeks run from Monday to Sunday. "Last week to
/// date" covers last week's Monday up to the same weekday as today, so it compares like with like.
/// </summary>
/// <param name="Today">The current study day.</param>
/// <param name="HasStudyTime">Whether any study time was recorded in the last 365 study days.</param>
/// <param name="SessionsPlannedThisWeek">Sessions of this week dated today or earlier.</param>
/// <param name="SessionsDoneThisWeek">Those of <paramref name="SessionsPlannedThisWeek"/> that were marked as done.</param>
/// <param name="Days">Monday to Sunday of this week.</param>
/// <param name="Weeks">The last 8 weeks, oldest first; the last one is this week.</param>
/// <param name="Heatmap">The last 5 weeks, Monday to Sunday, oldest first.</param>
public sealed record StudyTimeStatisticsDto(
    DateOnly Today,
    bool HasStudyTime,
    int ThisWeekMinutes,
    int LastWeekToDateMinutes,
    int ReviewsThisWeek,
    int ReviewsLastWeekToDate,
    int SessionsDoneThisWeek,
    int SessionsPlannedThisWeek,
    int CurrentStreakDays,
    int LongestStreakDays,
    IReadOnlyList<StudyDayDto> Days,
    IReadOnlyList<StudyWeekDto> Weeks,
    IReadOnlyList<StudyHeatmapDayDto> Heatmap
)
{
    public const int WindowDays = 365;
    public const int WeekCount = 8;
    public const int HeatmapWeekCount = 5;
}
