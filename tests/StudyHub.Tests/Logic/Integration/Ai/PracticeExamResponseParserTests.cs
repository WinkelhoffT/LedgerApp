using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StudyHub.Logic.Integration.Ai;
using StudyHub.Shared.Ai;
using StudyHub.Shared.PracticeExams;

namespace StudyHub.Tests.Logic.Integration.Ai;

public class PracticeExamResponseParserTests
{
    [Fact]
    public void Parse_WithValidJson_MapsBothTaskKinds()
    {
        const string json = """
            {"tasks":[
              {"kind":"single_choice","text":"Welche Laufzeit?","points":2,
               "options":[{"text":"O(n)","isCorrect":false,"rationale":"Zu klein."},{"text":"O(n log n)","isCorrect":true,"rationale":"Richtig."}],
               "criteria":[],"solution":"Erklärung","sourceId":3},
              {"kind":"open","text":"Erklären Sie Dijkstra.","points":6,"options":[],
               "criteria":[{"description":"Idee","points":2},{"description":"Korrektheit","points":4}],
               "solution":"Musterlösung","sourceId":1}
            ]}
            """;

        var tasks = PracticeExamResponseParser.Parse("end_turn", json);

        Assert.Equal(2, tasks.Count);
        Assert.Equal(PracticeExamTaskKind.SingleChoice, tasks[0].Kind);
        Assert.Equal("Welche Laufzeit?", tasks[0].Text);
        Assert.Equal(2, tasks[0].Points);
        Assert.Equal(
            new GeneratedPracticeExamOption("O(n log n)", true, "Richtig."),
            tasks[0].Options[1]
        );
        Assert.Equal(3, tasks[0].SourceId);
        Assert.Equal(PracticeExamTaskKind.Open, tasks[1].Kind);
        Assert.Equal(
            [new GeneratedPracticeExamCriterion("Idee", 2), new("Korrektheit", 4)],
            tasks[1].Criteria
        );
        Assert.Equal("Musterlösung", tasks[1].Solution);
    }

    [Fact]
    public void Parse_WithRefusal_ThrowsRefused()
    {
        var ex = Assert.Throws<AiGenerationFailedException>(() =>
            PracticeExamResponseParser.Parse("refusal", null)
        );

        Assert.Equal(AiGenerationFailureReason.Refused, ex.Reason);
    }

    [Fact]
    public void Parse_WithMaxTokens_ThrowsTruncated()
    {
        var ex = Assert.Throws<AiGenerationFailedException>(() =>
            PracticeExamResponseParser.Parse("max_tokens", "{\"tasks\":[{\"kind\":\"op")
        );

        Assert.Equal(AiGenerationFailureReason.Truncated, ex.Reason);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("{\"cards\":[]}")]
    public void Parse_WithUnusableText_ThrowsInvalidResponse(string? text)
    {
        var ex = Assert.Throws<AiGenerationFailedException>(() =>
            PracticeExamResponseParser.Parse("end_turn", text)
        );

        Assert.Equal(AiGenerationFailureReason.InvalidResponse, ex.Reason);
    }

    [Fact]
    public void Parse_WithMissingFieldsOrUnknownKind_MapsToValuesTheValidatorDrops()
    {
        var tasks = PracticeExamResponseParser.Parse(
            "end_turn",
            "{\"tasks\":[{\"kind\":\"essay\",\"text\":\"Q\"},null,{\"kind\":\"open\"}]}"
        );

        Assert.Equal(2, tasks.Count);
        Assert.Null(tasks[0].Kind);
        Assert.Equal("Q", tasks[0].Text);
        Assert.Equal(PracticeExamTaskKind.Open, tasks[1].Kind);
        Assert.Equal("", tasks[1].Text);
        Assert.Equal(0, tasks[1].Points);
        Assert.Empty(tasks[1].Criteria);
        Assert.Null(tasks[1].SourceId);
    }

    [Fact]
    public async Task Generator_WithoutApiKey_ThrowsAiNotConfigured()
    {
        await using var services = new ServiceCollection()
            .AddLogging()
            .AddStudyHubAi(new ConfigurationBuilder().Build())
            .BuildServiceProvider();
        var generator = services.GetRequiredService<IPracticeExamGenerator>();

        Assert.Equal(PracticeExamPrompt.Version, generator.PromptVersion);
        await Assert.ThrowsAsync<AiNotConfiguredException>(() =>
            generator.GenerateAsync(
                new PracticeExamGenerationInput(
                    "claude-sonnet-5-5",
                    PracticeExamLevel.University,
                    60,
                    PracticeExamSourceKind.Course,
                    "Algorithmen",
                    [new PracticeExamSource(1, "Dijkstra", "# Dijkstra", Guid.NewGuid(), null)],
                    null
                )
            )
        );
    }
}
