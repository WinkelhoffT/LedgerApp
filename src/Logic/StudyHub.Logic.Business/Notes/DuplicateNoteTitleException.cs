namespace StudyHub.Logic.Business.Notes;

public sealed class DuplicateNoteTitleException(string title) : Exception($"A note titled '{title}' already exists.")
{
    public string Title { get; } = title;
}
