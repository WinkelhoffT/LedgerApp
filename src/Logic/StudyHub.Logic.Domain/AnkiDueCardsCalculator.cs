using StudyHub.Logic.Domain.Contract;
using StudyHub.Shared.Anki;

namespace StudyHub.Logic.Domain;

public sealed class AnkiDueCardsCalculator : IAnkiDueCardsCalculator
{
    private const string SubdeckSeparator = "::";

    /// <summary>
    /// Sums only top-level decks, because Anki's counts for a parent deck already include its
    /// subdecks (<c>Informatik::Algorithmen</c> is part of <c>Informatik</c>).
    /// </summary>
    public AnkiDueCards Calculate(IReadOnlyList<AnkiDeckCountsDto> deckCounts)
    {
        var topLevelDecks = deckCounts
            .Where(deck => !deck.DeckName.Contains(SubdeckSeparator, StringComparison.Ordinal))
            .OrderByDescending(deck => deck.NewCount + deck.LearnCount + deck.ReviewCount)
            .ThenBy(deck => deck.DeckName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        var newCount = topLevelDecks.Sum(deck => deck.NewCount);
        var learnCount = topLevelDecks.Sum(deck => deck.LearnCount);
        var reviewCount = topLevelDecks.Sum(deck => deck.ReviewCount);

        return new AnkiDueCards(
            newCount,
            learnCount,
            reviewCount,
            HasCardsToStudy: newCount + learnCount + reviewCount > 0,
            topLevelDecks);
    }
}
