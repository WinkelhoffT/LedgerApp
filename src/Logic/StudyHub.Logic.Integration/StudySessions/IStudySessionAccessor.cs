using StudyHub.Shared.StudySessions;

namespace StudyHub.Logic.Integration.StudySessions;

/// <summary>
/// Narrow HTTP access to StudyHub.Api's study session endpoints, covering the calendar's session
/// dialog and the "Mark as done" button of a session card.
/// </summary>
public interface IStudySessionAccessor
{
    Task<StudySessionDto> CreateAsync(
        CreateStudySessionRequest request,
        CancellationToken cancellationToken = default
    );

    Task<StudySessionDto> UpdateAsync(
        UpdateStudySessionRequest request,
        CancellationToken cancellationToken = default
    );

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<StudySessionDto> CompleteAsync(
        Guid id,
        CompleteStudySessionRequest request,
        CancellationToken cancellationToken = default
    );

    Task<StudySessionDto> ResetCompletionAsync(
        Guid id,
        CancellationToken cancellationToken = default
    );
}
