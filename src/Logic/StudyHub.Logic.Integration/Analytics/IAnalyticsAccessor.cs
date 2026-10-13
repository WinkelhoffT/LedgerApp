using StudyHub.Shared.Analytics;

namespace StudyHub.Logic.Integration.Analytics;

/// <summary>Narrow HTTP access to StudyHub.Api's analytics endpoints, covering the Analytics page.</summary>
public interface IAnalyticsAccessor
{
    Task<StudyTimeStatisticsDto> GetStudyTimeAsync(CancellationToken cancellationToken = default);

    Task<CourseProgressOverviewDto> GetCourseProgressAsync(
        CancellationToken cancellationToken = default
    );
}
