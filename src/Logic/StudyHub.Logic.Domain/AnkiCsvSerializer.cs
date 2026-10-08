using System.Text;
using System.Text.RegularExpressions;
using StudyHub.Logic.Domain.Contract;
using StudyHub.Shared.Flashcards;

namespace StudyHub.Logic.Domain;

public sealed partial class AnkiCsvSerializer : IAnkiCsvSerializer
{
    private const string FileExtension = ".csv";
    private const string FallbackFileName = "flashcards";
    private const int FileNameMaxLength = 100;

    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    public string CreateFileName(string name)
    {
        var baseName = name ?? string.Empty;
        if (baseName.EndsWith(FileExtension, StringComparison.OrdinalIgnoreCase))
        {
            baseName = baseName[..^FileExtension.Length];
        }

        baseName = InvalidFileNameCharacters().Replace(baseName, "_");
        baseName = CollapseWhitespace(baseName).Trim('.', ' ', '_');
        baseName = Truncate(baseName, FileNameMaxLength).TrimEnd('.', ' ');

        return (baseName.Length == 0 ? FallbackFileName : baseName) + FileExtension;
    }

    public byte[] Serialize(string deckName, IReadOnlyList<FlashcardDto> cards)
    {
        var builder = new StringBuilder();
        builder.Append("#separator:Semicolon\n");
        builder.Append("#html:true\n");
        builder.Append("#notetype:Basic\n");
        builder.Append("#deck:").Append(CollapseWhitespace(deckName)).Append('\n');
        builder.Append("#columns:Front;Back;Tags\n");
        builder.Append("#tags column:3\n");

        foreach (var card in cards)
        {
            builder
                .Append(QuoteField(card.Front)).Append(';')
                .Append(QuoteField(card.Back)).Append(';')
                .Append(QuoteField(FormatTags(card.Tags))).Append('\n');
        }

        return Utf8WithoutBom.GetBytes(builder.ToString());
    }

    private static string QuoteField(string value)
    {
        var singleLine = LineBreaks().Replace(value, "<br>");
        return "\"" + singleLine.Replace("\"", "\"\"") + "\"";
    }

    // Anki separates tags with spaces, so spaces inside a single tag become underscores.
    private static string FormatTags(IReadOnlyList<string> tags) =>
        string.Join(' ', tags.Select(tag => Whitespace().Replace(tag.Trim(), "_")).Where(tag => tag.Length > 0));

    private static string CollapseWhitespace(string value) => Whitespace().Replace(value, " ").Trim();

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength].TrimEnd();

    [GeneratedRegex(@"\r\n|\r|\n")]
    private static partial Regex LineBreaks();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    [GeneratedRegex(@"[\\/:*?""<>|\x00-\x1F]")]
    private static partial Regex InvalidFileNameCharacters();
}
