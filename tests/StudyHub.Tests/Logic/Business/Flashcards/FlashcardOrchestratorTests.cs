using System.Text;
using Microsoft.Extensions.Options;
using Moq;
using StudyHub.Data.Contract;
using StudyHub.Logic.Business;
using StudyHub.Logic.Domain;
using StudyHub.Logic.Integration.Ai;
using StudyHub.Shared.Configuration;
using StudyHub.Shared.Courses;
using StudyHub.Shared.Flashcards;
using StudyHub.Shared.Notes;
using StudyHub.Shared.Semesters;

namespace StudyHub.Tests.Logic.Business.Flashcards;

public class FlashcardOrchestratorTests
{
    private static readonly Guid CourseId = Guid.NewGuid();
    private static readonly Guid SemesterId = Guid.NewGuid();

    private readonly Mock<INoteRepository> _noteRepository = new();
    private readonly Mock<ICourseRepository> _courseRepository = new();
    private readonly Mock<ISemesterRepository> _semesterRepository = new();
    private readonly Mock<IFlashcardGenerator> _generator = new();
    private readonly FlashcardOrchestrator _sut;

    public FlashcardOrchestratorTests()
    {
        _sut = new FlashcardOrchestrator(
            _noteRepository.Object,
            _courseRepository.Object,
            _semesterRepository.Object,
            new FlashcardValidator(),
            new AnkiCsvSerializer(),
            _generator.Object,
            new ConfiguredAiModelCatalog(Options.Create(new AnthropicOptions
            {
                DefaultModel = "claude-sonnet-5-5",
                Models = [new AnthropicModelOption { Id = "claude-opus-5-5", DisplayName = "Opus" }],
            })));

        _courseRepository.Setup(r => r.GetByIdAsync(CourseId, default))
            .ReturnsAsync(new Course(CourseId, "Algorithms", null, "#2563eb", SemesterId, false, DateTime.UtcNow, DateTime.UtcNow));
        _semesterRepository.Setup(r => r.GetByIdAsync(SemesterId, default))
            .ReturnsAsync(new Semester(SemesterId, "WS 2025/26", new DateOnly(2025, 10, 1), new DateOnly(2026, 3, 31), false, DateTime.UtcNow, DateTime.UtcNow));

        _generator.Setup(g => g.GenerateAsync(It.IsAny<FlashcardGenerationInput>(), default))
            .ReturnsAsync([new FlashcardDto("Frage 1", "Antwort 1", ["graphen"]), new FlashcardDto("Frage 2", "Antwort 2", [])]);
    }

    private Note SetupNote(string content = "# Dijkstra\nNur nicht-negative Kanten.", bool isArchived = false, Guid? courseId = null, Guid? semesterId = null)
    {
        var note = new Note(
            Guid.NewGuid(), "Dijkstra", content, null,
            courseId ?? (semesterId is null ? CourseId : null), semesterId,
            isArchived, DateTime.UtcNow, DateTime.UtcNow);
        _noteRepository.Setup(r => r.GetByIdAsync(note.Id, default)).ReturnsAsync(note);
        return note;
    }

    [Fact]
    public async Task GenerateAsync_WithCourseNote_ReturnsCardsAndCourseDeckName()
    {
        var note = SetupNote();

        var result = await _sut.GenerateAsync(new GenerateFlashcardsRequest(note.Id, 10, "Laufzeit"));

        Assert.Equal(note.Id, result.NoteId);
        Assert.Equal("StudyHub::Algorithms::Dijkstra", result.DeckName);
        Assert.Equal("Dijkstra.csv", result.FileName);
        Assert.Equal("claude-sonnet-5-5", result.Model);
        Assert.Equal(["Frage 1", "Frage 2"], result.Cards.Select(c => c.Front));
        _generator.Verify(g => g.GenerateAsync(
            It.Is<FlashcardGenerationInput>(i => i.Model == "claude-sonnet-5-5" && i.NoteTitle == "Dijkstra" && i.NoteContent == note.Content && i.CardCount == 10 && i.FocusHint == "Laufzeit"),
            default), Times.Once);
    }

    [Fact]
    public async Task GenerateAsync_WithSelectedModel_PassesModelToGenerator()
    {
        var note = SetupNote();

        var result = await _sut.GenerateAsync(new GenerateFlashcardsRequest(note.Id, 10, null, "claude-opus-5-5"));

        Assert.Equal("claude-opus-5-5", result.Model);
        _generator.Verify(g => g.GenerateAsync(It.Is<FlashcardGenerationInput>(i => i.Model == "claude-opus-5-5"), default), Times.Once);
    }

    [Fact]
    public async Task GenerateAsync_WithUnknownModel_ThrowsValidationWithoutCallingAi()
    {
        var note = SetupNote();

        await Assert.ThrowsAsync<FlashcardValidationException>(
            () => _sut.GenerateAsync(new GenerateFlashcardsRequest(note.Id, 10, null, "gpt-unknown")));
        _generator.Verify(g => g.GenerateAsync(It.IsAny<FlashcardGenerationInput>(), default), Times.Never);
    }

