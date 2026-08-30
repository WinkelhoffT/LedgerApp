namespace StudyHub.Logic.Domain.Documents;

public sealed class DocumentValidationException(string message) : Exception(message);
