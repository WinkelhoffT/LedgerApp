namespace StudyHub.Shared.Flashcards;

public sealed record FlashcardDeckDto(
    Guid Id,
    string Name,
    Guid? CourseId,
    Guid? SemesterId,
    int NewCardsPerDay,
    int ReviewsPerDay,
    bool IsArchived,
    int CardCount,
    FlashcardStudyCountsDto DueCounts,
    DateTime CreatedAt,
    DateTime UpdatedAt);
