using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;

namespace StudyHub.UI.Flashcards;

/// <summary>
/// Renders a card field. Card text follows Anki and may contain HTML (AI output or imported Anki
/// decks); everything is HTML-encoded first and only a small formatting allowlist is turned back
/// into markup, always without attributes, so a card can never inject scripts, styles or links into
/// the page. Images are removed, since the CSV import carries no media.
/// </summary>
public static partial class FlashcardHtmlFormatter
{
    public static MarkupString ToPreview(string? value)
    {
        var encoded = WebUtility.HtmlEncode(value ?? string.Empty);
        encoded = ImageTag().Replace(encoded, string.Empty);
        encoded = AllowedTag().Replace(encoded, match => $"<{match.Groups[1].Value}{match.Groups[2].Value.ToLowerInvariant()}>");
        encoded = EncodedEntity().Replace(encoded, match => $"&{match.Groups[1].Value};");
        encoded = LineBreaks().Replace(encoded, "<br>");
        return new MarkupString(encoded);
    }

    [GeneratedRegex(@"&lt;(/?)(br|b|i|u|code|div|p|span|sub|sup|pre|ul|ol|li)(?:\s(?:(?!&gt;).)*?)?\s*/?&gt;", RegexOptions.IgnoreCase)]
    private static partial Regex AllowedTag();

    [GeneratedRegex(@"&lt;img\b(?:(?!&gt;).)*&gt;", RegexOptions.IgnoreCase)]
    private static partial Regex ImageTag();

    [GeneratedRegex(@"&amp;(lt|gt|amp|quot|nbsp);")]
    private static partial Regex EncodedEntity();

    [GeneratedRegex(@"\r\n|\r|\n")]
    private static partial Regex LineBreaks();
}
