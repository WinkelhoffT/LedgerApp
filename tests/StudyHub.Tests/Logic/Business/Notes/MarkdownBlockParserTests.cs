using StudyHub.Logic.Business.Notes;

namespace StudyHub.Tests.Logic.Business.Notes;

public class MarkdownBlockParserTests
{
    private readonly MarkdownBlockParser _sut = new();

    [Fact]
    public void Split_PlainText_ReturnsSingleNonCodeSegment()
    {
        var segments = _sut.Split("Just a paragraph.");

        var segment = Assert.Single(segments);
        Assert.False(segment.IsCode);
        Assert.Equal("Just a paragraph.", segment.Text);
    }

    [Fact]
    public void Split_FencedCodeBlock_ReturnsCodeSegmentWithLanguage()
    {
        var segments = _sut.Split("```csharp\nvar x = 1;\n```");

        var segment = Assert.Single(segments);
        Assert.True(segment.IsCode);
        Assert.Equal("csharp", segment.Language);
        Assert.Equal("var x = 1;", segment.Text);
    }

    [Fact]
    public void Split_TextSurroundingCodeBlock_ReturnsThreeSegmentsInOrder()
    {
        var content = "Before text\n\n```csharp\nvar x = 1;\n```\n\nAfter text";

        var segments = _sut.Split(content);

        Assert.Equal(3, segments.Count);
        Assert.False(segments[0].IsCode);
        Assert.Equal("Before text", segments[0].Text);
        Assert.True(segments[1].IsCode);
        Assert.Equal("var x = 1;", segments[1].Text);
        Assert.False(segments[2].IsCode);
        Assert.Equal("After text", segments[2].Text);
    }

    [Fact]
    public void Split_EmptyContent_ReturnsNoSegments()
    {
        Assert.Empty(_sut.Split(string.Empty));
    }
}
