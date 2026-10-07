using Microsoft.Extensions.Options;
using StudyHub.Logic.Integration.Ai;
using StudyHub.Shared.Configuration;

namespace StudyHub.Tests.Logic.Integration.Ai;

public class ConfiguredAiModelCatalogTests
{
    private static ConfiguredAiModelCatalog Create(
        string defaultModel,
        params AnthropicModelOption[] models
    ) =>
        new(
            Options.Create(
                new AnthropicOptions { DefaultModel = defaultModel, Models = [.. models] }
            )
        );

    [Fact]
    public void GetModels_ReturnsConfiguredModelsAndMarksDefault()
    {
        var sut = Create(
            "claude-sonnet-5-5",
            new AnthropicModelOption { Id = "claude-sonnet-5-5", DisplayName = "Sonnet" },
            new AnthropicModelOption { Id = "claude-opus-5-5", DisplayName = "Opus" }
        );

        var models = sut.GetModels();

        Assert.Equal(["claude-sonnet-5-5", "claude-opus-5-5"], models.Select(m => m.Id));
        Assert.Equal([true, false], models.Select(m => m.IsDefault));
        Assert.Equal("Sonnet", models[0].DisplayName);
    }

    [Fact]
    public void GetModels_WithoutConfiguredList_ContainsOnlyDefault()
    {
        var model = Assert.Single(Create("claude-sonnet-5-5").GetModels());

        Assert.Equal("claude-sonnet-5-5", model.Id);
        Assert.Equal("claude-sonnet-5-5", model.DisplayName);
        Assert.True(model.IsDefault);
    }

    [Fact]
    public void GetModels_DefaultMissingFromList_IsAddedFirst()
    {
        var models = Create(
                "claude-sonnet-5-5",
                new AnthropicModelOption { Id = "claude-opus-5-5" }
            )
            .GetModels();

        Assert.Equal(["claude-sonnet-5-5", "claude-opus-5-5"], models.Select(m => m.Id));
    }

    [Fact]
    public void GetModels_SkipsEmptyAndDuplicateEntries()
    {
        var models = Create(
                "claude-sonnet-5-5",
                new AnthropicModelOption { Id = "claude-sonnet-5-5" },
                new AnthropicModelOption { Id = " " },
                new AnthropicModelOption { Id = "claude-sonnet-5-5", DisplayName = "Duplicate" }
            )
            .GetModels();

        Assert.Single(models);
    }

    [Fact]
    public void IsAvailable_OnlyForListedModels()
    {
        var sut = Create("claude-sonnet-5-5", new AnthropicModelOption { Id = "claude-opus-5-5" });

        Assert.True(sut.IsAvailable("claude-opus-5-5"));
        Assert.True(sut.IsAvailable("claude-sonnet-5-5"));
        Assert.False(sut.IsAvailable("claude-haiku-4-5"));
    }
}
