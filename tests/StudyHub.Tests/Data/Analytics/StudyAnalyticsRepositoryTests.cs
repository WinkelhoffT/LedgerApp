using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StudyHub.Data;
using StudyHub.Shared.Analytics;
using StudyHub.Shared.Courses;
using StudyHub.Shared.Flashcards;
using StudyHub.Shared.PracticeExams;
using StudyHub.Shared.Semesters;
using StudyHub.Shared.StudySessions;

namespace StudyHub.Tests.Data.Analytics;

/// <summary>
/// Runs against SQLite in memory with the real migrations, so the joins, the grouping and the
/// check constraints are translated and enforced as in production.
/// </summary>
public sealed class StudyAnalyticsRepositoryTests : IAsyncLifetime
{
    private static readonly DateTime Now = new(2026, 10, 9, 10, 0, 0, DateTimeKind.Utc);

    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private ApplicationDbContext _dbContext = default!;
    private StudyAnalyticsRepository _sut = default!;
    private Semester _semester = default!;
    private Course _course = default!;

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        _dbContext = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options
        );
        await _dbContext.Database.MigrateAsync();

        _semester = new Semester(
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
            _semester.Id,
            false,
            Now,
            Now
        );
        _dbContext.AddRange(_semester, _course);
        await _dbContext.SaveChangesAsync();
        _sut = new StudyAnalyticsRepository(_dbContext);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private async Task SaveAsync(params object[] entities)
    {
        _dbContext.AddRange(entities);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();
    }

    private static FlashcardDeck Deck(
        Guid? courseId = null,
        Guid? semesterId = null,
        bool isArchived = false
    ) =>
        new(
            Guid.NewGuid(),
            $"Deck {Guid.NewGuid()}",
            courseId,
            semesterId,
            20,
            200,
            isArchived,
            Now,
            Now
        );

    private static Flashcard Card(
        FlashcardDeck deck,
        FlashcardState state = FlashcardState.New,
        int intervalDays = 0
    ) =>
        new(
            Guid.NewGuid(),
            deck.Id,
            "Frage",
            "Antwort",
            null,
            null,
            state,
            0,
            Now,
            intervalDays,
            2500,
            0,
            0,
            null,
            Now,
            Now
        );

    private static FlashcardReview Review(Flashcard card, DateTime reviewedAt) =>
        new(
            Guid.NewGuid(),
            card.Id,
            reviewedAt,
            FlashcardRating.Good,
            FlashcardState.Review,
            1,
            3,
            2500
        );

    private static PracticeExam Exam(
        Guid? courseId = null,
        Guid? deckId = null,
        bool isArchived = false
    ) =>
        new(
            Guid.NewGuid(),
            "Probeklausur",
            PracticeExamLevel.University,
            60,
            deckId is null ? PracticeExamSourceKind.Course : PracticeExamSourceKind.Deck,
            courseId,
            deckId,
            "claude-sonnet-5-5",
            "practice-exam-v1",
            null,
            isArchived,
            Now,
            Now
        );

    private static PracticeExamAttempt Attempt(
        PracticeExam exam,
        DateTime startedAt,
        DateTime? submittedAt = null,
        DateTime? gradedAt = null,
        int? awardedPoints = null
    ) => new(Guid.NewGuid(), exam.Id, startedAt, null, submittedAt, gradedAt, 40, awardedPoints);

    private static StudySession Session(
        DateOnly date,
        TimeOnly startTime,
        DateTime? completedAt = null,
        int? actualDurationMinutes = null
    ) =>
        new(
            Guid.NewGuid(),
            "Graph review",
            null,
            null,
            date,
            startTime,
            60,
            null,
            completedAt,
            actualDurationMinutes,
            Now,
            Now
        );

    [Fact]
    public async Task GetSessionsAsync_ReturnsTheRangeWithCompletionOrderedByDateAndStart()
    {
        var done = Session(new DateOnly(2026, 10, 8), new TimeOnly(9, 0), Now, 75);
        var later = Session(new DateOnly(2026, 10, 8), new TimeOnly(14, 0));
        var first = Session(new DateOnly(2026, 10, 7), new TimeOnly(18, 0));
        var outside = Session(new DateOnly(2026, 10, 10), new TimeOnly(9, 0));
        await SaveAsync(later, done, outside, first);

        var sessions = await _sut.GetSessionsAsync(
            new DateOnly(2026, 10, 7),
            new DateOnly(2026, 10, 9)
        );

        Assert.Equal([first.Id, done.Id, later.Id], sessions.Select(s => s.Id));
        Assert.Equal(Now, sessions[1].CompletedAt);
        Assert.Equal(75, sessions[1].ActualDurationMinutes);
        Assert.Null(sessions[2].CompletedAt);
        Assert.Null(sessions[2].ActualDurationMinutes);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task Saving_AHalfCompletedSession_IsRejected(bool hasCompletedAt, bool hasDuration)
    {
        var session = Session(
            new DateOnly(2026, 10, 8),
            new TimeOnly(9, 0),
            hasCompletedAt ? Now : null,
            hasDuration ? 60 : null
        );

        await Assert.ThrowsAsync<DbUpdateException>(() => SaveAsync(session));
    }

    [Theory]
    [InlineData(StudySession.MinDurationMinutes - 1)]
    [InlineData(StudySession.MaxDurationMinutes + 1)]
    public async Task Saving_AnActualDurationOutsideTheLimits_IsRejected(int minutes)
    {
        var session = Session(new DateOnly(2026, 10, 8), new TimeOnly(9, 0), Now, minutes);

        await Assert.ThrowsAsync<DbUpdateException>(() => SaveAsync(session));
    }

    [Fact]
    public async Task GetReviewActivitiesAsync_ReturnsAnswersInTheRangeWithTheDeckCourseOrderedByTime()
    {
        var courseDeck = Deck(_course.Id);
        var semesterDeck = Deck(semesterId: _semester.Id);
        var courseCard = Card(courseDeck);
        var semesterCard = Card(semesterDeck);
        await SaveAsync(
            courseDeck,
            semesterDeck,
            courseCard,
            semesterCard,
            Review(courseCard, Now.AddMinutes(2)),
            Review(semesterCard, Now),
            Review(courseCard, Now.AddMinutes(-1)),
            Review(courseCard, Now.AddHours(1))
        );

        var activities = await _sut.GetReviewActivitiesAsync(Now, Now.AddHours(1));

        Assert.Equal(
            [
                new FlashcardReviewActivity(Now, null),
                new FlashcardReviewActivity(Now.AddMinutes(2), _course.Id),
            ],
            activities
        );
    }

    [Fact]
    public async Task GetSubmittedAttemptActivitiesAsync_ReturnsOnlySubmittedAttemptsThatOverlapTheRange()
    {
        var deck = Deck(_course.Id);
        var courseExam = Exam(courseId: _course.Id);
        var deckExam = Exam(deckId: deck.Id);
        var submitted = Attempt(courseExam, Now, Now.AddMinutes(50));
        var fromDeck = Attempt(deckExam, Now.AddHours(1), Now.AddHours(2));
        var open = Attempt(courseExam, Now.AddHours(3));
        var before = Attempt(courseExam, Now.AddHours(-3), Now.AddHours(-2));
        var startedBefore = Attempt(courseExam, Now.AddMinutes(-30), Now.AddMinutes(10));
        await SaveAsync(
            deck,
            courseExam,
            deckExam,
            submitted,
            fromDeck,
            open,
            before,
            startedBefore
        );

        var activities = await _sut.GetSubmittedAttemptActivitiesAsync(Now, Now.AddDays(1));

        Assert.Equal(
            [
                new PracticeExamAttemptActivity(
                    Now.AddMinutes(-30),
                    Now.AddMinutes(10),
                    60,
                    _course.Id
                ),
                new PracticeExamAttemptActivity(Now, Now.AddMinutes(50), 60, _course.Id),
                new PracticeExamAttemptActivity(Now.AddHours(1), Now.AddHours(2), 60, _course.Id),
            ],
            activities
        );
    }

    [Fact]
    public async Task GetDeckProgressCountsAsync_CountsTheBucketsOfDecksThatAreNotArchived()
    {
        var deck = Deck(_course.Id);
        var looseDeck = Deck();
        var archivedDeck = Deck(_course.Id, isArchived: true);
        var emptyDeck = Deck(_course.Id);
        await SaveAsync(
            deck,
            looseDeck,
            archivedDeck,
            emptyDeck,
            Card(deck),
            Card(deck),
            Card(deck, FlashcardState.Learning),
            Card(deck, FlashcardState.Relearning, 5),
            Card(deck, FlashcardState.Review, 1),
            Card(deck, FlashcardState.Review, FlashcardDeckProgressCounts.MatureIntervalDays - 1),
            Card(deck, FlashcardState.Review, FlashcardDeckProgressCounts.MatureIntervalDays),
            Card(looseDeck, FlashcardState.Review, 40),
            Card(archivedDeck)
        );

        var counts = await _sut.GetDeckProgressCountsAsync();

        Assert.Equal(2, counts.Count);
        Assert.Equal(
            new FlashcardDeckProgressCounts(deck.Id, _course.Id, 2, 2, 2, 1),
            Assert.Single(counts, c => c.DeckId == deck.Id)
        );
        Assert.Equal(
            new FlashcardDeckProgressCounts(looseDeck.Id, null, 0, 0, 0, 1),
            Assert.Single(counts, c => c.DeckId == looseDeck.Id)
        );
    }

    [Fact]
    public async Task GetGradedResultsAsync_ReturnsGradedAttemptsOfExamsThatAreNotArchived()
    {
        var deck = Deck(_course.Id);
        var looseDeck = Deck();
        var courseExam = Exam(courseId: _course.Id);
        var deckExam = Exam(deckId: deck.Id);
        var looseExam = Exam(deckId: looseDeck.Id);
        var archivedExam = Exam(courseId: _course.Id, isArchived: true);
        await SaveAsync(
            deck,
            looseDeck,
            courseExam,
            deckExam,
            looseExam,
            archivedExam,
            Attempt(courseExam, Now, Now.AddMinutes(50), Now.AddMinutes(55), 30),
            Attempt(deckExam, Now, Now.AddMinutes(40), Now.AddMinutes(45), 20),
            Attempt(looseExam, Now, Now.AddMinutes(40), Now.AddMinutes(46), 10),
            Attempt(courseExam, Now.AddHours(1), Now.AddHours(2)),
            Attempt(courseExam, Now.AddHours(3)),
            Attempt(archivedExam, Now, Now.AddMinutes(30), Now.AddMinutes(31), 40)
        );

        var results = await _sut.GetGradedResultsAsync();

        Assert.Equal(
            [
                new PracticeExamResult(_course.Id, Now.AddMinutes(45), 20, 40),
                new PracticeExamResult(null, Now.AddMinutes(46), 10, 40),
                new PracticeExamResult(_course.Id, Now.AddMinutes(55), 30, 40),
            ],
            results
        );
    }
}
