using StudyHub.Logic.Domain.Contract;
using StudyHub.Shared.Configuration;

namespace StudyHub.Logic.Domain;

public sealed class StudyTimeProcessor : IStudyTimeProcessor
{
    private const int SessionPriority = 0;
    private const int ActivityPriority = 1;
    private const int MaxExamDurationFactor = 2;

    private static readonly TimeSpan MaxAnswerTime = TimeSpan.FromSeconds(60);

    private readonly IStudyDayProvider _studyDayProvider;
    private readonly TimeZoneInfo _timeZone;

    public StudyTimeProcessor(CalendarOptions options, IStudyDayProvider studyDayProvider)
    {
        _studyDayProvider = studyDayProvider;
        _timeZone = TimeZoneInfo.FindSystemTimeZoneById(options.TimeZone);
    }

    public IReadOnlyList<StudyTimeDay> GetDays(StudyActivities activities)
    {
        var seconds = new Dictionary<DateOnly, double>();
        var courseSeconds = new Dictionary<DateOnly, Dictionary<Guid, double>>();

        foreach (var interval in RemoveOverlaps(GetIntervals(activities)))
        {
            foreach (var (date, start, end) in SplitByStudyDay(interval))
            {
                var duration = (end - start).TotalSeconds;
                seconds[date] = seconds.GetValueOrDefault(date) + duration;

                if (interval.CourseId is { } courseId)
                {
                    var byCourse = courseSeconds.TryGetValue(date, out var existing)
                        ? existing
                        : courseSeconds[date] = [];
                    byCourse[courseId] = byCourse.GetValueOrDefault(courseId) + duration;
                }
            }
        }

        var reviewCounts = activities
            .Reviews.GroupBy(r => _studyDayProvider.GetDate(r.ReviewedAt))
            .ToDictionary(g => g.Key, g => g.Count());

        // Rounded once per day, so many short answers add up before they are rounded.
        return seconds
            .Keys.Union(reviewCounts.Keys)
            .Order()
            .Select(date => new StudyTimeDay(
                date,
                ToMinutes(seconds.GetValueOrDefault(date)),
                reviewCounts.GetValueOrDefault(date),
                courseSeconds.TryGetValue(date, out var byCourse)
                    ? byCourse.ToDictionary(c => c.Key, c => ToMinutes(c.Value))
                    : new Dictionary<Guid, int>()
            ))
            .ToList();
    }

    public int GetHeatLevel(int minutes) =>
        minutes switch
        {
            <= 0 => 0,
            < 30 => 1,
            < 60 => 2,
            < 120 => 3,
            _ => 4,
        };

    private IEnumerable<StudyInterval> GetIntervals(StudyActivities activities)
    {
        foreach (var session in activities.Sessions)
        {
            if (session is { CompletedAt: not null, ActualDurationMinutes: { } actualMinutes })
            {
                var start = ToUtc(session.Date, session.StartTime);
                yield return new StudyInterval(
                    start,
                    start.AddMinutes(actualMinutes),
                    SessionPriority,
                    session.CourseId
                );
            }
        }

        // The time between two answers is the time the second card was on screen; a longer gap is
        // a pause and counts as the maximum answer time only.
        DateTime? previous = null;
        foreach (var review in activities.Reviews.OrderBy(r => r.ReviewedAt))
        {
            var gap = review.ReviewedAt - previous;
            var length = gap < MaxAnswerTime ? gap.Value : MaxAnswerTime;
            yield return new StudyInterval(
                review.ReviewedAt - length,
                review.ReviewedAt,
                ActivityPriority,
                review.CourseId
            );
            previous = review.ReviewedAt;
        }

        // An attempt without a time limit can stay open overnight; it counts at most twice the exam's duration.
        foreach (var attempt in activities.Attempts)
        {
            var latestEnd = attempt.StartedAt.AddMinutes(
                attempt.ExamDurationMinutes * MaxExamDurationFactor
            );
            yield return new StudyInterval(
                attempt.StartedAt,
                attempt.SubmittedAt < latestEnd ? attempt.SubmittedAt : latestEnd,
                ActivityPriority,
                attempt.CourseId
            );
        }
    }

    // Sorted by start, every point an earlier interval covers lies before the latest end so far.
    private static IEnumerable<StudyInterval> RemoveOverlaps(IEnumerable<StudyInterval> intervals)
    {
        var coveredUntil = DateTime.MinValue;

        foreach (var interval in intervals.OrderBy(i => i.Start).ThenBy(i => i.Priority))
        {
            var start = interval.Start > coveredUntil ? interval.Start : coveredUntil;
            if (start < interval.End)
            {
                yield return interval with
                {
                    Start = start,
                };
                coveredUntil = interval.End;
            }
        }
    }

    private IEnumerable<(DateOnly Date, DateTime Start, DateTime End)> SplitByStudyDay(
        StudyInterval interval
    )
    {
        var start = interval.Start;

        while (start < interval.End)
        {
            var date = _studyDayProvider.GetDate(start);
            var nextDayStart = _studyDayProvider.GetStart(date.AddDays(1));
            var end = interval.End < nextDayStart ? interval.End : nextDayStart;

            yield return (date, start, end);
            start = end;
        }
    }

    // Session times are local wall-clock values; a start inside a daylight-saving gap does not
    // exist that day and moves forward, as the study day start does.
    private DateTime ToUtc(DateOnly date, TimeOnly time)
    {
        var local = date.ToDateTime(time);
        if (_timeZone.IsInvalidTime(local))
        {
            local = local.AddHours(1);
        }

        return TimeZoneInfo.ConvertTimeToUtc(local, _timeZone);
    }

    private static int ToMinutes(double seconds) =>
        (int)Math.Round(seconds / 60, MidpointRounding.AwayFromZero);
}
