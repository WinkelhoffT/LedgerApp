using System.Text.RegularExpressions;

namespace StudyHub.Logic.Business.Notes;

public sealed class MarkdownBlockParser : IMarkdownBlockParser
{
    private static readonly Regex FencedCodeBlockPattern = new(@"```(\S*)\r?\n(.*?)```", RegexOptions.Singleline);

    public IReadOnlyList<MarkdownBlockSegment> Split(string content)
    {
        var segments = new List<MarkdownBlockSegment>();
        var lastIndex = 0;

        foreach (Match match in FencedCodeBlockPattern.Matches(content))
        {
            if (match.Index > lastIndex)
            {
                segments.Add(new MarkdownBlockSegment(false, string.Empty, content[lastIndex..match.Index].Trim('\n')));
            }

            segments.Add(new MarkdownBlockSegment(true, match.Groups[1].Value, match.Groups[2].Value.TrimEnd('\n')));

            lastIndex = match.Index + match.Length;
        }

        if (lastIndex < content.Length)
        {
            segments.Add(new MarkdownBlockSegment(false, string.Empty, content[lastIndex..].Trim('\n')));
        }

        return segments;
    }
}
