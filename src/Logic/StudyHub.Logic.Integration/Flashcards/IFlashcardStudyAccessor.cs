using StudyHub.Shared.Flashcards;

namespace StudyHub.Logic.Integration.Flashcards;

/// <summary>Narrow HTTP access to StudyHub.Api's study endpoints, covering what the study page calls.</summary>
public interface IFlashcardStudyAccessor
{
    /// <returns>The next card, or <c>null</c> when the deck is finished for now.</returns>
    Task<StudyCardDto?> GetNextAsync(Guid deckId, CancellationToken cancellationToken = default);

    /// <returns>The next card after this answer, or <c>null</c> when the deck is finished for now.</returns>
    Task<StudyCardDto?> AnswerAsync(
        AnswerFlashcardRequest request,
        CancellationToken cancellationToken = default
    );
}
