using StudyHub.Shared.Courses;
using StudyHub.Shared.Semesters;
using StudyHub.Shared.StudySessions;

namespace StudyHub.Logic.Business;

/// <summary>Maps stored sessions to the DTO the calendar and the session endpoints return.</summary>
internal static class StudySessionMapper
{
    /// <param name="course">The session's course, if it has one.</param>
    /// <param name="semester">The session's semester, if it has one.</param>
    public static StudySessionDto ToDto(StudySession session, Course? course, Semester? semester) =>
        new(
            session.Id,
            session.Title,
            session.CourseId,
            session.SemesterId,
            session.Date,
            session.StartTime,
            session.StartTime.AddMinutes(session.DurationMinutes),
            session.DurationMinutes,
            session.Location,
            course?.Name ?? semester?.Name,
            course?.Color,
            session.CreatedAt,
            session.UpdatedAt
        );
}
