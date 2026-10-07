using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;

namespace StudyHub.UI.Flashcards;

/// <summary>
/// Renders a card field for preview. Card text is meant for Anki and may contain a few HTML tags;
/// everything is HTML-encoded first and only the small formatting whitelist the prompt asks for
/// (<c>br</c>, <c>b</c>, <c>i</c>, <c>code</c>) and the basic entities are turned back into markup,
/// so AI output can never inject arbitrary HTML into the page.
/// </summary>
public static partial class FlashcardHtmlFormatter
{
    public static MarkupString ToPreview(string? value)
    {
        var encoded = WebUtility.HtmlEncode(value ?? string.Empty);
        encoded = AllowedTag().Replace(encoded, match => $"<{match.Groups[1].Value}{match.Groups[2].Value.ToLowerInvariant()}>");
        encoded = EncodedEntity().Replace(encoded, match => $"&{match.Groups[1].Value};");
        encoded = LineBreaks().Replace(encoded, "<br>");
        return new MarkupString(encoded);
    }

    [GeneratedRegex(@"&lt;(/?)(br|b|i|code)\s*/?&gt;", RegexOptions.IgnoreCase)]
    private static partial Regex AllowedTag();

    [GeneratedRegex(@"&amp;(lt|gt|amp|quot|nbsp);")]
    private static partial Regex EncodedEntity();

    [GeneratedRegex(@"\r\n|\r|\n")]
    private static partial Regex LineBreaks();
}
