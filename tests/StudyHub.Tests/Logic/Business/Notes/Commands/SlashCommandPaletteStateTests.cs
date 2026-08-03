using Moq;
using StudyHub.Logic.Business.Notes.Commands;

namespace StudyHub.Tests.Logic.Business.Notes.Commands;

public class SlashCommandPaletteStateTests
{
    private readonly Mock<ISlashCommandRegistry> _registry = new();
    private readonly ISlashCommand _heading1 = new HeadingSlashCommand(1);
    private readonly ISlashCommand _heading2 = new HeadingSlashCommand(2);
    private readonly ISlashCommand _divider = new DividerSlashCommand();

    private SlashCommandPaletteState CreateSut() => new(_registry.Object);

    [Fact]
    public void Open_PopulatesResultsFromRegistrySearch()
    {
        _registry.Setup(r => r.Search("hea")).Returns([_heading1, _heading2]);
        var sut = CreateSut();

        sut.Open("hea");

        Assert.True(sut.IsOpen);
        Assert.Equal(2, sut.Results.Count);
        Assert.Equal(0, sut.SelectedIndex);
    }

    [Fact]
    public void SetQuery_WithNoResults_LeavesSelectedIndexAtZero()
    {
        _registry.Setup(r => r.Search(It.IsAny<string>())).Returns([]);
        var sut = CreateSut();

        sut.Open("xyz");

        Assert.Empty(sut.Results);
        Assert.Equal(0, sut.SelectedIndex);
        Assert.Null(sut.GetSelected());
    }

    [Fact]
    public void MoveSelection_WrapsAroundPastTheEnd()
    {
        _registry.Setup(r => r.Search(string.Empty)).Returns([_heading1, _heading2, _divider]);
        var sut = CreateSut();
        sut.Open(string.Empty);

        sut.MoveSelection(1);
        sut.MoveSelection(1);
        sut.MoveSelection(1);

        Assert.Equal(0, sut.SelectedIndex);
    }

    [Fact]
    public void MoveSelection_WrapsAroundBeforeTheStart()
    {
        _registry.Setup(r => r.Search(string.Empty)).Returns([_heading1, _heading2, _divider]);
        var sut = CreateSut();
        sut.Open(string.Empty);

        sut.MoveSelection(-1);

        Assert.Equal(2, sut.SelectedIndex);
    }

    [Fact]
    public void GetSelected_ReturnsCommandAtSelectedIndex()
    {
        _registry.Setup(r => r.Search(string.Empty)).Returns([_heading1, _heading2]);
        var sut = CreateSut();
        sut.Open(string.Empty);

        sut.MoveSelection(1);

        Assert.Equal(_heading2, sut.GetSelected());
    }

    [Fact]
    public void SetSelectedIndex_ClampsWithinResultsBounds()
    {
        _registry.Setup(r => r.Search(string.Empty)).Returns([_heading1, _heading2]);
        var sut = CreateSut();
        sut.Open(string.Empty);

        sut.SetSelectedIndex(10);

        Assert.Equal(1, sut.SelectedIndex);
    }

    [Fact]
    public void Close_ResetsStateToClosed()
    {
        _registry.Setup(r => r.Search(string.Empty)).Returns([_heading1]);
        var sut = CreateSut();
        sut.Open(string.Empty);

        sut.Close();

        Assert.False(sut.IsOpen);
        Assert.Empty(sut.Results);
        Assert.Equal(string.Empty, sut.Query);
        Assert.Equal(0, sut.SelectedIndex);
    }
}
