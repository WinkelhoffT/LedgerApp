using StudyHub.Data.Contract;
using StudyHub.Logic.Business.Contract;
using StudyHub.Logic.Domain.Contract;
using StudyHub.Logic.Integration.Ai;
using StudyHub.Shared.Ai;
using StudyHub.Shared.PracticeExams;

namespace StudyHub.Logic.Business;

public sealed class PracticeExamGenerationOrchestrator(
    IPracticeExamValidator practiceExamValidator,
    IPracticeExamMaterialProvider materialProvider,
    IPracticeExamGenerator practiceExamGenerator,
    IAiModelCatalog aiModelCatalog,
    IPracticeExamLifecycle practiceExamLifecycle,
    IPracticeExamRepository practiceExamRepository
) : IPracticeExamGenerationOrchestrator
{
    public IReadOnlyList<AiModelDto> GetAvailableModels() => aiModelCatalog.GetModels();

    public async Task<PracticeExamDto> GenerateAsync(
        GeneratePracticeExamRequest request,
        CancellationToken cancellationToken = default
    )
    {
        practiceExamValidator.ValidateGenerationOptions(
            request.Level,
            request.DurationMinutes,
            request.FocusHint
        );

        var model = string.IsNullOrWhiteSpace(request.Model)
            ? aiModelCatalog.DefaultModelId
            : request.Model.Trim();
        if (!aiModelCatalog.IsAvailable(model))
        {
            throw new PracticeExamValidationException($"The model '{model}' is not available.");
        }

        var material = await materialProvider.GetMaterialAsync(request, cancellationToken);

        var generatedTasks = await practiceExamGenerator.GenerateAsync(
            new PracticeExamGenerationInput(
                model,
                request.Level,
                request.DurationMinutes,
                request.SourceKind,
                material.SourceName,
                material.Sources,
                request.FocusHint
            ),
            cancellationToken
        );

        var tasks = practiceExamValidator.FilterGeneratedTasks(
            generatedTasks,
            material.Sources.Select(source => source.Id).ToList()
        );
        if (tasks.Count == 0)
        {
            throw new AiGenerationFailedException(
                AiGenerationFailureReason.InvalidResponse,
                "Claude did not return any usable tasks for this material."
            );
        }

        var exam = practiceExamLifecycle.Create(
            request,
            model,
            practiceExamGenerator.PromptVersion,
            material,
            tasks
        );

        await practiceExamRepository.AddAsync(exam, cancellationToken);
        await practiceExamRepository.SaveChangesAsync(cancellationToken);

        return PracticeExamMapper.ToDto(
            exam.Exam,
            material.SourceName,
            PracticeExamMapper.ToTotals(exam),
            attemptCount: 0,
            bestAttempt: null,
            openAttemptId: null
        );
    }
}
