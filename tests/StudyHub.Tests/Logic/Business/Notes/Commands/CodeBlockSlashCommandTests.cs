using StudyHub.Logic.Business.Notes.Commands;

namespace StudyHub.Tests.Logic.Business.Notes.Commands;

public class CodeBlockSlashCommandTests
{
    private readonly CodeBlockSlashCommand _sut = new();

    [Fact]
    public void Apply_ReturnsFencedCodeBlockWithDefaultLanguage()
    {
        var result = _sut.Apply(precedingText: string.Empty);

        Assert.Equal("```csharp\n\n```", result.Text);
    }

    [Fact]
    public void Apply_PlacesCursorOnTheBlankLineInsideTheFence()
    {
        var result = _sut.Apply(precedingText: string.Empty);

        var textUpToCursor = result.Text[..result.CursorOffset];
        var textAfterCursor = result.Text[result.CursorOffset..];

        Assert.Equal("```csharp\n", textUpToCursor);
        Assert.Equal("\n```", textAfterCursor);
    }

    [Fact]
    public void Id_MatchesPublicCommandIdConstant()
    {
        Assert.Equal(CodeBlockSlashCommand.CommandId, _sut.Id);
    }
}
