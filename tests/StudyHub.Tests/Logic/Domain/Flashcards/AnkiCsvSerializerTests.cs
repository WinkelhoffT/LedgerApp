using System.Text;
using StudyHub.Logic.Domain;
using StudyHub.Shared.Flashcards;

namespace StudyHub.Tests.Logic.Domain.Flashcards;

public class AnkiCsvSerializerTests
{
    private readonly AnkiCsvSerializer _sut = new();

    private string SerializeToString(params FlashcardDto[] cards) =>
        Encoding.UTF8.GetString(_sut.Serialize("StudyHub::Algorithms::Graphs", cards));

    private static string[] DataLines(string csv) =>
        csv.Split('\n', StringSplitOptions.RemoveEmptyEntries).Where(line => !line.StartsWith('#')).ToArray();

    [Fact]
    public void Serialize_WritesAnkiHeaderLines()
    {
        var csv = SerializeToString(new FlashcardDto("Q", "A", []));

        Assert.StartsWith(
            "#separator:Semicolon\n#html:true\n#notetype:Basic\n#deck:StudyHub::Algorithms::Graphs\n#columns:Front;Back;Tags\n#tags column:3\n",
            csv);
    }

    [Fact]
    public void Serialize_QuotesEveryFieldAndEscapesQuotes()
    {
        var csv = SerializeToString(new FlashcardDto("Was bedeutet \"stabil\"?", "Gleiche Schlüssel; gleiche Reihenfolge", ["sortieren"]));

        Assert.Equal(["\"Was bedeutet \"\"stabil\"\"?\";\"Gleiche Schlüssel; gleiche Reihenfolge\";\"sortieren\""], DataLines(csv));
    }

    [Fact]
    public void Serialize_ConvertsLineBreaksToBr()
    {
        var csv = SerializeToString(new FlashcardDto("Q", "Zeile 1\nZeile 2\r\nZeile 3\rZeile 4", []));

        Assert.Equal(["\"Q\";\"Zeile 1<br>Zeile 2<br>Zeile 3<br>Zeile 4\";\"\""], DataLines(csv));
    }

    [Fact]
    public void Serialize_JoinsTagsWithSpacesAndReplacesInnerSpaces()
    {
        var csv = SerializeToString(new FlashcardDto("Q", "A", ["kürzeste wege", "dijkstra"]));

        Assert.EndsWith(";\"kürzeste_wege dijkstra\"", DataLines(csv).Single());
    }

    [Fact]
    public void Serialize_WritesUtf8WithoutBom()
    {
        var bytes = _sut.Serialize("Deck", [new FlashcardDto("Größe", "Ä", [])]);

        Assert.NotEqual(0xEF, bytes[0]);
        Assert.Contains("Größe", Encoding.UTF8.GetString(bytes));
    }

    [Fact]
    public void CreateDeckName_CombinesRootParentAndTitle()
    {
        Assert.Equal("StudyHub::Algorithms::Dijkstra vs Bellman-Ford", _sut.CreateDeckName("Algorithms", "Dijkstra vs Bellman-Ford"));
    }

    [Fact]
    public void CreateDeckName_WithoutParent_SkipsParentLevel()
    {
        Assert.Equal("StudyHub::Syllabus", _sut.CreateDeckName(null, "Syllabus"));
    }

    [Fact]
    public void CreateDeckName_RemovesSubDeckSeparatorsInsideParts()
    {
        Assert.Equal("StudyHub::C:Basics::Pointers", _sut.CreateDeckName("C::Basics", "Pointers"));
    }

    [Theory]
    [InlineData("Lecture 1: Graphs/Trees?", "Lecture 1_ Graphs_Trees.csv")]
    [InlineData("notes.csv", "notes.csv")]
    [InlineData("   ", "flashcards.csv")]
    [InlineData("../../etc/passwd", "etc_passwd.csv")]
    public void CreateFileName_ProducesSafeCsvName(string input, string expected)
    {
        Assert.Equal(expected, _sut.CreateFileName(input));
    }

    [Fact]
    public void NormalizeDeckName_CollapsesLineBreaks()
    {
        Assert.Equal("StudyHub::A B", _sut.NormalizeDeckName("StudyHub::A\nB"));
    }

    [Fact]
    public void NormalizeDeckName_WithEmptyName_Throws()
    {
        Assert.Throws<FlashcardValidationException>(() => _sut.NormalizeDeckName("  "));
    }
}
