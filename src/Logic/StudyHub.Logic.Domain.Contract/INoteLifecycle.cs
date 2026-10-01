using StudyHub.Shared.Notes;

namespace StudyHub.Logic.Domain.Contract;

/// <summary>
/// Domain rules for creating and changing a <see cref="Note"/>. Notes are immutable records,
/// so every operation returns a new instance instead of mutating the given one.
/// </summary>
public interface INoteLifecycle
{
    Note Create(string title, string content, string? tags, Guid? courseId, Guid? semesterId);

    /// <exception cref="NoteArchivedException">The note is archived.</exception>
    Note Update(Note note, string title, string content, string? tags, Guid? courseId, Guid? semesterId);

    Note Archive(Note note);

    Note Restore(Note note);
}
