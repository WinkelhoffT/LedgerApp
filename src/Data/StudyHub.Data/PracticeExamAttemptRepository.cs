using Microsoft.EntityFrameworkCore;
using StudyHub.Data.Contract;
using StudyHub.Shared.PracticeExams;

namespace StudyHub.Data;

public sealed class PracticeExamAttemptRepository(ApplicationDbContext dbContext)
    : IPracticeExamAttemptRepository
{
    public async Task<IReadOnlyList<PracticeExamAttempt>> GetByExamIdsAsync(
        IReadOnlyCollection<Guid> examIds,
        CancellationToken cancellationToken = default
    )
    {
        if (examIds.Count == 0)
        {
            return [];
        }

        return await dbContext
            .PracticeExamAttempts.AsNoTracking()
            .Where(a => examIds.Contains(a.ExamId))
            .OrderByDescending(a => a.StartedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<StoredPracticeExamAttempt?> GetWithAnswersAsync(
        Guid attemptId,
        CancellationToken cancellationToken = default
    )
    {
        var attempt = await dbContext
            .PracticeExamAttempts.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == attemptId, cancellationToken);

        return attempt is null ? null : await WithAnswersAsync(attempt, cancellationToken);
    }

    public async Task<StoredPracticeExamAttempt?> GetOpenAttemptAsync(
        Guid examId,
        CancellationToken cancellationToken = default
    )
    {
        var attempt = await dbContext
            .PracticeExamAttempts.AsNoTracking()
            .Where(a => a.ExamId == examId && a.SubmittedAt == null)
            .OrderByDescending(a => a.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return attempt is null ? null : await WithAnswersAsync(attempt, cancellationToken);
    }

    public async Task AddAsync(
        StoredPracticeExamAttempt attempt,
        CancellationToken cancellationToken = default
    )
    {
        await dbContext.PracticeExamAttempts.AddAsync(attempt.Attempt, cancellationToken);
        await dbContext.PracticeExamAnswers.AddRangeAsync(attempt.Answers, cancellationToken);
        await dbContext.PracticeExamAnswerCriteria.AddRangeAsync(
            attempt.MetCriteria,
            cancellationToken
        );
    }

    public void Update(PracticeExamAttempt attempt) =>
        dbContext.PracticeExamAttempts.Update(attempt);

    public void UpdateAnswer(PracticeExamAnswer answer) =>
        dbContext.PracticeExamAnswers.Update(answer);

    public async Task ReplaceMetCriteriaAsync(
        Guid answerId,
        IReadOnlyCollection<Guid> criterionIds,
        CancellationToken cancellationToken = default
    )
    {
        var existing = await dbContext
            .PracticeExamAnswerCriteria.Where(c => c.AnswerId == answerId)
            .ToListAsync(cancellationToken);

        dbContext.PracticeExamAnswerCriteria.RemoveRange(
            existing.Where(c => !criterionIds.Contains(c.CriterionId))
        );
        await dbContext.PracticeExamAnswerCriteria.AddRangeAsync(
            criterionIds
                .Where(criterionId => existing.All(c => c.CriterionId != criterionId))
                .Select(criterionId => new PracticeExamAnswerCriterion(answerId, criterionId)),
            cancellationToken
        );
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);

    private async Task<StoredPracticeExamAttempt> WithAnswersAsync(
        PracticeExamAttempt attempt,
        CancellationToken cancellationToken
    )
    {
        var answers = await dbContext
            .PracticeExamAnswers.AsNoTracking()
            .Where(a => a.AttemptId == attempt.Id)
            .ToListAsync(cancellationToken);
        var answerIds = answers.Select(a => a.Id).ToList();

        var metCriteria = await dbContext
            .PracticeExamAnswerCriteria.AsNoTracking()
            .Where(c => answerIds.Contains(c.AnswerId))
            .ToListAsync(cancellationToken);

        return new StoredPracticeExamAttempt(attempt, answers, metCriteria);
    }
}
