using Microsoft.Extensions.Options;
using Moq;
using StudyHub.Data.Contract;
using StudyHub.Logic.Business;
using StudyHub.Logic.Domain;
using StudyHub.Logic.Integration.Ai;
using StudyHub.Shared.Ai;
using StudyHub.Shared.Configuration;
using StudyHub.Shared.Courses;
using StudyHub.Shared.Flashcards;
using StudyHub.Shared.Notes;
using StudyHub.Shared.PracticeExams;
using StudyHub.Tests.Builders;
using StudyHub.Tests.Fakes;

namespace StudyHub.Tests.Logic.Business.PracticeExams;

public class PracticeExamGenerationOrchestratorTests
{
    private static readonly DateTime Now = new(2026, 10, 9, 10, 0, 0, DateTimeKind.Utc);

    private readonly Mock<ICourseRepository> _courseRepository = new();
    private readonly Mock<INoteRepository> _noteRepository = new();
    private readonly Mock<IFlashcardDeckRepository> _deckRepository = new();
    private readonly Mock<IFlashcardRepository> _flashcardRepository = new();
    private readonly Mock<IPracticeExamGenerator> _generator = new();
    private readonly Mock<IPracticeExamRepository> _practiceExamRepository = new();
    private readonly PracticeExamGenerationOrchestrator _sut;
    private readonly Course _course;
    private readonly List<Note> _notes;
    private PracticeExamGenerationInput? _input;
    private StoredPracticeExam? _saved;

    public PracticeExamGenerationOrchestratorTests()
    {
        var timeProvider = new FixedTimeProvider(Now);
        _sut = new PracticeExamGenerationOrchestrator(
            new PracticeExamValidator(),
            new PracticeExamMaterialProvider(
                _courseRepository.Object,
                _noteRepository.Object,
                _deckRepository.Object,
                _flashcardRepository.Object,
                new PracticeExamSourceProcessor()
            ),
            _generator.Object,
            new ConfiguredAiModelCatalog(
                Options.Create(
                    new AnthropicOptions
                    {
                        DefaultModel = "claude-sonnet-5-5",
                        Models = [new AnthropicModelOption { Id = "claude-opus-5-5" }],
                    }
                )
            ),
            new PracticeExamLifecycle(
                timeProvider,
                new CalendarPeriodProvider(new CalendarOptions(), timeProvider),
                new Random(1)
            ),
            _practiceExamRepository.Object
        );

        _course = new Course(
            Guid.NewGuid(),
            "Algorithmen",
            null,
            "#2563eb",
            Guid.NewGuid(),
            false,
            Now,
            Now
        );
        _notes =
        [
            Note("Dijkstra", "# Dijkstra"),
            Note("Bellman-Ford", "# Bellman-Ford"),
            Note("Archiviert", "# Alt", isArchived: true),
        ];
        _courseRepository.Setup(r => r.GetByIdAsync(_course.Id, default)).ReturnsAsync(_course);
        _noteRepository.Setup(r => r.GetByCourseIdAsync(_course.Id, default)).ReturnsAsync(_notes);

        _generator.SetupGet(g => g.PromptVersion).Returns("practice-exam-v1");
        _generator
            .Setup(g => g.GenerateAsync(It.IsAny<PracticeExamGenerationInput>(), default))
            .Callback<PracticeExamGenerationInput, CancellationToken>((input, _) => _input = input)
            .ReturnsAsync([
                PracticeExamBuilder.SingleChoice(sourceId: 2),
                PracticeExamBuilder.Open(),
            ]);
        _practiceExamRepository
            .Setup(r => r.AddAsync(It.IsAny<StoredPracticeExam>(), default))
            .Callback<StoredPracticeExam, CancellationToken>((exam, _) => _saved = exam)
            .Returns(Task.CompletedTask);
    }

    private Note Note(string title, string content, bool isArchived = false) =>
        new(Guid.NewGuid(), title, content, null, _course.Id, null, isArchived, Now, Now);

    private GeneratePracticeExamRequest CourseRequest(
        IReadOnlyList<Guid>? noteIds = null,
        int durationMinutes = 60,
        string? model = null
    ) =>
        new(
            PracticeExamSourceKind.Course,
            _course.Id,
            noteIds,
            null,
            PracticeExamLevel.Gymnasium,
            durationMinutes,
            "nur Graphen",
            model
        );

    private static GeneratePracticeExamRequest DeckRequest(Guid deckId) =>
        new(
            PracticeExamSourceKind.Deck,
            null,
            null,
            deckId,
            PracticeExamLevel.University,
            30,
            null
        );

