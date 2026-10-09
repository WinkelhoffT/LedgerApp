using StudyHub.Data.Contract;
using StudyHub.Logic.Business.Contract;
using StudyHub.Logic.Domain.Contract;
using StudyHub.Shared.PracticeExams;

namespace StudyHub.Logic.Business;

public sealed class PracticeExamAttemptOrchestrator(
    IPracticeExamRepository practiceExamRepository,
    IPracticeExamAttemptRepository attemptRepository,
    IPracticeExamAttemptProcessor attemptProcessor,
    INoteRepository noteRepository,
    IFlashcardRepository flashcardRepository
) : IPracticeExamAttemptOrchestrator
{
    public async Task<PracticeExamSheetDto> StartAsync(
        Guid examId,
        StartPracticeExamAttemptRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var exam = await GetExistingExamAsync(examId, cancellationToken);
        var openAttempt = await attemptRepository.GetOpenAttemptAsync(examId, cancellationToken);

        var outcome = attemptProcessor.Start(exam, openAttempt, request.WithTimeLimit);
        if (outcome.IsNew)
        {
            await attemptRepository.AddAsync(outcome.Attempt, cancellationToken);
            await attemptRepository.SaveChangesAsync(cancellationToken);
        }

        return PracticeExamMapper.ToSheetDto(exam, outcome.Attempt);
    }

    public async Task<PracticeExamSheetDto> GetSheetAsync(
        Guid attemptId,
        CancellationToken cancellationToken = default
    )
    {
        var attempt = await GetExistingAttemptAsync(attemptId, cancellationToken);
        attemptProcessor.EnsureInProgress(attempt.Attempt);

        var exam = await GetExistingExamAsync(attempt.Attempt.ExamId, cancellationToken);
        return PracticeExamMapper.ToSheetDto(exam, attempt);
    }

    public async Task SaveAnswerAsync(
        Guid attemptId,
        Guid taskId,
        SavePracticeExamAnswerRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var attempt = await GetExistingAttemptAsync(attemptId, cancellationToken);
        var exam = await GetExistingExamAsync(attempt.Attempt.ExamId, cancellationToken);

        var answer = attemptProcessor.SaveAnswer(
            exam,
            attempt,
            taskId,
            request.SelectedOptionId,
            request.AnswerText
        );

        attemptRepository.UpdateAnswer(answer);
        await attemptRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<PracticeExamReviewDto> SubmitAsync(
        Guid attemptId,
        CancellationToken cancellationToken = default
    )
    {
        var attempt = await GetExistingAttemptAsync(attemptId, cancellationToken);
        var exam = await GetExistingExamAsync(attempt.Attempt.ExamId, cancellationToken);

        var submitted = attemptProcessor.Submit(exam, attempt);
        if (!ReferenceEquals(submitted, attempt))
        {
            attemptRepository.Update(submitted.Attempt);
            foreach (var answer in submitted.Answers)
            {
                attemptRepository.UpdateAnswer(answer);
            }

            await attemptRepository.SaveChangesAsync(cancellationToken);
        }

        return await ToReviewDtoAsync(exam, submitted, cancellationToken);
    }

    public async Task<PracticeExamReviewDto> GetReviewAsync(
        Guid attemptId,
        CancellationToken cancellationToken = default
    )
    {
        var attempt = await GetExistingAttemptAsync(attemptId, cancellationToken);
        attemptProcessor.EnsureSubmitted(attempt.Attempt);

        var exam = await GetExistingExamAsync(attempt.Attempt.ExamId, cancellationToken);
        return await ToReviewDtoAsync(exam, attempt, cancellationToken);
    }

    public async Task<PracticeExamReviewDto> GradeAsync(
        Guid attemptId,
        Guid taskId,
        GradePracticeExamAnswerRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var attempt = await GetExistingAttemptAsync(attemptId, cancellationToken);
        var exam = await GetExistingExamAsync(attempt.Attempt.ExamId, cancellationToken);

        var graded = attemptProcessor.Grade(exam, attempt, taskId, request.MetCriterionIds ?? []);
        var answer = graded.Answers.Single(a => a.TaskId == taskId);

        attemptRepository.Update(graded.Attempt);
        attemptRepository.UpdateAnswer(answer);
        await attemptRepository.ReplaceMetCriteriaAsync(
            answer.Id,
            graded
                .MetCriteria.Where(c => c.AnswerId == answer.Id)
                .Select(c => c.CriterionId)
                .ToList(),
            cancellationToken
        );
        await attemptRepository.SaveChangesAsync(cancellationToken);

        return await ToReviewDtoAsync(exam, graded, cancellationToken);
    }

    private async Task<StoredPracticeExam> GetExistingExamAsync(
        Guid examId,
        CancellationToken cancellationToken
    ) =>
        await practiceExamRepository.GetWithTasksAsync(examId, cancellationToken)
        ?? throw new PracticeExamNotFoundException(examId);

    private async Task<StoredPracticeExamAttempt> GetExistingAttemptAsync(
        Guid attemptId,
        CancellationToken cancellationToken
    ) =>
        await attemptRepository.GetWithAnswersAsync(attemptId, cancellationToken)
        ?? throw new PracticeExamAttemptNotFoundException(attemptId);

    private async Task<PracticeExamReviewDto> ToReviewDtoAsync(
        StoredPracticeExam exam,
        StoredPracticeExamAttempt attempt,
        CancellationToken cancellationToken
    )
    {
        var noteIds = exam
            .Tasks.Select(t => t.Task.SourceNoteId)
            .OfType<Guid>()
            .Distinct()
            .ToList();
        var cardIds = exam
            .Tasks.Select(t => t.Task.SourceFlashcardId)
            .OfType<Guid>()
            .Distinct()
            .ToList();

        var notes = await noteRepository.GetByIdsAsync(noteIds, cancellationToken);
        var cards = await flashcardRepository.GetByIdsAsync(cardIds, cancellationToken);

        return PracticeExamMapper.ToReviewDto(
            exam,
            attempt,
            attemptProcessor.GetStatus(attempt.Attempt),
            attemptProcessor.GetPercent(attempt.Attempt),
            notes.ToDictionary(n => n.Id),
            cards.ToDictionary(c => c.Id)
        );
    }
}
