using StudyHub.Data.Contract;
using StudyHub.Logic.Business.Contract;
using StudyHub.Logic.Domain.Contract;
using StudyHub.Shared.Analytics;

namespace StudyHub.Logic.Business;

public sealed class CourseProgressOrchestrator(
    IStudyAnalyticsRepository analyticsRepository,
    ISemesterRepository semesterRepository,
    ICourseRepository courseRepository,
    IActiveSemesterProvider activeSemesterProvider,
    IStudyDayProvider studyDayProvider,
    IStudyTimeProcessor studyTimeProcessor,
    ICourseProgressProcessor courseProgressProcessor
) : ICourseProgressOrchestrator
{
    public async Task<CourseProgressOverviewDto> GetOverviewAsync(
        CancellationToken cancellationToken = default
    )
    {
        var today = studyDayProvider.GetCurrent().Date;
        var semester = activeSemesterProvider.GetActive(
            await semesterRepository.GetAllAsync(cancellationToken),
            today
        );
        if (semester is null)
        {
            return CourseProgressOverviewDto.Empty;
        }

        var courses = (await courseRepository.GetBySemesterIdAsync(semester.Id, cancellationToken))
            .Where(c => !c.IsArchived)
            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var deckCounts = (
            await analyticsRepository.GetDeckProgressCountsAsync(cancellationToken)
        ).ToLookup(d => d.CourseId);
        var results = (await analyticsRepository.GetGradedResultsAsync(cancellationToken)).ToLookup(
            r => r.CourseId
        );

        var windowStart = today.AddDays(1 - StudyTimeStatisticsDto.WindowDays);
        var studyMinutes = await GetStudyMinutesByCourseAsync(
            semester.StartDate > windowStart ? semester.StartDate : windowStart,
            today,
            cancellationToken
        );

        return new CourseProgressOverviewDto(
            HasActiveSemester: true,
            semester.Name,
            courses
                .Select(course =>
                {
                    var exams = courseProgressProcessor.GetExamResults(results[course.Id]);
                    return new CourseProgressDto(
                        course.Id,
                        course.Name,
                        course.Color,
                        studyMinutes.GetValueOrDefault(course.Id),
                        courseProgressProcessor.GetFlashcardProgress(deckCounts[course.Id]),
                        exams.LatestPercent,
                        exams.BestPercent,
                        exams.GradedAttempts
                    );
                })
                .ToList()
        );
    }

    // Sessions are read by their calendar dates, which run one day ahead of the study day between
    // midnight and the study day start; answers and attempts by the study day instants.
    private async Task<Dictionary<Guid, int>> GetStudyMinutesByCourseAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken
    )
    {
        var fromUtc = studyDayProvider.GetStart(from);
        var toUtc = studyDayProvider.GetStart(to.AddDays(1));
        var activities = new StudyActivities(
            await analyticsRepository.GetSessionsAsync(from, to.AddDays(1), cancellationToken),
            await analyticsRepository.GetReviewActivitiesAsync(fromUtc, toUtc, cancellationToken),
            await analyticsRepository.GetSubmittedAttemptActivitiesAsync(
                fromUtc,
                toUtc,
                cancellationToken
            )
        );

        return studyTimeProcessor
            .GetDays(activities)
            .Where(d => d.Date >= from && d.Date <= to)
            .SelectMany(d => d.MinutesByCourse)
            .GroupBy(c => c.Key)
            .ToDictionary(g => g.Key, g => g.Sum(c => c.Value));
    }
}
