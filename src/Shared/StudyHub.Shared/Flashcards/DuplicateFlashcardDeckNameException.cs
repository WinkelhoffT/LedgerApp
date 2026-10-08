namespace StudyHub.Shared.Flashcards;

public sealed class DuplicateFlashcardDeckNameException(string name) : Exception($"A deck named '{name}' already exists.")
{
    public string Name { get; } = name;
}
