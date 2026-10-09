using StudyHub.Shared.PracticeExams;

namespace StudyHub.Logic.Business.Contract;

public interface IPracticeExamOrchestrator
{
    Task<IReadOnlyList<PracticeExamDto>> GetAllAsync(
        bool includeArchived,
        CancellationToken cancellationToken = default
    );

    Task<PracticeExamDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>The exam's attempts, newest first.</summary>
    Task<IReadOnlyList<PracticeExamAttemptSummaryDto>> GetAttemptsAsync(
        Guid examId,
        CancellationToken cancellationToken = default
    );

    Task<PracticeExamDto> ArchiveAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PracticeExamDto> RestoreAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Leaves a flawed task out of future attempts.</summary>
    Task<PracticeExamDto> ExcludeTaskAsync(
        Guid taskId,
        CancellationToken cancellationToken = default
    );
}
