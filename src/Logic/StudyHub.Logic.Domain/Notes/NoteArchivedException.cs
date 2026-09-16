namespace StudyHub.Logic.Domain.Notes;

public sealed class NoteArchivedException(Guid noteId) : Exception($"Note '{noteId}' is archived.")
{
    public Guid NoteId { get; } = noteId;
}
