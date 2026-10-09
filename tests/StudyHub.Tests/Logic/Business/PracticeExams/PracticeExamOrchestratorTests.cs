using Moq;
using StudyHub.Data.Contract;
using StudyHub.Logic.Business;
using StudyHub.Logic.Domain;
using StudyHub.Shared.Configuration;
using StudyHub.Shared.Courses;
using StudyHub.Shared.Flashcards;
using StudyHub.Shared.PracticeExams;
using StudyHub.Tests.Builders;
using StudyHub.Tests.Fakes;

namespace StudyHub.Tests.Logic.Business.PracticeExams;

public class PracticeExamOrchestratorTests
{
    private static readonly DateTime Now = new(2026, 10, 9, 10, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IPracticeExamRepository> _practiceExamRepository = new();
    private readonly Mock<IPracticeExamAttemptRepository> _attemptRepository = new();
    private readonly Mock<ICourseRepository> _courseRepository = new();
    private readonly Mock<IFlashcardDeckRepository> _deckRepository = new();
    private readonly PracticeExamOrchestrator _sut;
    private readonly Course _course;

    public PracticeExamOrchestratorTests()
    {
        var timeProvider = new FixedTimeProvider(Now);
        _sut = new PracticeExamOrchestrator(
            _practiceExamRepository.Object,
            _attemptRepository.Object,
            _courseRepository.Object,
            _deckRepository.Object,
            new PracticeExamLifecycle(
                timeProvider,
                new CalendarPeriodProvider(new CalendarOptions(), timeProvider),
                new Random(1)
            ),
            new PracticeExamAttemptProcessor(timeProvider)
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
        _courseRepository.Setup(r => r.GetAllAsync(default)).ReturnsAsync([_course]);
        _courseRepository.Setup(r => r.GetByIdAsync(_course.Id, default)).ReturnsAsync(_course);
        _deckRepository.Setup(r => r.GetAllAsync(default)).ReturnsAsync([]);
        _attemptRepository
            .Setup(r => r.GetByExamIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), default))
            .ReturnsAsync([]);
        _practiceExamRepository
            .Setup(r => r.GetTaskTotalsAsync(It.IsAny<Guid?>(), default))
            .ReturnsAsync([]);
    }

    private PracticeExam SetupExam(bool isArchived = false)
    {
        var exam = PracticeExamBuilder.Exam(isArchived).Exam with { CourseId = _course.Id };
        _practiceExamRepository.Setup(r => r.GetByIdAsync(exam.Id, default)).ReturnsAsync(exam);
        return exam;
    }

    [Fact]
    public async Task GetAllAsync_ReturnsActiveExamsWithStatistics()
    {
        var exam = SetupExam();
        var archived = SetupExam(isArchived: true);
        _practiceExamRepository.Setup(r => r.GetAllAsync(default)).ReturnsAsync([exam, archived]);
        _practiceExamRepository
            .Setup(r => r.GetTaskTotalsAsync(null, default))
            .ReturnsAsync([new PracticeExamTaskTotals(exam.Id, 3, 11, 1)]);
        var open = PracticeExamBuilder.Attempt(startedAt: Now.AddDays(2)) with { ExamId = exam.Id };
        var good = PracticeExamBuilder.Attempt(10, 8, Now.AddDays(1), Now, Now) with
        {
            ExamId = exam.Id,
        };
        var weak = PracticeExamBuilder.Attempt(10, 4, Now, Now, Now) with { ExamId = exam.Id };
        _attemptRepository
            .Setup(r =>
                r.GetByExamIdsAsync(
                    It.Is<IReadOnlyCollection<Guid>>(ids => ids.Single() == exam.Id),
                    default
                )
            )
            .ReturnsAsync([open, good, weak]);

        var dto = Assert.Single(await _sut.GetAllAsync(includeArchived: false));

        Assert.Equal(exam.Id, dto.Id);
        Assert.Equal("Algorithmen", dto.SourceName);
        Assert.Equal(3, dto.TaskCount);
        Assert.Equal(11, dto.TotalPoints);
        Assert.Equal(1, dto.ExcludedTaskCount);
        Assert.Equal(3, dto.AttemptCount);
        Assert.Equal(good.Id, dto.BestAttempt!.Id);
        Assert.Equal(80, dto.BestAttempt.Percent);
        Assert.Equal(PracticeExamAttemptStatus.Graded, dto.BestAttempt.Status);
        Assert.Equal(open.Id, dto.OpenAttemptId);
    }

