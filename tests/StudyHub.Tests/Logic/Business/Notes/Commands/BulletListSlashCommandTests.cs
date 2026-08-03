using StudyHub.Logic.Business.Notes.Commands;

namespace StudyHub.Tests.Logic.Business.Notes.Commands;

public class BulletListSlashCommandTests
{
    [Fact]
    public void Apply_ReturnsBulletMarkerWithCursorAtEnd()
    {
        var result = new BulletListSlashCommand().Apply(precedingText: string.Empty);

        Assert.Equal("- ", result.Text);
        Assert.Equal(2, result.CursorOffset);
    }
}
