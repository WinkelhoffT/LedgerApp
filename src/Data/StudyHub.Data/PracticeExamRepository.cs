using Microsoft.EntityFrameworkCore;
using StudyHub.Data.Contract;
using StudyHub.Shared.PracticeExams;

namespace StudyHub.Data;

public sealed class PracticeExamRepository(ApplicationDbContext dbContext) : IPracticeExamRepository
{
    public async Task<IReadOnlyList<PracticeExam>> GetAllAsync(
        CancellationToken cancellationToken = default
    ) =>
        await dbContext
            .PracticeExams.AsNoTracking()
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<PracticeExam?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    ) =>
        dbContext
            .PracticeExams.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<StoredPracticeExam?> GetWithTasksAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        var exam = await GetByIdAsync(id, cancellationToken);
        if (exam is null)
        {
            return null;
        }

        var tasks = await dbContext
            .PracticeExamTasks.AsNoTracking()
            .Where(t => t.ExamId == id)
            .OrderBy(t => t.Position)
            .ToListAsync(cancellationToken);
        var taskIds = tasks.Select(t => t.Id).ToList();

        var options = (
            await dbContext
                .PracticeExamOptions.AsNoTracking()
                .Where(o => taskIds.Contains(o.TaskId))
                .ToListAsync(cancellationToken)
        ).ToLookup(o => o.TaskId);
        var criteria = (
            await dbContext
                .PracticeExamCriteria.AsNoTracking()
                .Where(c => taskIds.Contains(c.TaskId))
                .ToListAsync(cancellationToken)
        ).ToLookup(c => c.TaskId);

        return new StoredPracticeExam(
            exam,
            tasks
                .Select(task => new StoredPracticeExamTask(
                    task,
                    options[task.Id].OrderBy(o => o.Position).ToList(),
                    criteria[task.Id].OrderBy(c => c.Position).ToList()
                ))
                .ToList()
        );
    }

    public async Task<IReadOnlyList<PracticeExamTaskTotals>> GetTaskTotalsAsync(
        Guid? examId = null,
        CancellationToken cancellationToken = default
    ) =>
        await dbContext
            .PracticeExamTasks.AsNoTracking()
            .Where(t => examId == null || t.ExamId == examId)
            .GroupBy(t => t.ExamId)
            .Select(g => new PracticeExamTaskTotals(
                g.Key,
                g.Count(t => !t.IsExcluded),
                g.Where(t => !t.IsExcluded).Sum(t => t.Points),
                g.Count(t => t.IsExcluded)
            ))
            .ToListAsync(cancellationToken);

    public Task<PracticeExamTask?> GetTaskByIdAsync(
        Guid taskId,
        CancellationToken cancellationToken = default
    ) =>
        dbContext
            .PracticeExamTasks.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);

    public async Task AddAsync(
        StoredPracticeExam exam,
        CancellationToken cancellationToken = default
    )
    {
        await dbContext.PracticeExams.AddAsync(exam.Exam, cancellationToken);
        await dbContext.PracticeExamTasks.AddRangeAsync(
            exam.Tasks.Select(t => t.Task),
            cancellationToken
        );
        await dbContext.PracticeExamOptions.AddRangeAsync(
            exam.Tasks.SelectMany(t => t.Options),
            cancellationToken
        );
        await dbContext.PracticeExamCriteria.AddRangeAsync(
            exam.Tasks.SelectMany(t => t.Criteria),
            cancellationToken
        );
    }

    public void Update(PracticeExam exam) => dbContext.PracticeExams.Update(exam);

    public void UpdateTask(PracticeExamTask task) => dbContext.PracticeExamTasks.Update(task);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
