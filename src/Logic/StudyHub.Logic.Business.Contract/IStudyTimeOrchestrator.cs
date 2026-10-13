using StudyHub.Shared.Analytics;

namespace StudyHub.Logic.Business.Contract;

public interface IStudyTimeOrchestrator
{
    /// <summary>Study time, streak and chart series of the last 365 study days.</summary>
    Task<StudyTimeStatisticsDto> GetStatisticsAsync(CancellationToken cancellationToken = default);
}
