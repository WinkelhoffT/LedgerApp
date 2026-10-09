using StudyHub.Shared.PracticeExams;

namespace StudyHub.Data.Contract;

/// <summary>Attempts at practice exams with their answers and self-graded criteria.</summary>
public interface IPracticeExamAttemptRepository
{
    /// <summary>The attempts of the given exams, without answers.</summary>
    Task<IReadOnlyList<PracticeExamAttempt>> GetByExamIdsAsync(
        IReadOnlyCollection<Guid> examIds,
        CancellationToken cancellationToken = default
    );

    /// <summary>The attempt with its answers and ticked criteria.</summary>
    Task<StoredPracticeExamAttempt?> GetWithAnswersAsync(
        Guid attemptId,
        CancellationToken cancellationToken = default
    );

    /// <summary>The exam's attempt that has not been submitted yet, with its answers.</summary>
    Task<StoredPracticeExamAttempt?> GetOpenAttemptAsync(
        Guid examId,
        CancellationToken cancellationToken = default
    );

    Task AddAsync(StoredPracticeExamAttempt attempt, CancellationToken cancellationToken = default);

    void Update(PracticeExamAttempt attempt);

    void UpdateAnswer(PracticeExamAnswer answer);

    /// <summary>Replaces the criteria ticked for one answer.</summary>
    Task ReplaceMetCriteriaAsync(
        Guid answerId,
        IReadOnlyCollection<Guid> criterionIds,
        CancellationToken cancellationToken = default
    );

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
