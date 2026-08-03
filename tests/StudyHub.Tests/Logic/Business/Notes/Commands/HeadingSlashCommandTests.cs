using StudyHub.Logic.Business.Notes.Commands;

namespace StudyHub.Tests.Logic.Business.Notes.Commands;

public class HeadingSlashCommandTests
{
    [Theory]
    [InlineData(1, "# ")]
    [InlineData(2, "## ")]
    [InlineData(3, "### ")]
    public void Apply_ReturnsPrefixWithCursorAtEnd(int level, string expectedText)
    {
        var command = new HeadingSlashCommand(level);

        var result = command.Apply(precedingText: string.Empty);

        Assert.Equal(expectedText, result.Text);
        Assert.Equal(expectedText.Length, result.CursorOffset);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void Constructor_WithInvalidLevel_Throws(int level)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new HeadingSlashCommand(level));
    }

    [Fact]
    public void Category_IsText()
    {
        Assert.Equal(SlashCommandCategory.Text, new HeadingSlashCommand(1).Category);
    }
}
