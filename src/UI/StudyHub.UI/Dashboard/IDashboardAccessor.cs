using StudyHub.Logic.Business.Dashboard;

namespace StudyHub.UI.Dashboard;

/// <summary>
/// Narrow HTTP access to StudyHub.Api's dashboard endpoint, covering only what the Dashboard page
/// actually calls - not the full <see cref="IDashboardManagement"/> business contract.
/// </summary>
public interface IDashboardAccessor
{
    Task<SemesterProgressDto> GetSemesterProgressAsync(CancellationToken cancellationToken = default);
}
