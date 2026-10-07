using StudyHub.Shared.Dashboard;

namespace StudyHub.Logic.Integration.Dashboard;

/// <summary>
/// Narrow HTTP access to StudyHub.Api's dashboard endpoints, covering only what the Dashboard
/// page actually calls - not a full Business-shaped management contract.
/// </summary>
public interface IDashboardAccessor
{
    Task<SemesterProgressDto> GetSemesterProgressAsync(CancellationToken cancellationToken = default);

    Task<AnkiStudyStatusDto> GetAnkiStudyStatusAsync(CancellationToken cancellationToken = default);
}
