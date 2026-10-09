using StudyHub.Shared.PracticeExams;

namespace StudyHub.Data.Contract;

/// <summary>Stored practice exams with their tasks, options and criteria.</summary>
public interface IPracticeExamRepository
{
    /// <summary>All exams, newest first.</summary>
    Task<IReadOnlyList<PracticeExam>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<PracticeExam?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>The exam with its tasks, each with its options and criteria, ordered by position.</summary>
    Task<StoredPracticeExam?> GetWithTasksAsync(
        Guid id,
        CancellationToken cancellationToken = default
    );

    /// <summary>Task count and points per exam that has tasks; <c>null</c> covers every exam.</summary>
    Task<IReadOnlyList<PracticeExamTaskTotals>> GetTaskTotalsAsync(
        Guid? examId = null,
        CancellationToken cancellationToken = default
    );

    Task<PracticeExamTask?> GetTaskByIdAsync(
        Guid taskId,
        CancellationToken cancellationToken = default
    );

    Task AddAsync(StoredPracticeExam exam, CancellationToken cancellationToken = default);

    void Update(PracticeExam exam);

    void UpdateTask(PracticeExamTask task);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
