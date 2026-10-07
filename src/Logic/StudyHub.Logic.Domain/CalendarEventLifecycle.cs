using StudyHub.Logic.Domain.Contract;
using StudyHub.Shared.CalendarEvents;

namespace StudyHub.Logic.Domain;

public sealed class CalendarEventLifecycle(TimeProvider timeProvider) : ICalendarEventLifecycle
{
    private static readonly TimeSpan EndOfDay = TimeSpan.FromDays(1);

    public CalendarEvent Create(
        CalendarEventKind kind,
        string title,
        Guid? courseId,
        Guid? semesterId,
        DateOnly date,
        TimeOnly? startTime,
        int? durationMinutes,
        string? location
    )
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        return Validate(
            new CalendarEvent(
                Id: Guid.CreateVersion7(),
                Kind: kind,
                Title: title,
                CourseId: courseId,
                SemesterId: semesterId,
                Date: date,
                StartTime: startTime,
                DurationMinutes: durationMinutes,
                Location: location,
                CreatedAt: now,
                UpdatedAt: now
            )
        );
    }

    public CalendarEvent Update(
        CalendarEvent calendarEvent,
        CalendarEventKind kind,
        string title,
        Guid? courseId,
        Guid? semesterId,
        DateOnly date,
        TimeOnly? startTime,
        int? durationMinutes,
        string? location
    ) =>
        Validate(
            calendarEvent with
            {
                Kind = kind,
                Title = title,
                CourseId = courseId,
                SemesterId = semesterId,
                Date = date,
                StartTime = startTime,
                DurationMinutes = durationMinutes,
                Location = location,
                UpdatedAt = timeProvider.GetUtcNow().UtcDateTime,
            }
        );

    private static CalendarEvent Validate(CalendarEvent calendarEvent)
    {
        if (!Enum.IsDefined(calendarEvent.Kind))
        {
            throw new CalendarEventValidationException(
                "Choose whether the event is an exam or a deadline."
            );
        }

        var title = calendarEvent.Title?.Trim() ?? string.Empty;
        if (title.Length == 0)
        {
            throw new CalendarEventValidationException("Title is required.");
        }

        if (title.Length > CalendarEvent.TitleMaxLength)
        {
            throw new CalendarEventValidationException(
                $"Title must not exceed {CalendarEvent.TitleMaxLength} characters."
            );
        }

        if (calendarEvent.CourseId is not null && calendarEvent.SemesterId is not null)
        {
            throw new CalendarEventValidationException(
                "An event can belong to a course or a semester, not both."
            );
        }

        var startTime = calendarEvent.StartTime is { } time
            ? new TimeOnly(time.Hour, time.Minute)
            : (TimeOnly?)null;
        ValidateTime(calendarEvent.Kind, startTime, calendarEvent.DurationMinutes);

        var location = string.IsNullOrWhiteSpace(calendarEvent.Location)
            ? null
            : calendarEvent.Location.Trim();
        if (location?.Length > CalendarEvent.LocationMaxLength)
        {
            throw new CalendarEventValidationException(
                $"Location must not exceed {CalendarEvent.LocationMaxLength} characters."
            );
        }

        return calendarEvent with
        {
            Title = title,
            StartTime = startTime,
            Location = location,
        };
    }

    private static void ValidateTime(
        CalendarEventKind kind,
        TimeOnly? startTime,
        int? durationMinutes
    )
    {
        switch (kind, startTime, durationMinutes)
        {
            case (CalendarEventKind.Deadline, _, not null):
                throw new CalendarEventValidationException(
                    "A deadline has a due time but no duration."
                );
            case (CalendarEventKind.Exam, null, not null):
                throw new CalendarEventValidationException(
                    "An all-day exam has no duration. Set a start time or clear the duration."
                );
            case (CalendarEventKind.Exam, not null, null):
                throw new CalendarEventValidationException(
                    "An exam with a start time needs a duration."
                );
            case (CalendarEventKind.Exam, { } start, { } duration):
                if (
                    duration
                    is < CalendarEvent.MinDurationMinutes
                        or > CalendarEvent.MaxDurationMinutes
                )
                {
                    throw new CalendarEventValidationException(
                        $"Duration must be between {CalendarEvent.MinDurationMinutes} and {CalendarEvent.MaxDurationMinutes} minutes."
                    );
                }

                if (start.ToTimeSpan() + TimeSpan.FromMinutes(duration) > EndOfDay)
                {
                    throw new CalendarEventValidationException(
                        "An exam must end by midnight of the day it starts."
                    );
                }

                break;
        }
    }
}
