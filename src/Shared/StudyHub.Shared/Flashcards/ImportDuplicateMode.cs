namespace StudyHub.Shared.Flashcards;

/// <summary>What an import does with a row whose front already exists in the target deck (Anki's "If matches").</summary>
public enum ImportDuplicateMode
{
    UpdateCurrent,
    KeepCurrent,
    KeepBoth,
}
