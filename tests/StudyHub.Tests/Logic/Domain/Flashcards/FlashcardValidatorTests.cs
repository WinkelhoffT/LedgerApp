using StudyHub.Logic.Domain;
using StudyHub.Shared.Flashcards;

namespace StudyHub.Tests.Logic.Domain.Flashcards;

public class FlashcardValidatorTests
{
    private readonly FlashcardValidator _sut = new();

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public void ValidateGenerationOptions_WithCardCountOutOfRange_Throws(int cardCount)
    {
        Assert.Throws<FlashcardValidationException>(() => _sut.ValidateGenerationOptions(cardCount, null));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    public void ValidateGenerationOptions_WithCardCountInRange_DoesNotThrow(int cardCount)
    {
        _sut.ValidateGenerationOptions(cardCount, "definitions");
    }

    [Fact]
    public void ValidateGenerationOptions_WithTooLongFocusHint_Throws()
    {
        var focusHint = new string('x', GenerateFlashcardsRequest.FocusHintMaxLength + 1);

        Assert.Throws<FlashcardValidationException>(() => _sut.ValidateGenerationOptions(10, focusHint));
    }

    [Fact]
    public void FilterGeneratedCards_DropsInvalidCardsAndCapsCount()
    {
        var cards = new List<FlashcardDto>
        {
            new("Q1", "A1", []),
            new("", "A2", []),
            new("Q3", new string('x', FlashcardDto.BackMaxLength + 1), []),
            new("Q4", "A4", []),
            new("Q5", "A5", []),
        };

        var result = _sut.FilterGeneratedCards(cards, maxCount: 2);

        Assert.Equal(["Q1", "Q4"], result.Select(c => c.Front));
    }

    [Fact]
    public void FilterGeneratedCards_TrimsTextAndCleansTags()
    {
        var result = _sut.FilterGeneratedCards([new FlashcardDto("  Q  ", " A ", [" graphs ", "", "Graphs", "dijkstra"])], 10);

        var card = Assert.Single(result);
        Assert.Equal("Q", card.Front);
        Assert.Equal("A", card.Back);
        Assert.Equal(["graphs", "dijkstra"], card.Tags);
    }

    [Fact]
    public void ValidateForExport_WithNoCards_Throws()
    {
        Assert.Throws<FlashcardValidationException>(() => _sut.ValidateForExport([]));
    }

    [Fact]
    public void ValidateForExport_WithTooManyCards_Throws()
    {
        var cards = Enumerable.Range(0, GenerateFlashcardsRequest.MaxCardCount + 1).Select(i => new FlashcardDto($"Q{i}", "A", [])).ToList();

        Assert.Throws<FlashcardValidationException>(() => _sut.ValidateForExport(cards));
    }

    [Fact]
    public void ValidateForExport_WithEmptyBack_ThrowsWithCardNumber()
    {
        var ex = Assert.Throws<FlashcardValidationException>(
            () => _sut.ValidateForExport([new FlashcardDto("Q1", "A1", []), new FlashcardDto("Q2", "  ", [])]));

        Assert.StartsWith("Card 2:", ex.Message);
    }

    [Fact]
    public void ValidateForExport_WithTooLongTag_Throws()
    {
        var tag = new string('t', FlashcardDto.TagMaxLength + 1);

        Assert.Throws<FlashcardValidationException>(() => _sut.ValidateForExport([new FlashcardDto("Q", "A", [tag])]));
    }

    [Fact]
    public void ValidateForExport_WithValidCards_ReturnsNormalizedCards()
    {
        var result = _sut.ValidateForExport([new FlashcardDto(" Q ", "A", ["x"])]);

        Assert.Equal("Q", Assert.Single(result).Front);
    }
}
