using StudyHub.Shared.PracticeExams;

namespace StudyHub.Logic.Integration.PracticeExams;

/// <summary>
/// Narrow HTTP access to StudyHub.Api's stored practice exams, covering what the overview, the
/// cover page and the review's "Exclude task" call.
/// </summary>
public interface IPracticeExamAccessor
{
    Task<IReadOnlyList<PracticeExamDto>> GetAllAsync(
        bool includeArchived,
        CancellationToken cancellationToken = default
    );

    Task<PracticeExamDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PracticeExamAttemptSummaryDto>> GetAttemptsAsync(
        Guid examId,
        CancellationToken cancellationToken = default
    );

    Task<PracticeExamDto> ArchiveAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PracticeExamDto> RestoreAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PracticeExamDto> ExcludeTaskAsync(
        Guid taskId,
        CancellationToken cancellationToken = default
    );
}
