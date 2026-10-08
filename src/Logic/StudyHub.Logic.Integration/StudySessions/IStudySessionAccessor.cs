using StudyHub.Shared.StudySessions;

namespace StudyHub.Logic.Integration.StudySessions;

/// <summary>Narrow HTTP access to StudyHub.Api's study session endpoints, covering the calendar's session dialog.</summary>
public interface IStudySessionAccessor
{
    Task<StudySessionDto> CreateAsync(CreateStudySessionRequest request, CancellationToken cancellationToken = default);

    Task<StudySessionDto> UpdateAsync(UpdateStudySessionRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
