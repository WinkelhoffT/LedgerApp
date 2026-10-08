using StudyHub.Data.Contract;
using StudyHub.Logic.Business.Contract;
using StudyHub.Logic.Domain.Contract;
using StudyHub.Shared.Courses;
using StudyHub.Shared.Flashcards;
using StudyHub.Shared.Semesters;

namespace StudyHub.Logic.Business;

public sealed class FlashcardDeckOrchestrator(
    IFlashcardDeckRepository deckRepository,
    IFlashcardRepository flashcardRepository,
    ICourseRepository courseRepository,
    ISemesterRepository semesterRepository,
    IFlashcardDeckLifecycle deckLifecycle,
    IStudyQueueProvider studyQueueProvider,
    IStudyDayProvider studyDayProvider
) : IFlashcardDeckOrchestrator
{
    public async Task<IReadOnlyList<FlashcardDeckDto>> GetAllAsync(
        bool includeArchived,
        CancellationToken cancellationToken = default
    )
    {
        var decks = (await deckRepository.GetAllAsync(cancellationToken))
            .Where(d => includeArchived || !d.IsArchived)
            .ToList();
        if (decks.Count == 0)
        {
            return [];
        }

        var today = studyDayProvider.GetCurrent();
        var cardCounts = (await flashcardRepository.GetCardCountsAsync(today.NextStart, cancellationToken: cancellationToken))
            .ToDictionary(c => c.DeckId);
        var reviewCounts = (await flashcardRepository.GetReviewCountsAsync(today.Start, cancellationToken: cancellationToken))
            .ToDictionary(c => c.DeckId);

        return decks
            .Select(deck => ToDto(deck, cardCounts.GetValueOrDefault(deck.Id), reviewCounts.GetValueOrDefault(deck.Id)))
            .ToList();
    }

    public async Task<FlashcardDeckDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var deck = await GetExistingDeckAsync(id, cancellationToken);
        return await ToDtoAsync(deck, cancellationToken);
    }

    public async Task<FlashcardDeckDto> CreateAsync(
        CreateFlashcardDeckRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var deck = deckLifecycle.Create(
            request.Name,
            request.CourseId,
            request.SemesterId,
            request.NewCardsPerDay,
            request.ReviewsPerDay);

        await EnsureNameIsUniqueAsync(deck.Name, excludingId: null, cancellationToken);
        await EnsureCourseIsAssignableAsync(deck.CourseId, currentCourseId: null, cancellationToken);
        await EnsureSemesterIsAssignableAsync(deck.SemesterId, currentSemesterId: null, cancellationToken);

        await deckRepository.AddAsync(deck, cancellationToken);
        await deckRepository.SaveChangesAsync(cancellationToken);

        return FlashcardMapper.ToDeckDto(deck, 0, FlashcardStudyCountsDto.Empty);
    }

    public async Task<FlashcardDeckDto> UpdateAsync(
        UpdateFlashcardDeckRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var deck = await GetExistingDeckAsync(request.Id, cancellationToken);
        var updated = deckLifecycle.Update(
            deck,
            request.Name,
            request.CourseId,
            request.SemesterId,
            request.NewCardsPerDay,
            request.ReviewsPerDay);

        await EnsureNameIsUniqueAsync(updated.Name, deck.Id, cancellationToken);
        await EnsureCourseIsAssignableAsync(updated.CourseId, deck.CourseId, cancellationToken);
        await EnsureSemesterIsAssignableAsync(updated.SemesterId, deck.SemesterId, cancellationToken);

        deckRepository.Update(updated);
        await deckRepository.SaveChangesAsync(cancellationToken);

        return await ToDtoAsync(updated, cancellationToken);
    }

    public async Task<FlashcardDeckDto> ArchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var deck = deckLifecycle.Archive(await GetExistingDeckAsync(id, cancellationToken));

        deckRepository.Update(deck);
        await deckRepository.SaveChangesAsync(cancellationToken);

        return await ToDtoAsync(deck, cancellationToken);
    }

    public async Task<FlashcardDeckDto> RestoreAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var deck = deckLifecycle.Restore(await GetExistingDeckAsync(id, cancellationToken));

        deckRepository.Update(deck);
        await deckRepository.SaveChangesAsync(cancellationToken);

        return await ToDtoAsync(deck, cancellationToken);
    }

    private async Task<FlashcardDeck> GetExistingDeckAsync(Guid id, CancellationToken cancellationToken) =>
        await deckRepository.GetByIdAsync(id, cancellationToken) ?? throw new FlashcardDeckNotFoundException(id);

    private async Task EnsureNameIsUniqueAsync(string name, Guid? excludingId, CancellationToken cancellationToken)
    {
        if (await deckRepository.ExistsByNameAsync(name, excludingId, cancellationToken))
        {
            throw new DuplicateFlashcardDeckNameException(name);
        }
    }

    // An archived course cannot be newly linked; a deck already linked to it may keep the link.
    private async Task EnsureCourseIsAssignableAsync(Guid? courseId, Guid? currentCourseId, CancellationToken cancellationToken)
    {
        if (courseId is not { } id)
        {
            return;
        }

        var course = await courseRepository.GetByIdAsync(id, cancellationToken) ?? throw new CourseNotFoundException(id);
        if (course.IsArchived && id != currentCourseId)
        {
            throw new CourseArchivedException(id);
        }
    }

    // An archived semester cannot be newly linked; a deck already linked to it may keep the link.
    private async Task EnsureSemesterIsAssignableAsync(Guid? semesterId, Guid? currentSemesterId, CancellationToken cancellationToken)
    {
        if (semesterId is not { } id)
        {
            return;
        }

        var semester = await semesterRepository.GetByIdAsync(id, cancellationToken) ?? throw new SemesterNotFoundException(id);
        if (semester.IsArchived && id != currentSemesterId)
        {
            throw new SemesterArchivedException(id);
        }
    }

    private async Task<FlashcardDeckDto> ToDtoAsync(FlashcardDeck deck, CancellationToken cancellationToken)
    {
        var today = studyDayProvider.GetCurrent();
        var cardCounts = await flashcardRepository.GetCardCountsAsync(today.NextStart, deck.Id, cancellationToken);
        var reviewCounts = await flashcardRepository.GetReviewCountsAsync(today.Start, deck.Id, cancellationToken);

        return ToDto(deck, cardCounts.FirstOrDefault(), reviewCounts.FirstOrDefault());
    }

    private FlashcardDeckDto ToDto(
        FlashcardDeck deck,
        FlashcardDeckCardCounts? cardCounts,
        FlashcardDeckReviewCounts? reviewCounts
    ) =>
        FlashcardMapper.ToDeckDto(
            deck,
            cardCounts?.Total ?? 0,
            studyQueueProvider.GetCounts(deck, cardCounts, reviewCounts));
}
