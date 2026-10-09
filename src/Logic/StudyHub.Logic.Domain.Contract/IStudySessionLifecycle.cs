using StudyHub.Shared.StudySessions;

namespace StudyHub.Logic.Domain.Contract;

/// <summary>
/// Domain rules for creating and changing a <see cref="StudySession"/>. Sessions are immutable
/// records, so every operation returns a new instance instead of mutating the given one. Start
/// times are kept to the whole minute; seconds are dropped.
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

    /// <exception cref="StudySessionValidationException">Title, owner, duration or location breaks a session rule.</exception>
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
}
