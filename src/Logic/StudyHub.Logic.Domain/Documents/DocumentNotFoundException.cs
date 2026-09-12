namespace StudyHub.Logic.Domain.Documents;

public sealed class DocumentNotFoundException(Guid documentId) : Exception($"Document '{documentId}' was not found.")
{
    public Guid DocumentId { get; } = documentId;
}
