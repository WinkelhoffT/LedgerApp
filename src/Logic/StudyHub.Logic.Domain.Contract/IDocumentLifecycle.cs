using StudyHub.Shared.Documents;

namespace StudyHub.Logic.Domain.Contract;

/// <summary>
/// Domain rules for creating and changing a <see cref="Document"/>. Documents are immutable
/// records, so every operation returns a new instance instead of mutating the given one.
/// </summary>
public interface IDocumentLifecycle
{
    Document Create(
        string fileName,
        string contentType,
        byte[] content,
        Guid? courseId,
        Guid? semesterId
    );

    Document Archive(Document document);

    Document Restore(Document document);
}
