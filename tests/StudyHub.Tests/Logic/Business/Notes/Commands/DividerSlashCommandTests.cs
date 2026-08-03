using StudyHub.Logic.Business.Notes.Commands;

namespace StudyHub.Tests.Logic.Business.Notes.Commands;

public class DividerSlashCommandTests
{
    private readonly DividerSlashCommand _sut = new();

    [Fact]
    public void Apply_AtStartOfBlock_DoesNotPrependBlankLines()
    {
        var result = _sut.Apply(precedingText: string.Empty);

        Assert.Equal("---\n\n\n", result.Text);
        Assert.Equal(result.Text.Length, result.CursorOffset);
    }

    [Fact]
    public void Apply_AfterExistingText_PrependsBlankLinesForSpacing()
    {
        var result = _sut.Apply(precedingText: "Previous text");

        Assert.Equal("\n\n\n---\n\n\n", result.Text);
        Assert.Equal(result.Text.Length, result.CursorOffset);
    }

    [Fact]
    public void Apply_AfterExistingText_ProducesExpectedSurroundingSpacing()
    {
        const string precedingText = "Previous text";
        var result = _sut.Apply(precedingText);

        var fullText = precedingText + result.Text + "Next text";

        Assert.Equal("Previous text\n\n\n---\n\n\nNext text", fullText);
    }
}
