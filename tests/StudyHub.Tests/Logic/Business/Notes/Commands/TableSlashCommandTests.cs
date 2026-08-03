using StudyHub.Logic.Business.Notes.Commands;

namespace StudyHub.Tests.Logic.Business.Notes.Commands;

public class TableSlashCommandTests
{
    private readonly TableSlashCommand _sut = new();

    [Fact]
    public void Apply_ReturnsHeaderSeparatorAndEmptyRow()
    {
        var result = _sut.Apply(precedingText: string.Empty);

        Assert.Equal(
            "| Column | Column |\n|--------|--------|\n|        |        |",
            result.Text);
    }

    [Fact]
    public void Apply_PlacesCursorInFirstEmptyCell()
    {
        var result = _sut.Apply(precedingText: string.Empty);

        var lastLine = result.Text.Split('\n')[^1];
        var cursorColumnInLastLine = result.CursorOffset - (result.Text.Length - lastLine.Length);

        Assert.Equal(1, cursorColumnInLastLine);
        Assert.Equal('|', lastLine[0]);
        Assert.Equal(' ', lastLine[cursorColumnInLastLine]);
    }
}
