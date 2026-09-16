namespace StudyHub.Logic.Domain.Notes;

public sealed class NoteDocument
{
    private NoteDocument()
    {
    }

    public Guid NoteId { get; private set; }

    public Guid DocumentId { get; private set; }

    public static NoteDocument Create(Guid noteId, Guid documentId) => new()
    {
        NoteId = noteId,
        DocumentId = documentId,
    };
}
