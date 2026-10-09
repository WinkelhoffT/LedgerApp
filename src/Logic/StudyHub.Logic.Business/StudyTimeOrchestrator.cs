using StudyHub.Data.Contract;
using StudyHub.Logic.Business.Contract;
using StudyHub.Logic.Domain.Contract;
using StudyHub.Shared.Analytics;

namespace StudyHub.Logic.Business;

public sealed class StudyTimeOrchestrator(
    IStudyAnalyticsRepository analyticsRepository,
    IStudyDayProvider studyDayProvider,
    ICalendarPeriodProvider periodProvider,
    IStudyTimeProcessor studyTimeProcessor,
    IStudyStreakProvider streakProvider
) : IStudyTimeOrchestrator
{
    private const int DaysPerWeek = 7;

    public async Task<StudyTimeStatisticsDto> GetStatisticsAsync(
        CancellationToken cancellationToken = default
    )
    {
        var today = studyDayProvider.GetCurrent().Date;
        var windowStart = today.AddDays(1 - StudyTimeStatisticsDto.WindowDays);

        // Sessions are read by their calendar dates, which run one day ahead of the study day
        // between midnight and the study day start; answers and attempts by the study day instants.
        var sessions = await analyticsRepository.GetSessionsAsync(
            windowStart,
            today.AddDays(1),
            cancellationToken
        );
        var fromUtc = studyDayProvider.GetStart(windowStart);
        var toUtc = studyDayProvider.GetStart(today.AddDays(1));
        var activities = new StudyActivities(
            sessions,
            await analyticsRepository.GetReviewActivitiesAsync(fromUtc, toUtc, cancellationToken),
            await analyticsRepository.GetSubmittedAttemptActivitiesAsync(
                fromUtc,
                toUtc,
                cancellationToken
            )
        );

        var days = studyTimeProcessor
            .GetDays(activities)
            .Where(d => d.Date >= windowStart && d.Date <= today)
            .ToDictionary(d => d.Date);
        var streak = streakProvider.GetStreak(
            days.Values.Where(d => d.Minutes > 0).Select(d => d.Date),
            today
        );

        var week = periodProvider.GetWeek(today);
        var thisWeekToDate = new CalendarPeriod(week.Start, today);
        var lastWeekToDate = new CalendarPeriod(
            week.Start.AddDays(-DaysPerWeek),
            today.AddDays(-DaysPerWeek)
        );
        var plannedThisWeek = sessions.Where(s => s.Date >= week.Start && s.Date <= today).ToList();

        int Minutes(DateOnly date) => days.GetValueOrDefault(date)?.Minutes ?? 0;
        int ReviewCount(DateOnly date) => days.GetValueOrDefault(date)?.ReviewCount ?? 0;

        return new StudyTimeStatisticsDto(
            today,
            days.Values.Any(d => d.Minutes > 0),
            thisWeekToDate.Days.Sum(Minutes),
            lastWeekToDate.Days.Sum(Minutes),
            thisWeekToDate.Days.Sum(ReviewCount),
            lastWeekToDate.Days.Sum(ReviewCount),
            plannedThisWeek.Count(s => s.CompletedAt is not null),
            plannedThisWeek.Count,
            streak.Current,
            streak.Longest,
            week.Days.Select(date => new StudyDayDto(
                    date,
                    Minutes(date),
                    ReviewCount(date),
                    date == today,
                    date > today
                ))
                .ToList(),
            Enumerable
                .Range(1 - StudyTimeStatisticsDto.WeekCount, StudyTimeStatisticsDto.WeekCount)
                .Select(offset => periodProvider.GetWeek(week.Start.AddDays(offset * DaysPerWeek)))
                .Select(w => new StudyWeekDto(
                    w.Start,
                    periodProvider.GetIsoWeek(w.Start),
                    w.Days.Sum(Minutes)
                ))
                .ToList(),
            new CalendarPeriod(
                week.Start.AddDays((1 - StudyTimeStatisticsDto.HeatmapWeekCount) * DaysPerWeek),
                week.End
            )
                .Days.Select(date => new StudyHeatmapDayDto(
                    date,
                    Minutes(date),
                    studyTimeProcessor.GetHeatLevel(Minutes(date)),
                    date > today
                ))
                .ToList()
        );
    }
}
