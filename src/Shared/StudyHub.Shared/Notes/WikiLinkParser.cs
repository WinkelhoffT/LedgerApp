using System.Text.RegularExpressions;

namespace StudyHub.Shared.Notes;

// Shared by StudyHub.Logic.Business (link resolution against NoteLinks) and StudyHub.UI (live
// preview rendering) so both sides parse `[[Title]]` wiki-links identically.
public static partial class WikiLinkParser
{
    [GeneratedRegex(@"\[\[(.+?)\]\]")]
    public static partial Regex Pattern();

    public static IReadOnlyCollection<string> ExtractTitles(string content)
    {
        var titles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in Pattern().Matches(content))
        {
            var title = match.Groups[1].Value.Trim();
            if (title.Length > 0)
            {
                titles.Add(title);
            }
        }

        return titles;
    }
}
