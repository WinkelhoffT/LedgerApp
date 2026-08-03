using StudyHub.Logic.Business.Notes.Commands;

namespace StudyHub.Tests.Logic.Business.Notes.Commands;

public class SlashCommandRegistryTests
{
    private readonly SlashCommandRegistry _sut = new(
    [
        new HeadingSlashCommand(1),
        new HeadingSlashCommand(2),
        new HeadingSlashCommand(3),
        new QuoteSlashCommand(),
        new DividerSlashCommand(),
        new BulletListSlashCommand(),
        new NumberedListSlashCommand(),
        new TaskListSlashCommand(),
        new CodeBlockSlashCommand(),
        new TableSlashCommand(),
    ]);

    [Fact]
    public void Search_WithEmptyQuery_ReturnsEveryCommand()
    {
        Assert.Equal(10, _sut.Search(string.Empty).Count);
    }

    [Fact]
    public void Search_WithHea_ReturnsAllThreeHeadings()
    {
        var results = _sut.Search("hea");

        Assert.Equal(3, results.Count);
        Assert.All(results, command => Assert.StartsWith("Heading", command.Title));
    }

    [Fact]
    public void Search_WithDiv_ReturnsOnlyDivider()
    {
        var results = _sut.Search("div");

        Assert.Single(results);
        Assert.Equal("Divider", results[0].Title);
    }

    [Fact]
    public void Search_WithCo_ReturnsOnlyCodeBlock()
    {
        var results = _sut.Search("co");

        Assert.Single(results);
        Assert.Equal("Code Block", results[0].Title);
    }

    [Fact]
    public void Search_WithNoMatch_ReturnsEmpty()
    {
        Assert.Empty(_sut.Search("xyz"));
    }

    [Fact]
    public void Search_IsCaseInsensitive()
    {
        Assert.Single(_sut.Search("DIV"));
    }
}
