namespace StudyHub.Shared.Flashcards;

public sealed class AiNotConfiguredException()
    : Exception("AI features are not configured: set the ANTHROPIC_API_KEY environment variable for StudyHub.Api.");
