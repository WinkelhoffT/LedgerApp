using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StudyHub.Data;
using StudyHub.Shared.Courses;
using StudyHub.Shared.Flashcards;
using StudyHub.Shared.Notes;
using StudyHub.Shared.PracticeExams;
using StudyHub.Shared.Semesters;

namespace StudyHub.Tests.Data.PracticeExams;

/// <summary>
/// Runs against SQLite in memory with the real migrations, so keys, check constraints and delete
/// rules are enforced as in production.
/// </summary>
public sealed class PracticeExamRepositoryTests : IAsyncLifetime
{
    private static readonly DateTime Now = new(2026, 10, 9, 10, 0, 0, DateTimeKind.Utc);

    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private ApplicationDbContext _dbContext = default!;
    private Course _course = default!;
    private Note _note = default!;
    private FlashcardDeck _deck = default!;
    private Flashcard _card = default!;

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        _dbContext = CreateDbContext();
        await _dbContext.Database.MigrateAsync();

        var semester = new Semester(
            Guid.NewGuid(),
            "Winter 2026/27",
            new DateOnly(2026, 10, 1),
            new DateOnly(2027, 3, 31),
            false,
            Now,
            Now
        );
        _course = new Course(
            Guid.NewGuid(),
            "Algorithmen",
            null,
            "#2563eb",
            semester.Id,
            false,
            Now,
            Now
        );
        _note = new Note(
            Guid.NewGuid(),
            "Dijkstra",
            "# Dijkstra",
            null,
            _course.Id,
            null,
            false,
            Now,
            Now
        );
        _deck = new FlashcardDeck(Guid.NewGuid(), "Graphen", null, null, 20, 200, false, Now, Now);
        _card = new Flashcard(
            Guid.NewGuid(),
            _deck.Id,
            "Frage",
            "Antwort",
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
        _dbContext.AddRange(semester, _course, _note, _deck, _card);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private ApplicationDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options);

    private StoredPracticeExam Exam(
        PracticeExamSourceKind sourceKind = PracticeExamSourceKind.Course,
        Guid? courseId = null,
        Guid? deckId = null
    )
    {
        var exam = new PracticeExam(
            Guid.NewGuid(),
            "Probeklausur Algorithmen · Universität · 09.10.2026",
            PracticeExamLevel.University,
            60,
            sourceKind,
            sourceKind == PracticeExamSourceKind.Course ? courseId ?? _course.Id : courseId,
            sourceKind == PracticeExamSourceKind.Deck ? deckId ?? _deck.Id : deckId,
            "claude-sonnet-5-5",
            "practice-exam-v1",
            null,
            false,
            Now,
            Now
        );

        var singleChoice = new PracticeExamTask(
            Guid.NewGuid(),
            exam.Id,
            1,
            PracticeExamTaskKind.SingleChoice,
            "Welche Laufzeit?",
            2,
            "Erklärung",
            _note.Id,
            null,
            false,
            Now
        );
        var open = new PracticeExamTask(
            Guid.NewGuid(),
            exam.Id,
            2,
            PracticeExamTaskKind.Open,
            "Erklären Sie Dijkstra.",
            5,
            "Musterlösung",
            null,
            _card.Id,
            false,
            Now
        );
        var excluded = open with
        {
            Id = Guid.NewGuid(),
            Position = 3,
            Points = 4,
            SourceFlashcardId = null,
            IsExcluded = true,
        };

        return new StoredPracticeExam(
            exam,
            [
                new StoredPracticeExamTask(
                    singleChoice,
                    Enumerable
                        .Range(1, 4)
                        .Reverse()
                        .Select(i => new PracticeExamOption(
                            Guid.NewGuid(),
                            singleChoice.Id,
                            i,
                            $"Option {i}",
                            "Begründung",
                            i == 2
                        ))
                        .ToList(),
                    []
                ),
                new StoredPracticeExamTask(
                    open,
                    [],
                    [
                        new PracticeExamCriterion(Guid.NewGuid(), open.Id, 2, "Korrektheit", 3),
                        new PracticeExamCriterion(Guid.NewGuid(), open.Id, 1, "Idee", 2),
                    ]
                ),
                new StoredPracticeExamTask(excluded, [], []),
            ]
        );
    }

