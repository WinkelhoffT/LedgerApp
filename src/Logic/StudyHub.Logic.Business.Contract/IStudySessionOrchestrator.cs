using StudyHub.Shared.StudySessions;

namespace StudyHub.Logic.Business.Contract;

public interface IStudySessionOrchestrator
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

    /// <summary>Marks the session as done, or changes the actual duration of a session that is already done.</summary>
    Task<StudySessionDto> CompleteAsync(
        Guid id,
        CompleteStudySessionRequest request,
        CancellationToken cancellationToken = default
    );

    /// <summary>Undoes "done".</summary>
    Task<StudySessionDto> ResetCompletionAsync(
        Guid id,
        CancellationToken cancellationToken = default
    );
}
