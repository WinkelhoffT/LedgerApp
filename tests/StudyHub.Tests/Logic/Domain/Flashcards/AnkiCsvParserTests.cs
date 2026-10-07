using System.Text;
using StudyHub.Logic.Domain;
using StudyHub.Logic.Domain.Contract;
using StudyHub.Shared.Flashcards;

namespace StudyHub.Tests.Logic.Domain.Flashcards;

public class AnkiCsvParserTests
{
    private readonly AnkiCsvParser _sut = new();

    private AnkiCsvParseResult Parse(string text) => _sut.Parse(Encoding.UTF8.GetBytes(text));

    private static byte[] ReadTestFile(string name) =>
        File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "TestData", "Flashcards", name));

    [Fact]
    public void Parse_AnkiExportWithAllColumns_MapsFieldsDeckNoteTypeAndTags()
    {
        var result = _sut.Parse(ReadTestFile("anki-notes-in-plain-text.txt"));

        Assert.Equal(4, result.Rows.Count);
        var first = result.Rows[0];
        Assert.Equal(7, first.LineNumber);
        Assert.Equal("Was berechnet der <b>Dijkstra</b>-Algorithmus?", first.Front);
        Assert.Equal(
            "Kürzeste Wege von einem Startknoten<br>bei nicht-negativen Kantengewichten.",
            first.Back
        );
        Assert.Equal(["graphen", "kürzeste_wege"], first.Tags);
        Assert.Equal("Informatik::Algorithmen", first.DeckName);
        Assert.Equal("Basic", first.NoteType);
        Assert.Equal("Basic (and reversed card)", result.Rows[1].NoteType);
        Assert.Equal("Cloze", result.Rows[2].NoteType);
        Assert.Null(result.DuplicateMode);
    }

    [Fact]
    public void Parse_AnkiExport_ReadsQuotedMultiLineFieldsAndCountsTheirLines()
    {
        var result = _sut.Parse(ReadTestFile("anki-notes-in-plain-text.txt"));

        var acid = result.Rows[3];
        Assert.Equal(10, acid.LineNumber);
        Assert.Equal("Was bedeutet \"ACID\"?", acid.Front);
        Assert.Equal("Atomicity, Consistency,\nIsolation, Durability", acid.Back);
        Assert.Equal("Informatik::Datenbanken", acid.DeckName);
    }

    [Fact]
    public void Parse_AnkiExportWithoutHtml_EscapesFieldsAndConvertsLineBreaks()
    {
        var result = _sut.Parse(ReadTestFile("anki-notes-without-html.txt"));

        Assert.Equal(
            "Ein Baum, in dem jeder Elternknoten &lt;= seine Kinder ist.",
            result.Rows[0].Back
        );
        Assert.Equal(["datenstrukturen", "heap"], result.Rows[0].Tags);
        Assert.Equal(
            "Setzt Commits auf eine neue Basis:<br>keine Merge-Commits.",
            result.Rows[1].Back
        );
        Assert.Null(result.Rows[0].DeckName);
    }

    [Fact]
    public void Parse_FileWithBomAndCrlfWithoutHeader_DetectsCommaAndReadsQuotes()
    {
        var result = _sut.Parse(ReadTestFile("plain-comma-with-bom.csv"));

        Assert.Equal(["Frage 1", "Frage, mit Komma"], result.Rows.Select(r => r.Front));
        Assert.Equal(["Antwort 1", "Antwort 2"], result.Rows.Select(r => r.Back));
        Assert.Equal([1, 2], result.Rows.Select(r => r.LineNumber));
    }

    [Theory]
    [InlineData("Comma", "a,b")]
    [InlineData("SEMICOLON", "a;b")]
    [InlineData("tab", "a\tb")]
    [InlineData("Space", "a b")]
    [InlineData("pipe", "a|b")]
    [InlineData("colon", "a:b")]
    [InlineData(",", "a,b")]
    [InlineData(";", "a;b")]
    [InlineData("\t", "a\tb")]
    [InlineData(" ", "a b")]
    [InlineData("|", "a|b")]
    [InlineData(":", "a:b")]
    public void Parse_WithSeparatorHeader_UsesNamedOrLiteralDelimiter(string separator, string line)
    {
        var result = Parse($"#separator:{separator}\n{line}\n");

        Assert.Equal("a", result.Rows[0].Front);
        Assert.Equal("b", result.Rows[0].Back);
    }

    [Theory]
    [InlineData("a\tb|c;d:e,f g", "a", "b|c;d:e,f g")]
    [InlineData("a|b;c:d,e f", "a", "b;c:d,e f")]
    [InlineData("a;b:c,d e", "a", "b:c,d e")]
    [InlineData("a:b,c d", "a", "b,c d")]
    [InlineData("a,b c", "a", "b c")]
    [InlineData("a b", "a", "b")]
    public void Parse_WithoutSeparatorHeader_DetectsDelimiterInAnkisOrder(
        string line,
        string front,
        string back
    )
    {
        var result = Parse(line + "\n");

        Assert.Equal(front, result.Rows[0].Front);
        Assert.Equal(back, result.Rows[0].Back);
    }

    [Fact]
    public void Parse_WithUnknownSeparator_FallsBackToDetection()
    {
        var result = Parse("#separator:Dash\na;b\n");

        Assert.Equal("a", result.Rows[0].Front);
    }

    [Fact]
    public void Parse_SkipsCommentLinesAndBlankLinesAfterTheHeader()
    {
        var result = Parse("#separator:Semicolon\nq1;a1\n# a comment;x\n\nq2;a2\n");

        Assert.Equal(["q1", "q2"], result.Rows.Select(r => r.Front));
        Assert.Equal([2, 5], result.Rows.Select(r => r.LineNumber));
    }

    [Fact]
    public void Parse_ReadsDoubledQuotesAndDelimitersInsideQuotes()
    {
        var result = Parse(
            "#separator:Semicolon\n\"Was ist \"\"O(n)\"\"?\";\"lineare; Laufzeit\"\n"
        );

        Assert.Equal("Was ist \"O(n)\"?", result.Rows[0].Front);
        Assert.Equal("lineare; Laufzeit", result.Rows[0].Back);
    }

    [Fact]
    public void Parse_WithShortRow_TreatsMissingColumnsAsEmpty()
    {
        var result = Parse("#separator:Semicolon\n#tags column:3\nnur Vorderseite\n");

        Assert.Equal("nur Vorderseite", result.Rows[0].Front);
        Assert.Equal(string.Empty, result.Rows[0].Back);
        Assert.Empty(result.Rows[0].Tags);
    }

    [Fact]
    public void Parse_WithColumnsHeader_MapsFrontAndBackByLabel()
    {
        var result = Parse("#separator:Semicolon\n#columns:Tags;back;FRONT\nt1 t2;Antwort;Frage\n");

        Assert.Equal("Frage", result.Rows[0].Front);
        Assert.Equal("Antwort", result.Rows[0].Back);
        Assert.Equal(["t1", "t2"], result.Rows[0].Tags);
    }

    [Fact]
    public void Parse_WithMetaColumnsAndNoLabels_UsesFirstTwoOtherColumnsAsFields()
    {
        var result = Parse(
            "#separator:Pipe\n#deck column:1\n#tags column:3\nDeck A|Frage|t1|Antwort|ignoriert\n"
        );

        Assert.Equal("Frage", result.Rows[0].Front);
        Assert.Equal("Antwort", result.Rows[0].Back);
        Assert.Equal("Deck A", result.Rows[0].DeckName);
        Assert.Equal(["t1"], result.Rows[0].Tags);
    }

    [Fact]
    public void Parse_WithGlobalDeckNoteTypeAndTags_AppliesThemToEveryRow()
    {
        var result = Parse(
            "#separator:Semicolon\n#deck:Netze\n#notetype:Basic\n#tags:klausur ws25\nq;a\n"
        );

        Assert.Equal("Netze", result.Rows[0].DeckName);
        Assert.Equal("Basic", result.Rows[0].NoteType);
        Assert.Equal(["klausur", "ws25"], result.Rows[0].Tags);
    }

    [Fact]
    public void Parse_WithDeckColumnValue_WinsOverGlobalDeck()
    {
        var result = Parse(
            "#separator:Semicolon\n#deck:Netze\n#deck column:3\nq1;a1;Algorithmen\nq2;a2;\n"
        );

        Assert.Equal(["Algorithmen", "Netze"], result.Rows.Select(r => r.DeckName));
    }

    [Fact]
    public void Parse_WithOldStyleTagsLine_AddsTagsToEveryRow()
    {
        var result = Parse("tags:klausur wichtig\nq;a\n");

        Assert.Equal(["klausur", "wichtig"], result.Rows[0].Tags);
    }

    [Fact]
    public void Parse_MatchesHeaderKeysIgnoringCaseAndSpaces()
    {
        var result = Parse(
            "# SEPARATOR :Semicolon\n#HTML: FALSE \n#If Matches: Keep Both\nq;<b>a</b>\n"
        );

        Assert.Equal("&lt;b&gt;a&lt;/b&gt;", result.Rows[0].Back);
        Assert.Equal(ImportDuplicateMode.KeepBoth, result.DuplicateMode);
    }

    [Theory]
    [InlineData("update current", ImportDuplicateMode.UpdateCurrent)]
    [InlineData("keep current", ImportDuplicateMode.KeepCurrent)]
    [InlineData("keep both", ImportDuplicateMode.KeepBoth)]
    public void Parse_ReadsIfMatches(string value, ImportDuplicateMode expected)
    {
        Assert.Equal(expected, Parse($"#if matches:{value}\nq;a\n").DuplicateMode);
    }

    [Fact]
    public void Parse_IgnoresUnknownHeadersAndMatchScope()
    {
        var result = Parse("#match scope:notetype + deck\n#foo:bar\n#just a comment\nq;a\n");

        Assert.Equal("q", Assert.Single(result.Rows).Front);
    }

    [Fact]
    public void Parse_WithoutHtmlHeader_TreatsFileAsHtmlWhenATagAppearsInTheFirstRows()
    {
        var result = Parse("#separator:Semicolon\nq1;a1\nq2;a<br>2\n");

        Assert.Equal("a<br>2", result.Rows[1].Back);
    }

    [Fact]
    public void Parse_WithoutHtmlHeaderAndWithoutTags_EscapesFields()
    {
        var result = Parse("#separator:Semicolon\nIst a < b > c?;ja & nein\n");

        Assert.Equal("Ist a &lt; b &gt; c?", result.Rows[0].Front);
        Assert.Equal("ja &amp; nein", result.Rows[0].Back);
    }

    [Fact]
    public void Parse_WithHtmlTrue_KeepsFieldsUnchanged()
    {
        var result = Parse("#separator:Semicolon\n#html:true\na < b;x\n");

        Assert.Equal("a < b", result.Rows[0].Front);
    }

    [Fact]
    public void Parse_WithColumnsBeforeSeparator_SplitsColumnsWithTheFinalDelimiter()
    {
        var result = Parse("#columns:Back;Front\n#separator:Semicolon\nAntwort;Frage\n");

        Assert.Equal("Frage", result.Rows[0].Front);
    }

    [Fact]
    public void Parse_WithFileLargerThanTheLimit_Throws()
    {
        var content = new byte[ImportFlashcardsRequest.MaxFileSizeBytes + 1];

        var ex = Assert.Throws<FlashcardImportException>(() => _sut.Parse(content));

        Assert.Contains("5 MB", ex.Message);
    }

    [Fact]
    public void Parse_WithInvalidUtf8_Throws()
    {
        var latin1 = Encoding.Latin1.GetBytes("Größe;Antwort\n");

        var ex = Assert.Throws<FlashcardImportException>(() => _sut.Parse(latin1));

        Assert.Contains("UTF-8", ex.Message);
    }

    [Fact]
    public void Parse_WithUtf16_Throws()
    {
        var utf16 = Encoding
            .Unicode.GetPreamble()
            .Concat(Encoding.Unicode.GetBytes("q;a\n"))
            .ToArray();

        Assert.Throws<FlashcardImportException>(() => _sut.Parse(utf16));
    }

    [Fact]
    public void Parse_WithTooManyRows_Throws()
    {
        var text = string.Concat(
            Enumerable.Range(0, ImportFlashcardsRequest.MaxRows + 1).Select(i => $"q{i};a\n")
        );

        var ex = Assert.Throws<FlashcardImportException>(() => Parse(text));

        Assert.Contains("5,000", ex.Message);
    }

    [Fact]
    public void Parse_WithExactlyTheRowLimit_Succeeds()
    {
        var text = string.Concat(
            Enumerable.Range(0, ImportFlashcardsRequest.MaxRows).Select(i => $"q{i};a\n")
        );

        Assert.Equal(ImportFlashcardsRequest.MaxRows, Parse(text).Rows.Count);
    }

    [Fact]
    public void Parse_SerializerExport_ReturnsTheSameCards()
    {
        var cards = new[]
        {
            new FlashcardDto(
                "Was ist \"stabil\"?",
                "Gleiche Schlüssel; gleiche <b>Reihenfolge</b>",
                ["sortieren", "Kürzeste_Wege"]
            ),
            new FlashcardDto("Laufzeit?", "<code>O(n log n)</code>", []),
        };

        var result = _sut.Parse(new AnkiCsvSerializer().Serialize("StudyHub::Algorithmen", cards));

        Assert.Equal(cards.Select(c => c.Front), result.Rows.Select(r => r.Front));
        Assert.Equal(cards.Select(c => c.Back), result.Rows.Select(r => r.Back));
        Assert.Equal(cards.Select(c => c.Tags), result.Rows.Select(r => r.Tags));
        Assert.All(
            result.Rows,
            row =>
            {
                Assert.Equal("StudyHub::Algorithmen", row.DeckName);
                Assert.Equal("Basic", row.NoteType);
            }
        );
    }
}