    private async Task<StoredPracticeExam> SeedExamAsync(StoredPracticeExam? exam = null)
    {
        exam ??= Exam();
        var repository = new PracticeExamRepository(_dbContext);
        await repository.AddAsync(exam);
        await repository.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();
        return exam;
    }

    private static StoredPracticeExamAttempt Attempt(
        StoredPracticeExam exam,
        DateTime startedAt,
        DateTime? submittedAt = null
    )
    {
        var attempt = new PracticeExamAttempt(
            Guid.NewGuid(),
            exam.Exam.Id,
            startedAt,
            null,
            submittedAt,
            null,
            7,
            null
        );
        return new StoredPracticeExamAttempt(
            attempt,
            exam.Tasks.Take(2)
                .Select(t => new PracticeExamAnswer(
                    Guid.NewGuid(),
                    attempt.Id,
                    t.Task.Id,
                    null,
                    null,
                    null,
                    startedAt
                ))
                .ToList(),
            []
        );
    }

    [Fact]
    public async Task GetWithTasksAsync_RoundTripsTasksWithOptionsAndCriteriaInOrder()
    {
        var exam = await SeedExamAsync();

        var stored = await new PracticeExamRepository(CreateDbContext()).GetWithTasksAsync(
            exam.Exam.Id
        );

        Assert.NotNull(stored);
        Assert.Equal(exam.Exam, stored.Exam);
        Assert.Equal([1, 2, 3], stored.Tasks.Select(t => t.Task.Position));
        Assert.Equal([1, 2, 3, 4], stored.Tasks[0].Options.Select(o => o.Position));
        Assert.True(stored.Tasks[0].Options[1].IsCorrect);
        Assert.Equal(["Idee", "Korrektheit"], stored.Tasks[1].Criteria.Select(c => c.Description));
        Assert.Equal(_card.Id, stored.Tasks[1].Task.SourceFlashcardId);
    }

    [Fact]
    public async Task GetTaskTotalsAsync_CountsOnlyTasksThatAreNotExcluded()
    {
        var exam = await SeedExamAsync();
        var other = await SeedExamAsync();

        var all = await new PracticeExamRepository(_dbContext).GetTaskTotalsAsync();
        var one = await new PracticeExamRepository(_dbContext).GetTaskTotalsAsync(exam.Exam.Id);

        Assert.Equal(2, all.Count);
        Assert.Equal(new PracticeExamTaskTotals(exam.Exam.Id, 2, 7, 1), Assert.Single(one));
        Assert.Contains(new PracticeExamTaskTotals(other.Exam.Id, 2, 7, 1), all);
    }

    [Fact]
    public async Task UpdateTask_PersistsTheExclusion()
    {
        var exam = await SeedExamAsync();
        var repository = new PracticeExamRepository(_dbContext);

        repository.UpdateTask(exam.Tasks[0].Task with { IsExcluded = true });
        await repository.SaveChangesAsync();

        var task = await new PracticeExamRepository(CreateDbContext()).GetTaskByIdAsync(
            exam.Tasks[0].Task.Id
        );
        Assert.True(task!.IsExcluded);
    }

    [Fact]
    public async Task AddAsync_WithSourceColumnNotMatchingTheKind_IsRejectedByTheCheckConstraint()
    {
        var repository = new PracticeExamRepository(_dbContext);

        await repository.AddAsync(Exam(PracticeExamSourceKind.Deck, courseId: _course.Id));

        await Assert.ThrowsAsync<DbUpdateException>(() => repository.SaveChangesAsync());
    }

