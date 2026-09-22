using StudyHub.Shared.Dashboard;

namespace StudyHub.Logic.Integration.Contract.Dashboard;

/// <summary>
/// Narrow HTTP access to StudyHub.Api's dashboard endpoint, covering only what the Dashboard
/// page actually calls - not a full Business-shaped management contract.
/// </summary>
public interface IDashboardAccessor
{
    Task<SemesterProgressDto> GetSemesterProgressAsync(CancellationToken cancellationToken = default);
}
