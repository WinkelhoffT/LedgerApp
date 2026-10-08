using System.Text;
using System.Text.RegularExpressions;
using StudyHub.Logic.Domain.Contract;
using StudyHub.Shared.Flashcards;

namespace StudyHub.Logic.Domain;

/// <summary>
/// Follows Anki's text importer (<c>rslib/src/import_export/text/csv/metadata.rs</c> and
/// <c>import.rs</c> in https://github.com/ankitects/anki), see
/// <c>docs/plans/flashcard-study-plan.md</c>, section 4.
/// </summary>
public sealed partial class AnkiCsvParser : IAnkiCsvParser
{
    private const int DelimiterSampleLength = 8 * 1024;
    private const int HtmlSampleRows = 5;
    private const char Quote = '"';

    // Anki tries the delimiters in this order and takes the first one that occurs at all.
    private static readonly char[] DelimiterFallbackOrder = ['\t', '|', ';', ':', ',', ' '];

    private static readonly Dictionary<string, char> DelimiterNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["comma"] = ',',
        ["semicolon"] = ';',
        ["tab"] = '\t',
        ["space"] = ' ',
        ["pipe"] = '|',
        ["colon"] = ':',
    };

    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    public AnkiCsvParseResult Parse(byte[] content)
    {
        if (content.Length > ImportFlashcardsRequest.MaxFileSizeBytes)
        {
            throw new FlashcardImportException(
                $"The file is larger than {ImportFlashcardsRequest.MaxFileSizeBytes / (1024 * 1024)} MB.");
        }

        var text = Decode(content);
        var (header, position, lineNumber) = ReadHeader(text);
        var delimiter = header.Delimiter ?? DetectDelimiter(text, position);
        var records = ReadRecords(text, position, lineNumber, delimiter);
        var isHtml = header.IsHtml ?? records.Take(HtmlSampleRows).Any(record => record.Fields.Any(HtmlTag().IsMatch));

        var columnLabels = header.ColumnsValue is null
            ? []
            : ReadFields(header.ColumnsValue, 0, delimiter, out _).Select(label => label.Trim()).ToList();

        var rows = records
            .Select(record => ToRow(record, header, columnLabels, isHtml))
            .ToList();

        return new AnkiCsvParseResult(header.DuplicateMode, rows);
    }

    private static string Decode(byte[] content)
    {
        // UTF-16 text of ASCII characters is also valid UTF-8, so its byte order mark is rejected explicitly.
        if (content is [0xFF, 0xFE, ..] or [0xFE, 0xFF, ..])
        {
            throw NotUtf8();
        }

        var offset = content is [0xEF, 0xBB, 0xBF, ..] ? 3 : 0;

        try
        {
            var text = StrictUtf8.GetString(content, offset, content.Length - offset);
            return text.Contains('\0') ? throw NotUtf8() : text;
        }
        catch (DecoderFallbackException)
        {
            throw NotUtf8();
        }
    }

    private static FlashcardImportException NotUtf8() =>
        new("The file is not UTF-8 encoded. Export it from Anki again or save it as UTF-8.");

    private static (AnkiCsvHeader Header, int Position, int LineNumber) ReadHeader(string text)
    {
        var header = new AnkiCsvHeader();
        var position = 0;
        var lineNumber = 1;

        while (position < text.Length)
        {
            var lineEnd = FindLineEnd(text, position);
            var line = text[position..lineEnd];

            if (lineNumber == 1 && line.StartsWith("tags:", StringComparison.Ordinal))
            {
                header = header with { Tags = SplitTags(line["tags:".Length..]) };
            }
            else if (line.StartsWith('#'))
            {
                var colon = line.IndexOf(':');
                if (colon > 1)
                {
                    header = ApplyHeaderValue(header, line[1..colon], line[(colon + 1)..]);
                }
            }
            else
            {
                break;
            }

            position = SkipLineBreak(text, lineEnd);
            lineNumber++;
        }

        return (header, position, lineNumber);
    }

    private static AnkiCsvHeader ApplyHeaderValue(AnkiCsvHeader header, string key, string rawValue)
    {
        var value = rawValue.Trim();

        return key.Trim().ToLowerInvariant() switch
        {
            "separator" => ParseDelimiter(rawValue) is { } delimiter ? header with { Delimiter = delimiter } : header,
            "html" => bool.TryParse(value, out var isHtml) ? header with { IsHtml = isHtml } : header,
            "tags" => header with { Tags = SplitTags(value) },
            "columns" => header with { ColumnsValue = rawValue },
            "deck" => value.Length > 0 ? header with { DeckName = value } : header,
            "notetype" => value.Length > 0 ? header with { NoteType = value } : header,
            "deck column" => ParseColumn(value) is { } column ? header with { DeckColumn = column } : header,
            "notetype column" => ParseColumn(value) is { } column ? header with { NoteTypeColumn = column } : header,
            "tags column" => ParseColumn(value) is { } column ? header with { TagsColumn = column } : header,
            "guid column" => ParseColumn(value) is { } column ? header with { GuidColumn = column } : header,
            "if matches" => ParseDuplicateMode(value) is { } mode ? header with { DuplicateMode = mode } : header,

            // "#match scope:" is read but ignored, since StudyHub always matches within the target deck.
            // Unknown keys are ignored, as in Anki.
            _ => header,
        };
    }

    // A literal delimiter is taken as is (a single space is a valid value); a name may carry spaces.
    private static char? ParseDelimiter(string value)
    {
        if (value.Length == 1 && DelimiterFallbackOrder.Contains(value[0]))
        {
            return value[0];
        }

        return DelimiterNames.TryGetValue(value.Trim(), out var delimiter) ? delimiter : null;
    }

    private static int? ParseColumn(string value) =>
        int.TryParse(value, out var column) && column > 0 ? column : null;

    private static ImportDuplicateMode? ParseDuplicateMode(string value) =>
        Whitespace().Replace(value, " ").ToLowerInvariant() switch
        {
            "update current" => ImportDuplicateMode.UpdateCurrent,
            "keep current" => ImportDuplicateMode.KeepCurrent,
            "keep both" => ImportDuplicateMode.KeepBoth,
            _ => null,
        };

    private static char DetectDelimiter(string text, int position)
    {
        var sample = text.AsSpan(position, Math.Min(DelimiterSampleLength, text.Length - position));

        foreach (var delimiter in DelimiterFallbackOrder)
        {
            if (sample.Contains(delimiter))
            {
                return delimiter;
            }
        }

        return ' ';
    }

    private static List<AnkiCsvRecord> ReadRecords(string text, int position, int lineNumber, char delimiter)
    {
        var records = new List<AnkiCsvRecord>();

        while (position < text.Length)
        {
            var current = text[position];

            // Like Anki's CSV reader: blank lines are skipped, and a line starting with '#' is a
            // comment wherever it appears.
            if (current is '\n' or '\r' or '#')
            {
                position = SkipLineBreak(text, FindLineEnd(text, position));
                lineNumber++;
                continue;
            }

            var startLine = lineNumber;
            var fields = ReadFields(text, position, delimiter, out var end, ref lineNumber);
            records.Add(new AnkiCsvRecord(startLine, fields));

            if (records.Count > ImportFlashcardsRequest.MaxRows)
            {
                throw new FlashcardImportException($"The file has more than {ImportFlashcardsRequest.MaxRows:N0} rows.");
            }

            position = SkipLineBreak(text, end);
            lineNumber++;
        }

        return records;
    }

    private static List<string> ReadFields(string text, int position, char delimiter, out int end)
    {
        var lineNumber = 0;
        return ReadFields(text, position, delimiter, out end, ref lineNumber);
    }

    // Reads one record (RFC 4180): quoted fields may contain the delimiter, line breaks and "" for a
    // quote. Like Anki's CSV reader it is lenient: text after a closing quote is kept as is, and an
    // unterminated quote runs to the end of the file. Returns at the line break ending the record.
    private static List<string> ReadFields(string text, int position, char delimiter, out int end, ref int lineNumber)
    {
        var fields = new List<string>();
        var field = new StringBuilder();

        while (true)
        {
            if (position < text.Length && text[position] == Quote)
            {
                position++;
                while (position < text.Length)
                {
                    var current = text[position];
                    if (current == Quote)
                    {
                        if (position + 1 < text.Length && text[position + 1] == Quote)
                        {
                            field.Append(Quote);
                            position += 2;
                            continue;
                        }

                        position++;
                        break;
                    }

                    if (current == '\n' || (current == '\r' && (position + 1 == text.Length || text[position + 1] != '\n')))
                    {
                        lineNumber++;
                    }

                    field.Append(current);
                    position++;
                }
            }

            while (position < text.Length && text[position] != delimiter && text[position] is not ('\n' or '\r'))
            {
                field.Append(text[position]);
                position++;
            }

            fields.Add(field.ToString());
            field.Clear();

            if (position < text.Length && text[position] == delimiter)
            {
                position++;
                continue;
            }

            end = position;
            return fields;
        }
    }

    private static AnkiCsvRow ToRow(AnkiCsvRecord record, AnkiCsvHeader header, IReadOnlyList<string> columnLabels, bool isHtml)
    {
        var deckIndex = header.DeckColumn - 1;
        var noteTypeIndex = header.NoteTypeColumn - 1;
        var guidIndex = header.GuidColumn - 1;
        var tagsIndex = header.TagsColumn - 1 ?? IndexOfLabel(columnLabels, "tags");
        var metaColumns = new[] { deckIndex, noteTypeIndex, guidIndex, tagsIndex }.OfType<int>().ToHashSet();

        var (frontIndex, backIndex) = GetFieldIndexes(columnLabels, metaColumns);

        var tags = SplitTags(GetField(record, tagsIndex))
            .Concat(header.Tags ?? [])
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return new AnkiCsvRow(
            record.LineNumber,
            ToHtml(GetField(record, frontIndex), isHtml),
            ToHtml(GetField(record, backIndex), isHtml),
            tags,
            NullIfEmpty(GetField(record, deckIndex)) ?? header.DeckName,
            NullIfEmpty(GetField(record, noteTypeIndex)) ?? header.NoteType);
    }

    // Columns labelled Front and Back win; otherwise the first two columns that carry no deck,
    // note type, tags or GUID are the card's fields.
    private static (int Front, int Back) GetFieldIndexes(IReadOnlyList<string> columnLabels, HashSet<int> metaColumns)
    {
        if (IndexOfLabel(columnLabels, "front") is { } front && IndexOfLabel(columnLabels, "back") is { } back)
        {
            return (front, back);
        }

        var fieldColumns = Enumerable.Range(0, int.MaxValue).Where(index => !metaColumns.Contains(index)).Take(2).ToArray();
        return (fieldColumns[0], fieldColumns[1]);
    }

    private static int? IndexOfLabel(IReadOnlyList<string> columnLabels, string label)
    {
        for (var i = 0; i < columnLabels.Count; i++)
        {
            if (string.Equals(columnLabels[i], label, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return null;
    }

    // A row may be shorter than others; a missing column counts as empty.
    private static string GetField(AnkiCsvRecord record, int? index) =>
        index is { } i && i < record.Fields.Count ? record.Fields[i] : string.Empty;

    // Plain-text fields are escaped so they show literally, and their line breaks become <br>, as in Anki.
    private static string ToHtml(string field, bool isHtml) =>
        isHtml
            ? field
            : LineBreaks().Replace(field.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;"), "<br>");

    private static IReadOnlyList<string> SplitTags(string value) =>
        value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

    private static string? NullIfEmpty(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static int FindLineEnd(string text, int position)
    {
        var lineEnd = text.IndexOfAny(['\n', '\r'], position);
        return lineEnd < 0 ? text.Length : lineEnd;
    }

    private static int SkipLineBreak(string text, int lineEnd) =>
        lineEnd >= text.Length ? text.Length
        : text[lineEnd] == '\r' && lineEnd + 1 < text.Length && text[lineEnd + 1] == '\n' ? lineEnd + 2
        : lineEnd + 1;

    [GeneratedRegex(@"</?[a-zA-Z][a-zA-Z0-9]*(\s[^<>]*)?/?>")]
    private static partial Regex HtmlTag();

    [GeneratedRegex(@"\r\n|\r|\n")]
    private static partial Regex LineBreaks();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
