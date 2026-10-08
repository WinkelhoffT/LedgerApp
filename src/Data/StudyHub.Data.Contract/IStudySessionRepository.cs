using StudyHub.Shared.StudySessions;

namespace StudyHub.Data.Contract;

public interface IStudySessionRepository
{
    Task<StudySession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Sessions from <paramref name="from"/> to <paramref name="to"/>, both days included, ordered by date and start time.</summary>
    Task<IReadOnlyList<StudySession>> GetByDateRangeAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    Task AddAsync(StudySession session, CancellationToken cancellationToken = default);

    void Update(StudySession session);

    void Remove(StudySession session);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
