using StudyHub.Logic.Domain;
using StudyHub.Shared.Flashcards;
using StudyHub.Shared.Notes;
using StudyHub.Shared.PracticeExams;

namespace StudyHub.Tests.Logic.Domain.PracticeExams;

public class PracticeExamSourceProcessorTests
{
    private static readonly DateTime Now = new(2026, 10, 9, 8, 0, 0, DateTimeKind.Utc);

    private readonly PracticeExamSourceProcessor _sut = new();

    private static Note Note(string title, string content) =>
        new(Guid.NewGuid(), title, content, null, Guid.NewGuid(), null, false, Now, Now);

    private static Flashcard Card(
        string front,
        int lapses = 0,
        int easeFactor = 2500,
        string back = "Antwort",
        string? tags = null
    ) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            front,
            back,
            tags,
            null,
            FlashcardState.Review,
            0,
            Now,
            1,
            easeFactor,
            1,
            lapses,
            null,
            Now,
            Now
        );

    [Fact]
    public void CreateFromNotes_NumbersTheNotesByTitleAndLeavesOutEmptyOnes()
    {
        var dijkstra = Note("Dijkstra", "# Dijkstra");
        var bellmanFord = Note("Bellman-Ford", "# Bellman-Ford");

        var material = _sut.CreateFromNotes(
            "Algorithmen",
            [dijkstra, Note("Leer", "  "), bellmanFord]
        );

        Assert.Equal("Algorithmen", material.SourceName);
        Assert.Equal([1, 2], material.Sources.Select(s => s.Id));
        Assert.Equal([bellmanFord.Id, dijkstra.Id], material.Sources.Select(s => s.NoteId));
        Assert.Equal("Bellman-Ford", material.Sources[0].Title);
        Assert.Equal("# Bellman-Ford", material.Sources[0].Content);
        Assert.All(material.Sources, s => Assert.Null(s.FlashcardId));
        Assert.Equal(12 + 14 + 8 + 10, material.Length);
    }

    [Fact]
    public void CreateFromNotes_ExactlyAtTheLimit_UsesAllNotes()
    {
        var note = Note("T", new string('x', GeneratePracticeExamRequest.MaxMaterialLength - 1));

        var material = _sut.CreateFromNotes("Kurs", [note]);

        Assert.Equal(GeneratePracticeExamRequest.MaxMaterialLength, material.Length);
    }

    [Fact]
    public void CreateFromNotes_OneCharacterOverTheLimit_Throws()
    {
        var note = Note("T", new string('x', GeneratePracticeExamRequest.MaxMaterialLength));

        var ex = Assert.Throws<PracticeExamValidationException>(() =>
            _sut.CreateFromNotes("Kurs", [note])
        );

        Assert.Contains("150,001", ex.Message);
    }

    [Fact]
    public void CreateFromNotes_WithoutContent_Throws()
    {
        Assert.Throws<PracticeExamValidationException>(() =>
            _sut.CreateFromNotes("Kurs", [Note("Leer", "")])
        );
    }

    [Fact]
    public void CreateFromCards_PutsStrugglingCardsFirstThenKeepsTheGivenOrder()
    {
        var first = Card("Erste");
        var second = Card("Zweite");
        var lowEase = Card("Schwer", easeFactor: 1800);
        var oneLapse = Card("Vergessen", lapses: 1, easeFactor: 2300);
        var twoLapses = Card("Oft vergessen", lapses: 2, easeFactor: 2500);

        var material = _sut.CreateFromCards(
            "Graphen",
            [first, lowEase, second, oneLapse, twoLapses]
        );

        Assert.Equal(
            [twoLapses.Id, oneLapse.Id, lowEase.Id, first.Id, second.Id],
            material.Sources.Select(s => s.FlashcardId)
        );
        Assert.Equal([1, 2, 3, 4, 5], material.Sources.Select(s => s.Id));
        Assert.Equal(0, material.OmittedCount);
    }

    [Fact]
    public void CreateFromCards_FormatsFrontBackAndTags()
    {
        var material = _sut.CreateFromCards(
            "Graphen",
            [Card("Was ist BFS?", tags: "graphen suche")]
        );

        var source = Assert.Single(material.Sources);
        Assert.Equal("Front: Was ist BFS?\nBack: Antwort\nTags: graphen suche", source.Content);
        Assert.Equal(string.Empty, source.Title);
        Assert.Null(source.NoteId);
        Assert.Equal(12 + 7 + 13, material.Length);
    }

    [Fact]
    public void CreateFromCards_AboveTheLimit_LeavesOutTheLastCardsInOrder()
    {
        var half = GeneratePracticeExamRequest.MaxMaterialLength / 2;
        var struggling = Card(new string('a', half - 7), lapses: 3);
        var fits = Card(new string('b', half - 7));
        var tooMuch = Card("c");

        var material = _sut.CreateFromCards("Graphen", [fits, tooMuch, struggling]);

        Assert.Equal([struggling.Id, fits.Id], material.Sources.Select(s => s.FlashcardId));
        Assert.Equal(GeneratePracticeExamRequest.MaxMaterialLength, material.Length);
        Assert.Equal(1, material.OmittedCount);
    }

    [Fact]
    public void CreateFromCards_WithoutCards_Throws()
    {
        Assert.Throws<PracticeExamValidationException>(() => _sut.CreateFromCards("Leer", []));
    }
}
