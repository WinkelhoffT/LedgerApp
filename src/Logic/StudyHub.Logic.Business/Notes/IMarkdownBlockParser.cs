namespace StudyHub.Logic.Business.Notes;

public interface IMarkdownBlockParser
{
    /// <summary>Splits markdown content into plain-text and fenced-code segments, in order.</summary>
    IReadOnlyList<MarkdownBlockSegment> Split(string content);
}
