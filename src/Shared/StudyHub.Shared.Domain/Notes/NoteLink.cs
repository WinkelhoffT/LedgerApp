namespace StudyHub.Shared.Domain.Notes;

public sealed class NoteLink
{
    private NoteLink()
    {
    }

    public Guid SourceNoteId { get; private set; }

    public Guid TargetNoteId { get; private set; }

    public static NoteLink Create(Guid sourceNoteId, Guid targetNoteId) => new()
    {
        SourceNoteId = sourceNoteId,
        TargetNoteId = targetNoteId,
    };
}
