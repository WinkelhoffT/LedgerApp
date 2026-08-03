using StudyHub.Logic.Business.Notes.Commands;

namespace StudyHub.Tests.Logic.Business.Notes.Commands;

public class QuoteSlashCommandTests
{
    [Fact]
    public void Apply_ReturnsQuoteMarkerWithCursorAtEnd()
    {
        var result = new QuoteSlashCommand().Apply(precedingText: string.Empty);

        Assert.Equal("> ", result.Text);
        Assert.Equal(2, result.CursorOffset);
    }
}
