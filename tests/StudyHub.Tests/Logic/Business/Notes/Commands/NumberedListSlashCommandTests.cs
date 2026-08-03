using StudyHub.Logic.Business.Notes.Commands;

namespace StudyHub.Tests.Logic.Business.Notes.Commands;

public class NumberedListSlashCommandTests
{
    [Fact]
    public void Apply_ReturnsNumberedMarkerWithCursorAtEnd()
    {
        var result = new NumberedListSlashCommand().Apply(precedingText: string.Empty);

        Assert.Equal("1. ", result.Text);
        Assert.Equal(3, result.CursorOffset);
    }
}
