namespace StudyHub.Shared.Domain.Notes;

public sealed record NoteDocument
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
