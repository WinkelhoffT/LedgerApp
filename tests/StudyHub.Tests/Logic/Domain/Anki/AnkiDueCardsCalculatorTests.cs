using StudyHub.Logic.Domain;
using StudyHub.Shared.Anki;

namespace StudyHub.Tests.Logic.Domain.Anki;

public class AnkiDueCardsCalculatorTests
{
    private readonly AnkiDueCardsCalculator _sut = new();

    [Fact]
    public void Calculate_DoesNotDoubleCountSubdecks()
    {
        var result = _sut.Calculate(
        [
            new("Informatik", NewCount: 10, LearnCount: 2, ReviewCount: 30),
            new("Informatik::Algorithmen", NewCount: 4, LearnCount: 1, ReviewCount: 10),
            new("Informatik::Algorithmen::Graphen", NewCount: 1, LearnCount: 0, ReviewCount: 3),
        ]);

        Assert.Equal(10, result.NewCount);
        Assert.Equal(2, result.LearnCount);
        Assert.Equal(30, result.ReviewCount);
        Assert.Equal(["Informatik"], result.TopLevelDecks.Select(deck => deck.DeckName));
    }

    [Fact]
    public void Calculate_SumsTopLevelDecks_AndOrdersThemByDueCardsDescending()
    {
        var result = _sut.Calculate(
        [
            new("Mathe", NewCount: 1, LearnCount: 0, ReviewCount: 2),
            new("Informatik", NewCount: 10, LearnCount: 2, ReviewCount: 30),
            new("Englisch", NewCount: 0, LearnCount: 0, ReviewCount: 0),
        ]);

        Assert.Equal(11, result.NewCount);
        Assert.Equal(2, result.LearnCount);
        Assert.Equal(32, result.ReviewCount);
        Assert.Equal(["Informatik", "Mathe", "Englisch"], result.TopLevelDecks.Select(deck => deck.DeckName));
    }

    [Theory]
    [InlineData(1, 0, 0)]
    [InlineData(0, 1, 0)]
    [InlineData(0, 0, 1)]
    public void Calculate_WithAnyDueCard_HasCardsToStudy(int newCount, int learnCount, int reviewCount)
    {
        var result = _sut.Calculate([new("Default", newCount, learnCount, reviewCount)]);

        Assert.True(result.HasCardsToStudy);
    }

    [Fact]
    public void Calculate_WithNothingDue_HasNoCardsToStudy()
    {
        var result = _sut.Calculate([new("Default", NewCount: 0, LearnCount: 0, ReviewCount: 0)]);

        Assert.False(result.HasCardsToStudy);
    }

    [Fact]
    public void Calculate_WithNoDecks_ReturnsZeroCounts()
    {
        var result = _sut.Calculate([]);

        Assert.Equal(0, result.NewCount + result.LearnCount + result.ReviewCount);
        Assert.False(result.HasCardsToStudy);
        Assert.Empty(result.TopLevelDecks);
    }
}