    [Fact]
    public async Task GetAllAsync_WithArchived_IncludesThem()
    {
        var archived = SetupExam(isArchived: true);
        _practiceExamRepository.Setup(r => r.GetAllAsync(default)).ReturnsAsync([archived]);

        var dto = Assert.Single(await _sut.GetAllAsync(includeArchived: true));

        Assert.True(dto.IsArchived);
        Assert.Null(dto.BestAttempt);
    }

    [Fact]
    public async Task GetByIdAsync_WithUnknownId_Throws()
    {
        await Assert.ThrowsAsync<PracticeExamNotFoundException>(() =>
            _sut.GetByIdAsync(Guid.NewGuid())
        );
    }

    [Fact]
    public async Task GetByIdAsync_ForADeck_NamesTheDeck()
    {
        var deck = new FlashcardDeck(
            Guid.NewGuid(),
            "Graphen",
            null,
            null,
            20,
            200,
            false,
            Now,
            Now
        );
        _deckRepository.Setup(r => r.GetByIdAsync(deck.Id, default)).ReturnsAsync(deck);
        var exam = SetupExam() with
        {
            SourceKind = PracticeExamSourceKind.Deck,
            CourseId = null,
            DeckId = deck.Id,
        };
        _practiceExamRepository.Setup(r => r.GetByIdAsync(exam.Id, default)).ReturnsAsync(exam);

        var dto = await _sut.GetByIdAsync(exam.Id);

        Assert.Equal("Graphen", dto.SourceName);
    }

    [Fact]
    public async Task ArchiveAndRestoreAsync_SaveTheFlag()
    {
        var exam = SetupExam();

        var archived = await _sut.ArchiveAsync(exam.Id);
        _practiceExamRepository
            .Setup(r => r.GetByIdAsync(exam.Id, default))
            .ReturnsAsync(exam with { IsArchived = true });
        var restored = await _sut.RestoreAsync(exam.Id);

        Assert.True(archived.IsArchived);
        Assert.False(restored.IsArchived);
        _practiceExamRepository.Verify(
            r => r.Update(It.Is<PracticeExam>(e => e.Id == exam.Id && e.IsArchived)),
            Times.Once
        );
        _practiceExamRepository.Verify(r => r.SaveChangesAsync(default), Times.Exactly(2));
    }

    [Fact]
    public async Task ExcludeTaskAsync_MarksTheTaskAndSaves()
    {
        var exam = SetupExam();
        var task = PracticeExamBuilder.OpenTask(exam.Id, 1, [2]).Task;
        _practiceExamRepository.Setup(r => r.GetTaskByIdAsync(task.Id, default)).ReturnsAsync(task);

        await _sut.ExcludeTaskAsync(task.Id);

        _practiceExamRepository.Verify(
            r => r.UpdateTask(It.Is<PracticeExamTask>(t => t.Id == task.Id && t.IsExcluded)),
            Times.Once
        );
        _practiceExamRepository.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task ExcludeTaskAsync_WithUnknownTaskOrArchivedExam_Throws()
    {
        var exam = SetupExam(isArchived: true);
        var task = PracticeExamBuilder.OpenTask(exam.Id, 1, [2]).Task;
        _practiceExamRepository.Setup(r => r.GetTaskByIdAsync(task.Id, default)).ReturnsAsync(task);

        await Assert.ThrowsAsync<PracticeExamTaskNotFoundException>(() =>
            _sut.ExcludeTaskAsync(Guid.NewGuid())
        );
        await Assert.ThrowsAsync<PracticeExamArchivedException>(() =>
            _sut.ExcludeTaskAsync(task.Id)
        );
    }

    [Fact]
    public async Task GetAttemptsAsync_WithUnknownExam_Throws()
    {
        await Assert.ThrowsAsync<PracticeExamNotFoundException>(() =>
            _sut.GetAttemptsAsync(Guid.NewGuid())
        );
    }
}