    private FlashcardDeck SetupDeck(bool isArchived = false, params Flashcard[] cards)
    {
        var deck = new FlashcardDeck(
            Guid.NewGuid(),
            "Graphen",
            null,
            null,
            20,
            200,
            isArchived,
            Now,
            Now
        );
        _deckRepository.Setup(r => r.GetByIdAsync(deck.Id, default)).ReturnsAsync(deck);
        _flashcardRepository
            .Setup(r => r.GetByDeckIdAsync(deck.Id, null, default))
            .ReturnsAsync(cards);
        return deck;
    }

    private static Flashcard Card(Guid deckId) =>
        new(
            Guid.NewGuid(),
            deckId,
            "Was ist BFS?",
            "Breitensuche",
            null,
            null,
            FlashcardState.New,
            0,
            Now,
            0,
            2500,
            0,
            0,
            null,
            Now,
            Now
        );

    [Fact]
    public async Task GenerateAsync_SavesTheExamWithLevelDurationModelAndPromptVersion()
    {
        var dto = await _sut.GenerateAsync(CourseRequest(model: "claude-opus-5-5"));

        Assert.NotNull(_saved);
        Assert.Equal(PracticeExamLevel.Gymnasium, _saved.Exam.Level);
        Assert.Equal(60, _saved.Exam.DurationMinutes);
        Assert.Equal("claude-opus-5-5", _saved.Exam.Model);
        Assert.Equal("practice-exam-v1", _saved.Exam.PromptVersion);
        Assert.Equal(_course.Id, _saved.Exam.CourseId);
        _practiceExamRepository.Verify(r => r.SaveChangesAsync(default), Times.Once);

        Assert.Equal(_saved.Exam.Id, dto.Id);
        Assert.Equal("Algorithmen", dto.SourceName);
        Assert.Equal(2, dto.TaskCount);
        Assert.Equal(2 + 5, dto.TotalPoints);
        Assert.Equal(0, dto.AttemptCount);
        Assert.Null(dto.OpenAttemptId);
    }

    [Fact]
    public async Task GenerateAsync_SendsTheActiveNotesNumberedWithTheOptions()
    {
        await _sut.GenerateAsync(CourseRequest());

        Assert.NotNull(_input);
        Assert.Equal("claude-sonnet-5-5", _input.Model);
        Assert.Equal(PracticeExamLevel.Gymnasium, _input.Level);
        Assert.Equal(PracticeExamSourceKind.Course, _input.SourceKind);
        Assert.Equal("nur Graphen", _input.FocusHint);
        Assert.Equal(["Bellman-Ford", "Dijkstra"], _input.Sources.Select(s => s.Title));
        Assert.Equal(_notes[0].Id, _saved!.Tasks[0].Task.SourceNoteId);
    }

    [Fact]
    public async Task GenerateAsync_WithSelectedNotes_SendsOnlyThose()
    {
        await _sut.GenerateAsync(CourseRequest([_notes[0].Id]));

        Assert.Equal([_notes[0].Id], _input!.Sources.Select(s => s.NoteId));
    }

    [Fact]
    public async Task GenerateAsync_WithEmptySelection_Throws()
    {
        await Assert.ThrowsAsync<PracticeExamValidationException>(() =>
            _sut.GenerateAsync(CourseRequest([]))
        );
    }

