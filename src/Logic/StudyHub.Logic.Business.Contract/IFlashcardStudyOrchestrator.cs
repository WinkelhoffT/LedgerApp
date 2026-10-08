using StudyHub.Shared.Flashcards;

namespace StudyHub.Logic.Business.Contract;

/// <summary>A study session without server-side session state: every call picks the next card from the database.</summary>
public interface IFlashcardStudyOrchestrator
{
    /// <returns>The next card, or <c>null</c> when the deck is finished for now.</returns>
    Task<StudyCardDto?> GetNextAsync(Guid deckId, CancellationToken cancellationToken = default);

    /// <returns>The next card after this answer, or <c>null</c> when the deck is finished for now.</returns>
    Task<StudyCardDto?> AnswerAsync(AnswerFlashcardRequest request, CancellationToken cancellationToken = default);
}
