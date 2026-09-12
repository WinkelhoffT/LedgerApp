using StudyHub.Shared.Dashboard;

namespace StudyHub.Logic.Business.Dashboard;

public interface IDashboardManagement
{
    Task<SemesterProgressDto> GetSemesterProgressAsync(CancellationToken cancellationToken = default);
}