    [Theory]
    [InlineData(2)]
    [InlineData(-1)]
    public async Task GenerateAsync_WithArchivedOrForeignNote_Throws(int index)
    {
        var noteId = index >= 0 ? _notes[index].Id : Guid.NewGuid();

        await Assert.ThrowsAsync<PracticeExamValidationException>(() =>
            _sut.GenerateAsync(CourseRequest([noteId]))
        );
        _generator.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GenerateAsync_WithSelectedEmptyNote_Throws()
    {
        var empty = Note("Leer", " ");
        _notes.Add(empty);

        var ex = await Assert.ThrowsAsync<PracticeExamValidationException>(() =>
            _sut.GenerateAsync(CourseRequest([_notes[0].Id, empty.Id]))
        );

        Assert.Contains("Leer", ex.Message);
    }

    [Fact]
    public async Task GenerateAsync_WithCourseWithoutNotes_Throws()
    {
        _notes.Clear();

        await Assert.ThrowsAsync<PracticeExamValidationException>(() =>
            _sut.GenerateAsync(CourseRequest())
        );
    }

    [Fact]
    public async Task GenerateAsync_OverTheMaterialLimit_Throws()
    {
        _notes.Add(Note("Lang", new string('x', GeneratePracticeExamRequest.MaxMaterialLength)));

        await Assert.ThrowsAsync<PracticeExamValidationException>(() =>
            _sut.GenerateAsync(CourseRequest())
        );
        _generator.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GenerateAsync_WithArchivedCourse_Throws()
    {
        _courseRepository
            .Setup(r => r.GetByIdAsync(_course.Id, default))
            .ReturnsAsync(_course with { IsArchived = true });

        await Assert.ThrowsAsync<CourseArchivedException>(() =>
            _sut.GenerateAsync(CourseRequest())
        );
    }

    [Fact]
    public async Task GenerateAsync_WithMissingCourse_Throws()
    {
        await Assert.ThrowsAsync<CourseNotFoundException>(() =>
            _sut.GenerateAsync(CourseRequest() with { CourseId = Guid.NewGuid() })
        );
    }

    [Fact]
    public async Task GenerateAsync_WithUnknownModelOrDuration_ThrowsBeforeLoadingMaterial()
    {
        await Assert.ThrowsAsync<PracticeExamValidationException>(() =>
            _sut.GenerateAsync(CourseRequest(model: "gpt"))
        );
        await Assert.ThrowsAsync<PracticeExamValidationException>(() =>
            _sut.GenerateAsync(CourseRequest(durationMinutes: 50))
        );
        _courseRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GenerateAsync_FromADeck_SendsItsCards()
    {
        var deckId = Guid.NewGuid();
        var deck = SetupDeck(false, Card(deckId), Card(deckId));

        var dto = await _sut.GenerateAsync(DeckRequest(deck.Id));

        Assert.Equal(PracticeExamSourceKind.Deck, _input!.SourceKind);
        Assert.Equal("Graphen", _input.SourceName);
        Assert.All(_input.Sources, s => Assert.NotNull(s.FlashcardId));
        Assert.Equal(deck.Id, _saved!.Exam.DeckId);
        Assert.Equal("Graphen", dto.SourceName);
    }

    [Fact]
    public async Task GenerateAsync_WithArchivedDeck_Throws()
    {
        var deck = SetupDeck(true, Card(Guid.NewGuid()));

        await Assert.ThrowsAsync<FlashcardDeckArchivedException>(() =>
            _sut.GenerateAsync(DeckRequest(deck.Id))
        );
    }

    [Fact]
    public async Task GenerateAsync_WithMissingOrEmptyDeck_Throws()
    {
        var empty = SetupDeck();

        await Assert.ThrowsAsync<FlashcardDeckNotFoundException>(() =>
            _sut.GenerateAsync(DeckRequest(Guid.NewGuid()))
        );
        await Assert.ThrowsAsync<PracticeExamValidationException>(() =>
            _sut.GenerateAsync(DeckRequest(empty.Id))
        );
    }

    [Fact]
    public async Task GenerateAsync_PassesOnGeneratorFailures()
    {
        _generator
            .Setup(g => g.GenerateAsync(It.IsAny<PracticeExamGenerationInput>(), default))
            .ThrowsAsync(
                new AiGenerationFailedException(AiGenerationFailureReason.RateLimited, "Limit")
            );

        var ex = await Assert.ThrowsAsync<AiGenerationFailedException>(() =>
            _sut.GenerateAsync(CourseRequest())
        );

        Assert.Equal(AiGenerationFailureReason.RateLimited, ex.Reason);
        Assert.Null(_saved);
    }

    [Fact]
    public async Task GenerateAsync_DropsInvalidTasks()
    {
        _generator
            .Setup(g => g.GenerateAsync(It.IsAny<PracticeExamGenerationInput>(), default))
            .ReturnsAsync([
                PracticeExamBuilder.SingleChoice(optionCount: 3),
                PracticeExamBuilder.Open(),
            ]);

        var dto = await _sut.GenerateAsync(CourseRequest());

        Assert.Equal(1, dto.TaskCount);
        Assert.Equal(PracticeExamTaskKind.Open, Assert.Single(_saved!.Tasks).Task.Kind);
    }

    [Fact]
    public async Task GenerateAsync_WithoutUsableTasks_ThrowsInvalidResponse()
    {
        _generator
            .Setup(g => g.GenerateAsync(It.IsAny<PracticeExamGenerationInput>(), default))
            .ReturnsAsync([PracticeExamBuilder.Open() with { Kind = null }]);

        var ex = await Assert.ThrowsAsync<AiGenerationFailedException>(() =>
            _sut.GenerateAsync(CourseRequest())
        );

        Assert.Equal(AiGenerationFailureReason.InvalidResponse, ex.Reason);
        Assert.Null(_saved);
    }
}
