namespace StudyHub.Shared.Flashcards;

public sealed class AiNotConfiguredException()
    : Exception("AI features are not configured: set Anthropic:ApiKey for StudyHub.Api (user secret locally, Anthropic__ApiKey in Docker).");
