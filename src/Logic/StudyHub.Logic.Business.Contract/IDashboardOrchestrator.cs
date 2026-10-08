using StudyHub.Shared.Dashboard;
using StudyHub.Shared.Flashcards;

namespace StudyHub.Logic.Business.Contract;

public interface IDashboardOrchestrator
{
    Task<SemesterProgressDto> GetSemesterProgressAsync(CancellationToken cancellationToken = default);

    Task<FlashcardsDueDto> GetFlashcardsDueAsync(CancellationToken cancellationToken = default);
}
