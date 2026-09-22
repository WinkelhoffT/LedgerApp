using StudyHub.Logic.Business.Contract;
using StudyHub.Logic.Domain.SemesterProgress;
using StudyHub.Logic.Domain.Semesters;
using StudyHub.Shared.Dashboard;

namespace StudyHub.Logic.Business.Dashboard;

public sealed class DashboardOrchestrator(
    ISemesterRepository semesterRepository,
    IActiveSemesterProvider activeSemesterProvider,
    ISemesterProgressCalculator semesterProgressCalculator
) : IDashboardOrchestrator
{
    public async Task<SemesterProgressDto> GetSemesterProgressAsync(
        CancellationToken cancellationToken = default
    )
    {
        var semesters = await semesterRepository.GetAllAsync(cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var activeSemester = activeSemesterProvider.GetActive(semesters, today);
        if (activeSemester is null)
        {
            return SemesterProgressDto.Empty;
        }

        var progress = semesterProgressCalculator.Calculate(
            activeSemester.StartDate,
            activeSemester.EndDate,
            today
        );

        return new SemesterProgressDto(
            HasActiveSemester: true,
            SemesterId: activeSemester.Id,
            SemesterName: activeSemester.Name,
            StartDate: activeSemester.StartDate,
            EndDate: activeSemester.EndDate,
            TotalDays: progress.TotalDays,
            ElapsedDays: progress.ElapsedDays,
            RemainingDays: progress.RemainingDays,
            PercentComplete: progress.PercentComplete
        );
    }
}