    [Fact]
    public async Task DeletingASourceCard_KeepsTheTaskWithoutItsSource()
    {
        var exam = await SeedExamAsync(Exam(PracticeExamSourceKind.Deck));

        _dbContext.Flashcards.Remove(_card);
        await _dbContext.SaveChangesAsync();

        var stored = await new PracticeExamRepository(CreateDbContext()).GetWithTasksAsync(
            exam.Exam.Id
        );
        Assert.Equal(3, stored!.Tasks.Count);
        Assert.Null(stored.Tasks[1].Task.SourceFlashcardId);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsNewestFirst()
    {
        var older = await SeedExamAsync();
        var newer = Exam();
        await SeedExamAsync(newer with { Exam = newer.Exam with { CreatedAt = Now.AddHours(1) } });

        var exams = await new PracticeExamRepository(_dbContext).GetAllAsync();

        Assert.Equal([newer.Exam.Id, older.Exam.Id], exams.Select(e => e.Id));
    }

    [Fact]
    public async Task Attempts_RoundTripWithAnswersAndFindTheOpenOne()
    {
        var exam = await SeedExamAsync();
        var submitted = Attempt(exam, Now, submittedAt: Now.AddMinutes(30));
        var open = Attempt(exam, Now.AddHours(1));
        var repository = new PracticeExamAttemptRepository(_dbContext);
        await repository.AddAsync(submitted);
        await repository.AddAsync(open);
        await repository.SaveChangesAsync();

        var readRepository = new PracticeExamAttemptRepository(CreateDbContext());
        var found = await readRepository.GetOpenAttemptAsync(exam.Exam.Id);
        var attempts = await readRepository.GetByExamIdsAsync([exam.Exam.Id]);

        Assert.Equal(open.Attempt, found!.Attempt);
        Assert.Equal(2, found.Answers.Count);
        Assert.Equal([open.Attempt.Id, submitted.Attempt.Id], attempts.Select(a => a.Id));
        Assert.Empty(await readRepository.GetByExamIdsAsync([]));
    }

    [Fact]
    public async Task AddAsync_WithTwoAnswersForTheSameTask_IsRejectedByTheUniqueIndex()
    {
        var exam = await SeedExamAsync();
        var attempt = Attempt(exam, Now);
        var duplicate = attempt.Answers[0] with { Id = Guid.NewGuid() };
        var repository = new PracticeExamAttemptRepository(_dbContext);

        await repository.AddAsync(attempt with { Answers = [.. attempt.Answers, duplicate] });

        await Assert.ThrowsAsync<DbUpdateException>(() => repository.SaveChangesAsync());
    }

    [Fact]
    public async Task ReplaceMetCriteriaAsync_ReplacesTheTickedCriteriaOfOneAnswer()
    {
        var exam = await SeedExamAsync();
        var attempt = Attempt(exam, Now, submittedAt: Now);
        var answer = attempt.Answers[1];
        var criteria = exam.Tasks[1].Criteria;
        var repository = new PracticeExamAttemptRepository(_dbContext);
        await repository.AddAsync(
            attempt with
            {
                MetCriteria = [new PracticeExamAnswerCriterion(answer.Id, criteria[0].Id)],
            }
        );
        await repository.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        await repository.ReplaceMetCriteriaAsync(answer.Id, [criteria[1].Id]);
        repository.UpdateAnswer(answer with { AwardedPoints = criteria[1].Points });
        await repository.SaveChangesAsync();

        var stored = await new PracticeExamAttemptRepository(CreateDbContext()).GetWithAnswersAsync(
            attempt.Attempt.Id
        );
        Assert.Equal(
            [new PracticeExamAnswerCriterion(answer.Id, criteria[1].Id)],
            stored!.MetCriteria
        );
        Assert.Equal(
            criteria[1].Points,
            stored.Answers.Single(a => a.Id == answer.Id).AwardedPoints
        );
    }
}
