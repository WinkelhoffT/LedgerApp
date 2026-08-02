using StudyHub.Logic.Business.Semesters;

namespace StudyHub.Logic.Business.Dashboard;

public sealed class DashboardManagement(ISemesterManagement semesterManagement) : IDashboardManagement
{
    public async Task<SemesterProgressDto> GetSemesterProgressAsync(CancellationToken cancellationToken = default)
    {
        var semesters = await semesterManagement.GetAllAsync(cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var activeSemester = semesters
            .Where(s => !s.IsArchived && s.StartDate <= today && today <= s.EndDate)
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefault();

        return activeSemester is null
            ? SemesterProgressDto.Empty
            : SemesterProgressCalculator.Calculate(activeSemester, today);
    }
}
