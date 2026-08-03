using StudyHub.Logic.Business.Notes.Commands;

namespace StudyHub.Tests.Logic.Business.Notes.Commands;

public class SlashTriggerDetectorTests
{
    private readonly SlashTriggerDetector _sut = new();

    [Fact]
    public void Detect_SlashAtStartOfBlock_ReturnsTrigger()
    {
        var trigger = _sut.Detect("/hea", caretIndex: 4);

        Assert.NotNull(trigger);
        Assert.Equal(0, trigger.Start);
        Assert.Equal("hea", trigger.Query);
    }

    [Fact]
    public void Detect_SlashAfterWhitespace_ReturnsTrigger()
    {
        var trigger = _sut.Detect("Some notes /code", caretIndex: 16);

        Assert.NotNull(trigger);
        Assert.Equal(11, trigger.Start);
        Assert.Equal("code", trigger.Query);
    }

    [Fact]
    public void Detect_SlashInMiddleOfWord_ReturnsNull()
    {
        var trigger = _sut.Detect("and/or", caretIndex: 6);

        Assert.Null(trigger);
    }

    [Fact]
    public void Detect_QueryContainsWhitespace_ReturnsNull()
    {
        var trigger = _sut.Detect("/hea ding", caretIndex: 9);

        Assert.Null(trigger);
    }

    [Fact]
    public void Detect_NoSlashBeforeCaret_ReturnsNull()
    {
        var trigger = _sut.Detect("just text", caretIndex: 9);

        Assert.Null(trigger);
    }

    [Fact]
    public void Detect_CaretAtZero_ReturnsNull()
    {
        var trigger = _sut.Detect("/heading", caretIndex: 0);

        Assert.Null(trigger);
    }

    [Fact]
    public void Detect_CaretMidwayThroughQuery_ReturnsQueryUpToCaret()
    {
        var trigger = _sut.Detect("/heading", caretIndex: 4);

        Assert.NotNull(trigger);
        Assert.Equal(0, trigger.Start);
        Assert.Equal("hea", trigger.Query);
    }
}