    [Fact]
    public async Task GenerateAsync_WithSemesterNote_UsesSemesterNameInDeck()
    {
        var note = SetupNote(semesterId: SemesterId);

        var result = await _sut.GenerateAsync(new GenerateFlashcardsRequest(note.Id, 10, null));

        Assert.Equal("StudyHub::WS 2025/26::Dijkstra", result.DeckName);
    }

    [Fact]
    public async Task GenerateAsync_WithUnknownNote_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NoteNotFoundException>(() => _sut.GenerateAsync(new GenerateFlashcardsRequest(Guid.NewGuid(), 10, null)));
    }

    [Fact]
    public async Task GenerateAsync_WithArchivedNote_ThrowsArchived()
    {
        var note = SetupNote(isArchived: true);

        await Assert.ThrowsAsync<NoteArchivedException>(() => _sut.GenerateAsync(new GenerateFlashcardsRequest(note.Id, 10, null)));
        _generator.Verify(g => g.GenerateAsync(It.IsAny<FlashcardGenerationInput>(), default), Times.Never);
    }

    [Fact]
    public async Task GenerateAsync_WithEmptyNote_ThrowsValidationWithoutCallingAi()
    {
        var note = SetupNote(content: "   ");

        await Assert.ThrowsAsync<FlashcardValidationException>(() => _sut.GenerateAsync(new GenerateFlashcardsRequest(note.Id, 10, null)));
        _generator.Verify(g => g.GenerateAsync(It.IsAny<FlashcardGenerationInput>(), default), Times.Never);
    }

    [Fact]
    public async Task GenerateAsync_WithInvalidCardCount_ThrowsValidation()
    {
        var note = SetupNote();

        await Assert.ThrowsAsync<FlashcardValidationException>(() => _sut.GenerateAsync(new GenerateFlashcardsRequest(note.Id, 0, null)));
    }

    [Fact]
    public async Task GenerateAsync_WhenGeneratorFails_PropagatesException()
    {
        var note = SetupNote();
        _generator.Setup(g => g.GenerateAsync(It.IsAny<FlashcardGenerationInput>(), default))
            .ThrowsAsync(new FlashcardGenerationFailedException(FlashcardGenerationFailureReason.RateLimited, "rate limited"));

        var ex = await Assert.ThrowsAsync<FlashcardGenerationFailedException>(() => _sut.GenerateAsync(new GenerateFlashcardsRequest(note.Id, 10, null)));

        Assert.Equal(FlashcardGenerationFailureReason.RateLimited, ex.Reason);
    }

    [Fact]
    public async Task GenerateAsync_WhenGeneratorReturnsOnlyInvalidCards_ThrowsInvalidResponse()
    {
        var note = SetupNote();
        _generator.Setup(g => g.GenerateAsync(It.IsAny<FlashcardGenerationInput>(), default))
            .ReturnsAsync([new FlashcardDto("", "", [])]);

        var ex = await Assert.ThrowsAsync<FlashcardGenerationFailedException>(() => _sut.GenerateAsync(new GenerateFlashcardsRequest(note.Id, 10, null)));

        Assert.Equal(FlashcardGenerationFailureReason.InvalidResponse, ex.Reason);
    }

    [Fact]
    public async Task GenerateAsync_WhenGeneratorReturnsTooManyCards_CapsAtRequestedCount()
    {
        var note = SetupNote();

        var result = await _sut.GenerateAsync(new GenerateFlashcardsRequest(note.Id, 1, null));

        Assert.Single(result.Cards);
    }

    [Fact]
    public async Task ExportAsync_WithEditedCards_ReturnsCsvWithEdits()
    {
        var request = new ExportFlashcardsRequest(
            "StudyHub::Algorithms::Dijkstra",
            "Dijkstra.csv",
            [new FlashcardDto("Bearbeitete Frage", "Antwort", ["graphen"])]);

        var result = await _sut.ExportAsync(request);

        Assert.Equal("Dijkstra.csv", result.FileName);
        Assert.Equal("text/csv", result.ContentType);
        var csv = Encoding.UTF8.GetString(result.Content);
        Assert.Contains("#deck:StudyHub::Algorithms::Dijkstra\n", csv);
        Assert.Contains("\"Bearbeitete Frage\";\"Antwort\";\"graphen\"", csv);
    }

    [Fact]
    public async Task ExportAsync_SanitizesClientSuppliedFileName()
    {
        var request = new ExportFlashcardsRequest("Deck", "../evil\".csv", [new FlashcardDto("Q", "A", [])]);

        var result = await _sut.ExportAsync(request);

        Assert.Equal("evil.csv", result.FileName);
    }

    [Fact]
    public async Task ExportAsync_WithInvalidCard_ThrowsValidation()
    {
        var request = new ExportFlashcardsRequest("Deck", "x.csv", [new FlashcardDto("Q", "", [])]);

        await Assert.ThrowsAsync<FlashcardValidationException>(() => _sut.ExportAsync(request));
    }
}
