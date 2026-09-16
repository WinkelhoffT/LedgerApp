namespace StudyHub.Logic.Domain.Notes;

public sealed class NoteNotFoundException(Guid noteId) : Exception($"Note '{noteId}' was not found.")
{
    public Guid NoteId { get; } = noteId;
}
