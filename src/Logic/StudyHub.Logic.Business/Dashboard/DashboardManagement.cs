using StudyHub.Logic.Business.Semesters;
using StudyHub.Logic.Domain.SemesterProgress;

namespace StudyHub.Logic.Business.Dashboard;

public sealed class DashboardManagement(
    ISemesterManagement semesterManagement,
    ISemesterProgressCalculator semesterProgressCalculator) : IDashboardManagement
{
    public async Task<SemesterProgressDto> GetSemesterProgressAsync(CancellationToken cancellationToken = default)
    {
        var semesters = await semesterManagement.GetAllAsync(cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var activeSemester = semesters
            .Where(s => !s.IsArchived && s.StartDate <= today && today <= s.EndDate)
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefault();

        if (activeSemester is null)
        {
            return SemesterProgressDto.Empty;
        }

        var progress = semesterProgressCalculator.Calculate(activeSemester.StartDate, activeSemester.EndDate, today);

        return new SemesterProgressDto(
            HasActiveSemester: true,
            SemesterId: activeSemester.Id,
            SemesterName: activeSemester.Name,
            StartDate: activeSemester.StartDate,
            EndDate: activeSemester.EndDate,
            TotalDays: progress.TotalDays,
            ElapsedDays: progress.ElapsedDays,
            RemainingDays: progress.RemainingDays,
            PercentComplete: progress.PercentComplete);
    }
}
