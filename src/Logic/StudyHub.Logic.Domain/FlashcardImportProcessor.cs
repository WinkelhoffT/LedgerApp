using StudyHub.Logic.Domain.Contract;
using StudyHub.Shared.Flashcards;

namespace StudyHub.Logic.Domain;

public sealed class FlashcardImportProcessor(
    IFlashcardValidator flashcardValidator,
    IFlashcardDeckLifecycle deckLifecycle,
    IFlashcardLifecycle flashcardLifecycle) : IFlashcardImportProcessor
{
    private const string FallbackDeckName = "Imported cards";

    public IReadOnlyList<Guid> GetTargetDeckIds(AnkiCsvParseResult file, FlashcardImportTarget target, IReadOnlyList<FlashcardDeck> decks)
    {
        var decksByName = ToNameLookup(decks);
        var deckIds = new HashSet<Guid>();

        foreach (var row in file.Rows)
        {
            if (!TryGetDeckName(row, target, out var name))
            {
                continue;
            }

            if ((name is null ? target.Deck : decksByName.GetValueOrDefault(name)) is { } deck)
            {
                deckIds.Add(deck.Id);
            }
        }

        return deckIds.ToList();
    }

    public FlashcardImportOutcome Process(
        AnkiCsvParseResult file,
        FlashcardImportTarget target,
        IReadOnlyList<FlashcardDeck> decks,
        IReadOnlyList<Flashcard> existingCards)
    {
        if (file.Rows.Count == 0)
        {
            throw new FlashcardImportException("The file contains no cards.");
        }

        var duplicateMode = file.DuplicateMode ?? target.DuplicateMode;
        var decksByName = ToNameLookup(decks);
        var createdDecks = new List<FlashcardDeck>();

        // The first card per front decides how a later row with the same front is treated, as in Anki.
        var existingByDeck = existingCards
            .GroupBy(card => card.DeckId)
            .ToDictionary(group => group.Key, group => group.GroupBy(card => card.Front).ToDictionary(g => g.Key, g => g.First()));
        var pendingByDeck = new Dictionary<Guid, List<FlashcardDto>>();
        var pendingIndexByDeck = new Dictionary<Guid, Dictionary<string, int>>();
        var updated = new Dictionary<Guid, Flashcard>();
        var touchedDeckIds = new List<Guid>();
        var failures = new List<FlashcardImportFailureDto>();
        var skipped = 0;

        foreach (var row in file.Rows)
        {
            if (row.NoteType?.Contains("cloze", StringComparison.OrdinalIgnoreCase) == true)
            {
                failures.Add(new FlashcardImportFailureDto(row.LineNumber, "Cloze notes are not supported yet."));
                continue;
            }

            if (!flashcardValidator.TryNormalize(new FlashcardDto(row.Front, row.Back, row.Tags), out var content, out var error))
            {
                failures.Add(new FlashcardImportFailureDto(row.LineNumber, error));
                continue;
            }

            if (ResolveDeck(row, target, decksByName, createdDecks) is not { } deck)
            {
                failures.Add(new FlashcardImportFailureDto(row.LineNumber, DeckFailure(row, target, decksByName)));
                continue;
            }

            if (!touchedDeckIds.Contains(deck.Id))
            {
                touchedDeckIds.Add(deck.Id);
            }

            var existingCard = existingByDeck.GetValueOrDefault(deck.Id)?.GetValueOrDefault(content.Front);
            var pending = pendingByDeck.TryGetValue(deck.Id, out var list) ? list : pendingByDeck[deck.Id] = [];
            var pendingIndexes = pendingIndexByDeck.TryGetValue(deck.Id, out var indexes) ? indexes : pendingIndexByDeck[deck.Id] = [];
            var hasPending = pendingIndexes.TryGetValue(content.Front, out var pendingIndex);

            if (duplicateMode != ImportDuplicateMode.KeepBoth && (existingCard is not null || hasPending))
            {
                if (duplicateMode == ImportDuplicateMode.KeepCurrent)
                {
                    skipped++;
                }
                else if (existingCard is not null)
                {
                    var current = updated.GetValueOrDefault(existingCard.Id, existingCard);
                    updated[existingCard.Id] = flashcardLifecycle.UpdateContent(current, content);
                }
                else
                {
                    pending[pendingIndex] = content;
                }

                continue;
            }

            pendingIndexes.TryAdd(content.Front, pending.Count);
            pending.Add(content);
        }

        var added = pendingByDeck
            .Where(entry => entry.Value.Count > 0)
            .SelectMany(entry => flashcardLifecycle.Create(entry.Key, entry.Value, sourceNoteId: null))
            .ToList();

        if (added.Count == 0 && updated.Count == 0 && skipped == 0)
        {
            throw new FlashcardImportException(
                $"No card in the file could be imported. Line {failures[0].LineNumber}: {failures[0].Reason}");
        }

        var createdDeckIds = createdDecks.Select(d => d.Id).ToHashSet();
        var allDecks = decks.Concat(createdDecks).ToDictionary(d => d.Id);
        var affectedDecks = touchedDeckIds
            .Select(id => new ImportedFlashcardDeckDto(id, allDecks[id].Name, createdDeckIds.Contains(id)))
            .ToList();

        return new FlashcardImportOutcome(createdDecks, added, updated.Values.ToList(), skipped, failures, affectedDecks);
    }

    private static Dictionary<string, FlashcardDeck> ToNameLookup(IReadOnlyList<FlashcardDeck> decks) =>
        decks
            .GroupBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

    // Returns null when the row's deck cannot take cards: an invalid name or an archived deck.
    private FlashcardDeck? ResolveDeck(
        AnkiCsvRow row,
        FlashcardImportTarget target,
        Dictionary<string, FlashcardDeck> decksByName,
        List<FlashcardDeck> createdDecks)
    {
        if (!TryGetDeckName(row, target, out var name))
        {
            return null;
        }

        if (name is null)
        {
            return target.Deck;
        }

        if (decksByName.TryGetValue(name, out var deck))
        {
            return deck.IsArchived ? null : deck;
        }

        deck = deckLifecycle.Create(name, courseId: null, FlashcardDeck.DefaultNewCardsPerDay, FlashcardDeck.DefaultReviewsPerDay);
        decksByName[deck.Name] = deck;
        createdDecks.Add(deck);
        return deck;
    }

    private string DeckFailure(AnkiCsvRow row, FlashcardImportTarget target, Dictionary<string, FlashcardDeck> decksByName)
    {
        if (TryGetDeckName(row, target, out var name) && name is not null && decksByName.TryGetValue(name, out var deck))
        {
            return $"The deck '{deck.Name}' is archived.";
        }

        return $"The deck name '{row.DeckName}' is not valid.";
    }

    // The deck the row goes to, by normalized name; a null name means the existing target deck.
    // False when the name from the file is not a valid deck name.
    private bool TryGetDeckName(AnkiCsvRow row, FlashcardImportTarget target, out string? name)
    {
        name = null;

        if (row.DeckName is not null)
        {
            try
            {
                name = deckLifecycle.NormalizeName(row.DeckName);
                return true;
            }
            catch (FlashcardValidationException)
            {
                return false;
            }
        }

        if (target.Deck is null)
        {
            name = GetDeckNameFromFile(target.FileName);
        }

        return true;
    }

    private string GetDeckNameFromFile(string fileName)
    {
        var baseName = Path.GetFileNameWithoutExtension(fileName ?? string.Empty);
        if (baseName.Length > FlashcardDeck.NameMaxLength)
        {
            baseName = baseName[..FlashcardDeck.NameMaxLength];
        }

        try
        {
            return deckLifecycle.NormalizeName(baseName);
        }
        catch (FlashcardValidationException)
        {
            return FallbackDeckName;
        }
    }
}
