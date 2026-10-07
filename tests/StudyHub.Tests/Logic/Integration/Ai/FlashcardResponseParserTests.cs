using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StudyHub.Logic.Integration.Ai;
using StudyHub.Shared.Configuration;
using StudyHub.Shared.Flashcards;

namespace StudyHub.Tests.Logic.Integration.Ai;

public class FlashcardResponseParserTests
{
    [Fact]
    public void Parse_WithValidJson_MapsCards()
    {
        const string json = """
            {"cards":[{"front":"Was ist O(1)?","back":"Konstante Laufzeit.","tags":["komplexitaet"]},{"front":"F2","back":"B2","tags":[]}]}
            """;

        var cards = FlashcardResponseParser.Parse("end_turn", json);

        Assert.Equal(2, cards.Count);
        Assert.Equal("Was ist O(1)?", cards[0].Front);
        Assert.Equal("Konstante Laufzeit.", cards[0].Back);
        Assert.Equal(["komplexitaet"], cards[0].Tags);
        Assert.Equal("B2", cards[1].Back);
    }

    [Fact]
    public void Parse_WithRefusal_ThrowsRefused()
    {
        var ex = Assert.Throws<FlashcardGenerationFailedException>(() => FlashcardResponseParser.Parse("refusal", null));

        Assert.Equal(FlashcardGenerationFailureReason.Refused, ex.Reason);
    }

    [Fact]
    public void Parse_WithMaxTokens_ThrowsTruncated()
    {
        var ex = Assert.Throws<FlashcardGenerationFailedException>(() => FlashcardResponseParser.Parse("max_tokens", "{\"cards\":[{\"fr"));

        Assert.Equal(FlashcardGenerationFailureReason.Truncated, ex.Reason);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("{\"something\":1}")]
    public void Parse_WithUnusableText_ThrowsInvalidResponse(string? text)
    {
        var ex = Assert.Throws<FlashcardGenerationFailedException>(() => FlashcardResponseParser.Parse("end_turn", text));

        Assert.Equal(FlashcardGenerationFailureReason.InvalidResponse, ex.Reason);
    }

    [Fact]
    public void Parse_WithMissingFields_MapsToEmptyValuesForTheValidatorToDrop()
    {
        var cards = FlashcardResponseParser.Parse("end_turn", "{\"cards\":[{\"front\":\"Q\"}]}");

        var card = Assert.Single(cards);
        Assert.Equal("", card.Back);
        Assert.Empty(card.Tags);
    }

    [Fact]
    public async Task Generator_WithoutApiKey_ThrowsAiNotConfigured()
    {
        using var generator = new ClaudeFlashcardGenerator(
            Options.Create(new AnthropicOptions { ApiKey = null }),
            NullLogger<ClaudeFlashcardGenerator>.Instance);

        await Assert.ThrowsAsync<AiNotConfiguredException>(
            () => generator.GenerateAsync(new FlashcardGenerationInput("claude-sonnet-5-5", "Title", "Content", 5, null)));
    }
}
