using StudyHub.Shared.Dashboard;

namespace StudyHub.Logic.Business.Contract;

public interface IDashboardOrchestrator
{
    Task<SemesterProgressDto> GetSemesterProgressAsync(CancellationToken cancellationToken = default);
}
