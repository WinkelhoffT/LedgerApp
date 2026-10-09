using StudyHub.Data.Contract;
using StudyHub.Logic.Business.Contract;
using StudyHub.Logic.Domain.Contract;
using StudyHub.Shared.Calendar;
using StudyHub.Shared.CalendarEvents;
using StudyHub.Shared.Courses;
using StudyHub.Shared.Semesters;
using StudyHub.Shared.StudySessions;

namespace StudyHub.Logic.Business;

public sealed class CalendarOrchestrator(
    IStudySessionRepository sessionRepository,
    ICalendarEventRepository eventRepository,
    ICourseRepository courseRepository,
    ISemesterRepository semesterRepository,
    ICalendarPeriodProvider periodProvider,
    ICalendarLaneProcessor laneProcessor
) : ICalendarOrchestrator
{
    public async Task<CalendarMonthDto> GetMonthAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        var period = periodProvider.GetMonth(year, month);
        var entries = await LoadAsync(period, cancellationToken);

        return new CalendarMonthDto(
            year,
            month,
            periodProvider.GetToday(),
            await ToDaysAsync(period, entries, cancellationToken));
    }

    public async Task<CalendarWeekDto> GetWeekAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var period = periodProvider.GetWeek(date);
        var entries = await LoadAsync(period, cancellationToken);
        var hours = periodProvider.GetWeekHours(entries.Sessions.Select(ToSlot).Concat(entries.Events.Select(ToSlot).OfType<CalendarTimeSlot>()));

        return new CalendarWeekDto(
            period.Start,
            period.End,
            periodProvider.GetIsoWeek(period.Start),
            periodProvider.GetToday(),
            hours.StartHour,
            hours.EndHour,
            await ToDaysAsync(period, entries, cancellationToken));
    }

    public async Task<CalendarDayDto> GetTodayAsync(CancellationToken cancellationToken = default)
    {
        var today = periodProvider.GetToday();
        var period = new CalendarPeriod(today, today);

        return (await ToDaysAsync(period, await LoadAsync(period, cancellationToken), cancellationToken))[0];
    }

    private async Task<PeriodEntries> LoadAsync(CalendarPeriod period, CancellationToken cancellationToken) =>
        new(
            await sessionRepository.GetByDateRangeAsync(period.Start, period.End, cancellationToken),
            await eventRepository.GetByDateRangeAsync(period.Start, period.End, cancellationToken));

    private async Task<IReadOnlyList<CalendarDayDto>> ToDaysAsync(
        CalendarPeriod period,
        PeriodEntries entries,
        CancellationToken cancellationToken
    )
    {
        // Archived courses and semesters are included, so their entries keep their name and color.
        var courses = entries.Sessions.Any(s => s.CourseId is not null) || entries.Events.Any(e => e.CourseId is not null)
            ? (await courseRepository.GetAllAsync(cancellationToken)).ToDictionary(c => c.Id)
            : [];
        var semesters = entries.Sessions.Any(s => s.SemesterId is not null) || entries.Events.Any(e => e.SemesterId is not null)
            ? (await semesterRepository.GetAllAsync(cancellationToken)).ToDictionary(s => s.Id)
            : [];
        var sessionsByDate = entries.Sessions.ToLookup(s => s.Date);
        var eventsByDate = entries.Events.ToLookup(e => e.Date);

        return period.Days
            .Select(date =>
            {
                var sessions = sessionsByDate[date].OrderBy(s => s.StartTime).ToList();
                var events = eventsByDate[date]
                    .OrderBy(e => e.StartTime is not null)
                    .ThenBy(e => e.StartTime)
                    .ThenBy(e => e.Title, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                // Sessions and timed exams share the week view's columns; other events have no lane.
                var lanes = laneProcessor
                    .Assign(sessions.Select(ToSlot).Concat(events.Select(ToSlot).OfType<CalendarTimeSlot>()))
                    .ToDictionary(l => l.Id);

                return new CalendarDayDto(
                    date,
                    sessions
                        .Select(s => new CalendarSessionDto(
                            StudySessionMapper.ToDto(s, Find(courses, s.CourseId), Find(semesters, s.SemesterId)),
                            lanes[s.Id].Lane,
                            lanes[s.Id].LaneCount))
                        .ToList(),
                    events
                        .Select(e => new CalendarEventEntryDto(
                            CalendarEventMapper.ToDto(e, Find(courses, e.CourseId), Find(semesters, e.SemesterId)),
                            lanes.GetValueOrDefault(e.Id)?.Lane ?? 0,
                            lanes.GetValueOrDefault(e.Id)?.LaneCount ?? 1))
                        .ToList());
            })
            .ToList();
    }

    private static T? Find<T>(Dictionary<Guid, T> items, Guid? id) where T : class =>
        id is { } key ? items.GetValueOrDefault(key) : null;

    private static CalendarTimeSlot ToSlot(StudySession session) =>
        new(session.Id, session.StartTime, session.DurationMinutes);

    // Only a timed exam takes up time in the grid.
    private static CalendarTimeSlot? ToSlot(CalendarEvent calendarEvent) =>
        calendarEvent is { StartTime: { } start, DurationMinutes: { } duration }
            ? new CalendarTimeSlot(calendarEvent.Id, start, duration)
            : null;

    private sealed record PeriodEntries(IReadOnlyList<StudySession> Sessions, IReadOnlyList<CalendarEvent> Events);
}
