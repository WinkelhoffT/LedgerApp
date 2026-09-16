namespace StudyHub.Shared.Notes;

public sealed class NoteArchivedException(Guid noteId) : Exception($"Note '{noteId}' is archived.")
{
    public Guid NoteId { get; } = noteId;
}
