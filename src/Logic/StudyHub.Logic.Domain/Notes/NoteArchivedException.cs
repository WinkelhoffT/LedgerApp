namespace StudyHub.Logic.Domain.Notes;

public sealed class NoteArchivedException(Guid noteId)
    : Exception($"Note '{noteId}' is archived and cannot be edited.")
{
    public Guid NoteId { get; } = noteId;
}
