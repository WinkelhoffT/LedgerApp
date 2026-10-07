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
}
