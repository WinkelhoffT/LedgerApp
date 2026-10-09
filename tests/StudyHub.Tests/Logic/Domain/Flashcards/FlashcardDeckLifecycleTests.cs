using StudyHub.Logic.Domain;
using StudyHub.Shared.Flashcards;
using StudyHub.Tests.Fakes;

namespace StudyHub.Tests.Logic.Domain.Flashcards;

public class FlashcardDeckLifecycleTests
{
    private static readonly DateTime Now = new(2026, 10, 8, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid CourseId = Guid.NewGuid();

    private readonly FixedTimeProvider _timeProvider = new(Now);
    private readonly FlashcardDeckLifecycle _sut;

    public FlashcardDeckLifecycleTests()
    {
        _sut = new FlashcardDeckLifecycle(_timeProvider);
    }

    [Fact]
    public void Create_ReturnsActiveDeckWithNormalizedName()
    {
        var deck = _sut.Create("  Algorithmen\n::  Graphen ", CourseId, null, 20, 200);

        Assert.Equal("Algorithmen :: Graphen", deck.Name);
        Assert.Equal(CourseId, deck.CourseId);
        Assert.Equal(20, deck.NewCardsPerDay);
        Assert.Equal(200, deck.ReviewsPerDay);
        Assert.False(deck.IsArchived);
        Assert.Equal(Now, deck.CreatedAt);
    }

    [Theory]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithoutName_Throws(string? name)
    {
        Assert.Throws<FlashcardValidationException>(() => _sut.Create(name!, null, null, 20, 200));
    }

    [Fact]
    public void Create_WithTooLongName_Throws()
    {
        Assert.Throws<FlashcardValidationException>(() =>
            _sut.Create(new string('d', FlashcardDeck.NameMaxLength + 1), null, null, 20, 200)
        );
    }

    [Theory]
    [InlineData(-1, 200)]
    [InlineData(20, FlashcardDeck.MaxCardsPerDay + 1)]
    public void Create_WithLimitOutOfRange_Throws(int newCardsPerDay, int reviewsPerDay)
    {
        Assert.Throws<FlashcardValidationException>(() =>
            _sut.Create("Deck", null, null, newCardsPerDay, reviewsPerDay)
        );
    }

    [Fact]
    public void Update_ChangesSettingsAndTimestamp()
    {
        var deck = _sut.Create("Deck", null, null, 20, 200);
        _timeProvider.UtcNow = Now.AddHours(1);

        var updated = _sut.Update(deck, "Netze", CourseId, null, 0, 9_999);

        Assert.Equal("Netze", updated.Name);
        Assert.Equal(CourseId, updated.CourseId);
        Assert.Equal(0, updated.NewCardsPerDay);
        Assert.Equal(9_999, updated.ReviewsPerDay);
        Assert.Equal(Now.AddHours(1), updated.UpdatedAt);
    }

    [Fact]
    public void Update_ArchivedDeck_Throws()
    {
        var deck = _sut.Archive(_sut.Create("Deck", null, null, 20, 200));

        Assert.Throws<FlashcardDeckArchivedException>(() =>
            _sut.Update(deck, "Deck", null, null, 20, 200)
        );
    }

    [Fact]
    public void ArchiveThenRestore_RoundTrips()
    {
        var deck = _sut.Create("Deck", null, null, 20, 200);

        var archived = _sut.Archive(deck);
        var restored = _sut.Restore(archived);

        Assert.True(archived.IsArchived);
        Assert.False(restored.IsArchived);
    }

    [Fact]
    public void Create_WithSemester_LinksTheSemester()
    {
        var semesterId = Guid.NewGuid();

        var deck = _sut.Create("Workshop", null, semesterId, 20, 200);

        Assert.Equal(semesterId, deck.SemesterId);
        Assert.Null(deck.CourseId);
    }

    [Fact]
    public void Create_WithCourseAndSemester_Throws()
    {
        Assert.Throws<FlashcardValidationException>(() =>
            _sut.Create("Workshop", CourseId, Guid.NewGuid(), 20, 200)
        );
    }

    [Fact]
    public void Update_WithCourseAndSemester_Throws()
    {
        var deck = _sut.Create("Workshop", CourseId, null, 20, 200);

        Assert.Throws<FlashcardValidationException>(() =>
            _sut.Update(deck, "Workshop", CourseId, Guid.NewGuid(), 20, 200)
        );
    }
}
