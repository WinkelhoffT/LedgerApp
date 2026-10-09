using StudyHub.Shared.StudySessions;

namespace StudyHub.Logic.Domain.Contract;

/// <summary>
/// Domain rules for creating and changing a <see cref="StudySession"/>. Sessions are immutable
/// records, so every operation returns a new instance instead of mutating the given one. Start
/// times are kept to the whole minute; seconds are dropped. A session that is done has an actual
/// duration, follows the same limits as the planned one and cannot lie in the future.
/// </summary>
public interface IStudySessionLifecycle
{
    /// <exception cref="StudySessionValidationException">Title, owner, duration or location breaks a session rule.</exception>
    StudySession Create(
        string title,
        Guid? courseId,
        Guid? semesterId,
        DateOnly date,
        TimeOnly startTime,
        int durationMinutes,
        string? location
    );

    /// <summary>Changes the session; a session that is done stays done.</summary>
    /// <exception cref="StudySessionValidationException">
    /// Title, owner, duration or location breaks a session rule, or a session that is done would move to a future date.
    /// </exception>
    StudySession Update(
        StudySession session,
        string title,
        Guid? courseId,
        Guid? semesterId,
        DateOnly date,
        TimeOnly startTime,
        int durationMinutes,
        string? location
    );

    /// <summary>
    /// Marks the session as done, or changes the actual duration of a session that is already done;
    /// such a session keeps the instant it was first marked as done.
    /// </summary>
    /// <exception cref="StudySessionValidationException">
    /// The session lies after today (calendar time zone), or the actual duration breaks a session rule.
    /// </exception>
    StudySession Complete(StudySession session, int actualDurationMinutes);

    /// <summary>Undoes "done": the session is planned only again.</summary>
    StudySession ResetCompletion(StudySession session);
}
