using StudyHub.Logic.Domain.Contract;
using StudyHub.Shared.StudySessions;

namespace StudyHub.Logic.Domain;

public sealed class StudySessionLifecycle(TimeProvider timeProvider) : IStudySessionLifecycle
{
    private static readonly TimeSpan EndOfDay = TimeSpan.FromDays(1);

    public StudySession Create(
        string title,
        Guid? courseId,
        Guid? semesterId,
        DateOnly date,
        TimeOnly startTime,
        int durationMinutes,
        string? location
    )
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        return Validate(
            new StudySession(
                Id: Guid.CreateVersion7(),
                Title: title,
                CourseId: courseId,
                SemesterId: semesterId,
                Date: date,
                StartTime: startTime,
                DurationMinutes: durationMinutes,
                Location: location,
                CompletedAt: null,
                ActualDurationMinutes: null,
                CreatedAt: now,
                UpdatedAt: now
            )
        );
    }

    public StudySession Update(
        StudySession session,
        string title,
        Guid? courseId,
        Guid? semesterId,
        DateOnly date,
        TimeOnly startTime,
        int durationMinutes,
        string? location
    ) =>
        Validate(
            session with
            {
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

    private static StudySession Validate(StudySession session)
    {
        var title = session.Title?.Trim() ?? string.Empty;
        if (title.Length == 0)
        {
            throw new StudySessionValidationException("Title is required.");
        }

        if (title.Length > StudySession.TitleMaxLength)
        {
            throw new StudySessionValidationException(
                $"Title must not exceed {StudySession.TitleMaxLength} characters."
            );
        }

        if (session.CourseId is not null && session.SemesterId is not null)
        {
            throw new StudySessionValidationException(
                "A session can belong to a course or a semester, not both."
            );
        }

        if (
            session.DurationMinutes
            is < StudySession.MinDurationMinutes
                or > StudySession.MaxDurationMinutes
        )
        {
            throw new StudySessionValidationException(
                $"Duration must be between {StudySession.MinDurationMinutes} and {StudySession.MaxDurationMinutes} minutes."
            );
        }

        var startTime = new TimeOnly(session.StartTime.Hour, session.StartTime.Minute);
        if (startTime.ToTimeSpan() + TimeSpan.FromMinutes(session.DurationMinutes) > EndOfDay)
        {
            throw new StudySessionValidationException(
                "A session must end by midnight of the day it starts."
            );
        }

        var location = string.IsNullOrWhiteSpace(session.Location) ? null : session.Location.Trim();
        if (location?.Length > StudySession.LocationMaxLength)
        {
            throw new StudySessionValidationException(
                $"Location must not exceed {StudySession.LocationMaxLength} characters."
            );
        }

        return session with
        {
            Title = title,
            StartTime = startTime,
            Location = location,
        };
    }
}
