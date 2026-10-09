using StudyHub.Shared.Analytics;

namespace StudyHub.Logic.Business.Contract;

public interface ICourseProgressOrchestrator
{
    /// <summary>Flashcards, practice exam results and study time per course of the active semester.</summary>
    Task<CourseProgressOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default);
}
