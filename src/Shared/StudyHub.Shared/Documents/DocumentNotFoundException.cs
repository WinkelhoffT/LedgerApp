namespace StudyHub.Shared.Documents;

public sealed class DocumentNotFoundException(Guid documentId) : Exception($"Document '{documentId}' was not found.")
{
    public Guid DocumentId { get; } = documentId;
}
