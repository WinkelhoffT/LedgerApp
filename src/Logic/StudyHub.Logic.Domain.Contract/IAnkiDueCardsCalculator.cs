using StudyHub.Shared.Anki;

namespace StudyHub.Logic.Domain.Contract;

public interface IAnkiDueCardsCalculator
{
    AnkiDueCards Calculate(IReadOnlyList<AnkiDeckCountsDto> deckCounts);
}
