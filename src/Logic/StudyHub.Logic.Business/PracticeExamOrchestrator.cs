using StudyHub.Data.Contract;
using StudyHub.Logic.Business.Contract;
using StudyHub.Logic.Domain.Contract;
using StudyHub.Shared.PracticeExams;

namespace StudyHub.Logic.Business;

public sealed class PracticeExamOrchestrator(
    IPracticeExamRepository practiceExamRepository,
    IPracticeExamAttemptRepository attemptRepository,
    ICourseRepository courseRepository,
    IFlashcardDeckRepository deckRepository,
    IPracticeExamLifecycle practiceExamLifecycle,
    IPracticeExamAttemptProcessor attemptProcessor
) : IPracticeExamOrchestrator
{
    public async Task<IReadOnlyList<PracticeExamDto>> GetAllAsync(
        bool includeArchived,
        CancellationToken cancellationToken = default
    )
    {
        var exams = (await practiceExamRepository.GetAllAsync(cancellationToken))
            .Where(e => includeArchived || !e.IsArchived)
            .ToList();
        if (exams.Count == 0)
        {
            return [];
        }

        var totals = (
            await practiceExamRepository.GetTaskTotalsAsync(cancellationToken: cancellationToken)
        ).ToDictionary(t => t.ExamId);
        var attempts = (
            await attemptRepository.GetByExamIdsAsync(
                exams.Select(e => e.Id).ToList(),
                cancellationToken
            )
        ).ToLookup(a => a.ExamId);
        var courseNames = (await courseRepository.GetAllAsync(cancellationToken)).ToDictionary(
            c => c.Id,
            c => c.Name
        );
        var deckNames = (await deckRepository.GetAllAsync(cancellationToken)).ToDictionary(
            d => d.Id,
            d => d.Name
        );

        return exams
            .Select(exam =>
                ToDto(
                    exam,
                    GetSourceName(exam, courseNames, deckNames),
                    totals.GetValueOrDefault(exam.Id),
                    attempts[exam.Id].ToList()
                )
            )
            .ToList();
    }

    public async Task<PracticeExamDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    ) => await ToDtoAsync(await GetExistingExamAsync(id, cancellationToken), cancellationToken);

    public async Task<IReadOnlyList<PracticeExamAttemptSummaryDto>> GetAttemptsAsync(
        Guid examId,
        CancellationToken cancellationToken = default
    )
    {
        var exam = await GetExistingExamAsync(examId, cancellationToken);
        var attempts = await attemptRepository.GetByExamIdsAsync([exam.Id], cancellationToken);

        return attempts.Select(ToSummaryDto).ToList();
    }

    public async Task<PracticeExamDto> ArchiveAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        var exam = practiceExamLifecycle.Archive(await GetExistingExamAsync(id, cancellationToken));

        practiceExamRepository.Update(exam);
        await practiceExamRepository.SaveChangesAsync(cancellationToken);

        return await ToDtoAsync(exam, cancellationToken);
    }

    public async Task<PracticeExamDto> RestoreAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        var exam = practiceExamLifecycle.Restore(await GetExistingExamAsync(id, cancellationToken));

        practiceExamRepository.Update(exam);
        await practiceExamRepository.SaveChangesAsync(cancellationToken);

        return await ToDtoAsync(exam, cancellationToken);
    }

    public async Task<PracticeExamDto> ExcludeTaskAsync(
        Guid taskId,
        CancellationToken cancellationToken = default
    )
    {
        var task =
            await practiceExamRepository.GetTaskByIdAsync(taskId, cancellationToken)
            ?? throw new PracticeExamTaskNotFoundException(taskId);
        var exam = await GetExistingExamAsync(task.ExamId, cancellationToken);

        practiceExamRepository.UpdateTask(practiceExamLifecycle.ExcludeTask(exam, task));
        await practiceExamRepository.SaveChangesAsync(cancellationToken);

        return await ToDtoAsync(exam, cancellationToken);
    }

    private async Task<PracticeExam> GetExistingExamAsync(
        Guid id,
        CancellationToken cancellationToken
    ) =>
        await practiceExamRepository.GetByIdAsync(id, cancellationToken)
        ?? throw new PracticeExamNotFoundException(id);

    private async Task<PracticeExamDto> ToDtoAsync(
        PracticeExam exam,
        CancellationToken cancellationToken
    )
    {
        var totals = await practiceExamRepository.GetTaskTotalsAsync(exam.Id, cancellationToken);
        var attempts = await attemptRepository.GetByExamIdsAsync([exam.Id], cancellationToken);
        var sourceName = exam.SourceKind switch
        {
            PracticeExamSourceKind.Course when exam.CourseId is { } courseId => (
                await courseRepository.GetByIdAsync(courseId, cancellationToken)
            )?.Name,
            PracticeExamSourceKind.Deck when exam.DeckId is { } deckId => (
                await deckRepository.GetByIdAsync(deckId, cancellationToken)
            )?.Name,
            _ => null,
        };

        return ToDto(
            exam,
            sourceName ?? UnknownSourceName(exam),
            totals.FirstOrDefault(),
            attempts
        );
    }

    private PracticeExamDto ToDto(
        PracticeExam exam,
        string sourceName,
        PracticeExamTaskTotals? totals,
        IReadOnlyList<PracticeExamAttempt> attempts
    )
    {
        var best = attemptProcessor.SelectBest(attempts);

        return PracticeExamMapper.ToDto(
            exam,
            sourceName,
            totals,
            attempts.Count,
            best is null ? null : ToSummaryDto(best),
            attempts.FirstOrDefault(a => a.SubmittedAt is null)?.Id
        );
    }

    private PracticeExamAttemptSummaryDto ToSummaryDto(PracticeExamAttempt attempt) =>
        PracticeExamMapper.ToSummaryDto(
            attempt,
            attemptProcessor.GetStatus(attempt),
            attemptProcessor.GetPercent(attempt)
        );

    private static string GetSourceName(
        PracticeExam exam,
        IReadOnlyDictionary<Guid, string> courseNames,
        IReadOnlyDictionary<Guid, string> deckNames
    ) =>
        (
            exam.SourceKind == PracticeExamSourceKind.Course
                ? exam.CourseId is { } courseId
                    ? courseNames.GetValueOrDefault(courseId)
                    : null
                : exam.DeckId is { } deckId
                    ? deckNames.GetValueOrDefault(deckId)
                    : null
        ) ?? UnknownSourceName(exam);

    private static string UnknownSourceName(PracticeExam exam) =>
        exam.SourceKind == PracticeExamSourceKind.Course ? "Unknown course" : "Unknown deck";
}
