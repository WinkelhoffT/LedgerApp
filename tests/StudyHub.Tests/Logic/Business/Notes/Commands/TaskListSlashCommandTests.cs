using StudyHub.Logic.Business.Notes.Commands;

namespace StudyHub.Tests.Logic.Business.Notes.Commands;

public class TaskListSlashCommandTests
{
    [Fact]
    public void Apply_ReturnsCheckboxMarkerWithCursorAtEnd()
    {
        var result = new TaskListSlashCommand().Apply(precedingText: string.Empty);

        Assert.Equal("- [ ] ", result.Text);
        Assert.Equal(6, result.CursorOffset);
    }
}
