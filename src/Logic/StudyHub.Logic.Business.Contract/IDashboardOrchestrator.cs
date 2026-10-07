using StudyHub.Shared.Dashboard;

namespace StudyHub.Logic.Business.Contract;

public interface IDashboardOrchestrator
{
    Task<SemesterProgressDto> GetSemesterProgressAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Today's due Anki cards, read live from AnkiConnect. Never fails because Anki is unreachable;
    /// that is reported as <see cref="AnkiConnectionStatus.Unavailable"/>.
    /// </summary>
    Task<AnkiStudyStatusDto> GetAnkiStudyStatusAsync(CancellationToken cancellationToken = default);
}
