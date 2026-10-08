using StudyHub.Data.Contract;
using StudyHub.Logic.Business.Contract;
using StudyHub.Logic.Domain.Contract;
using StudyHub.Shared.Calendar;
using StudyHub.Shared.Courses;
using StudyHub.Shared.Semesters;
using StudyHub.Shared.StudySessions;

namespace StudyHub.Logic.Business;

public sealed class CalendarOrchestrator(
    IStudySessionRepository sessionRepository,
    ICourseRepository courseRepository,
    ISemesterRepository semesterRepository,
    ICalendarPeriodProvider periodProvider,
    IStudySessionLaneProcessor laneProcessor
) : ICalendarOrchestrator
{
    public async Task<CalendarMonthDto> GetMonthAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        var period = periodProvider.GetMonth(year, month);
        var sessions = await sessionRepository.GetByDateRangeAsync(period.Start, period.End, cancellationToken);

        return new CalendarMonthDto(
            year,
            month,
            periodProvider.GetToday(),
            await ToDaysAsync(period, sessions, cancellationToken));
    }

    public async Task<CalendarWeekDto> GetWeekAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var period = periodProvider.GetWeek(date);
        var sessions = await sessionRepository.GetByDateRangeAsync(period.Start, period.End, cancellationToken);
        var hours = periodProvider.GetWeekHours(sessions);

        return new CalendarWeekDto(
            period.Start,
            period.End,
            periodProvider.GetIsoWeek(period.Start),
            periodProvider.GetToday(),
            hours.StartHour,
            hours.EndHour,
            await ToDaysAsync(period, sessions, cancellationToken));
    }

    public async Task<CalendarDayDto> GetTodayAsync(CancellationToken cancellationToken = default)
    {
        var today = periodProvider.GetToday();
        var sessions = await sessionRepository.GetByDateRangeAsync(today, today, cancellationToken);

        return (await ToDaysAsync(new CalendarPeriod(today, today), sessions, cancellationToken))[0];
    }

    private async Task<IReadOnlyList<CalendarDayDto>> ToDaysAsync(
        CalendarPeriod period,
        IReadOnlyList<StudySession> sessions,
        CancellationToken cancellationToken
    )
    {
        // Archived courses and semesters are included, so their sessions keep their name and color.
        var courses = sessions.Any(s => s.CourseId is not null)
            ? (await courseRepository.GetAllAsync(cancellationToken)).ToDictionary(c => c.Id)
            : [];
        var semesters = sessions.Any(s => s.SemesterId is not null)
            ? (await semesterRepository.GetAllAsync(cancellationToken)).ToDictionary(s => s.Id)
            : [];
        var sessionsByDate = sessions.ToLookup(s => s.Date);

        return period.Days
            .Select(date => new CalendarDayDto(
                date,
                laneProcessor.Assign(sessionsByDate[date])
                    .Select(lane => new CalendarSessionDto(ToDto(lane.Session, courses, semesters), lane.Lane, lane.LaneCount))
                    .ToList()))
            .ToList();
    }

    private static StudySessionDto ToDto(
        StudySession session,
        Dictionary<Guid, Course> courses,
        Dictionary<Guid, Semester> semesters
    ) =>
        StudySessionMapper.ToDto(
            session,
            session.CourseId is { } courseId ? courses.GetValueOrDefault(courseId) : null,
            session.SemesterId is { } semesterId ? semesters.GetValueOrDefault(semesterId) : null);
}
