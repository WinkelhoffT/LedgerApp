using Microsoft.EntityFrameworkCore;
using StudyHub.Data;
using StudyHub.Shared.Flashcards;

namespace StudyHub.Tests.Data.Flashcards;

public class FlashcardDeckRepositoryTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static FlashcardDeck Deck(string name) =>
        new(Guid.NewGuid(), name, null, 20, 200, false, DateTime.UtcNow, DateTime.UtcNow);

    [Fact]
    public async Task ExistsByNameAsync_MatchesNameIgnoringCaseAndSurroundingSpaces()
    {
        await using var dbContext = CreateDbContext();
        var repository = new FlashcardDeckRepository(dbContext);
        await repository.AddAsync(Deck("Algorithmen"));
        await repository.SaveChangesAsync();

        Assert.True(await repository.ExistsByNameAsync("  ALGORITHMEN ", null));
        Assert.False(await repository.ExistsByNameAsync("Datenbanken", null));
    }

    [Fact]
    public async Task ExistsByNameAsync_IgnoresExcludedDeck()
    {
        await using var dbContext = CreateDbContext();
        var repository = new FlashcardDeckRepository(dbContext);
        var deck = Deck("Algorithmen");
        await repository.AddAsync(deck);
        await repository.SaveChangesAsync();

        Assert.False(await repository.ExistsByNameAsync("Algorithmen", deck.Id));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsDecksOrderedByName()
    {
        await using var dbContext = CreateDbContext();
        var repository = new FlashcardDeckRepository(dbContext);
        await repository.AddAsync(Deck("Netze"));
        await repository.AddAsync(Deck("Algorithmen"));
        await repository.SaveChangesAsync();

        var decks = await repository.GetAllAsync();

        Assert.Equal(["Algorithmen", "Netze"], decks.Select(d => d.Name));
    }

    [Fact]
    public async Task Update_PersistsChanges()
    {
        await using var dbContext = CreateDbContext();
        var repository = new FlashcardDeckRepository(dbContext);
        var deck = Deck("Netze");
        await repository.AddAsync(deck);
        await repository.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        repository.Update(deck with { NewCardsPerDay = 5 });
        await repository.SaveChangesAsync();

        Assert.Equal(5, (await repository.GetByIdAsync(deck.Id))!.NewCardsPerDay);
    }
}
