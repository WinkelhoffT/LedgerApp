namespace StudyHub.Shared.Ai;

/// <summary>An AI provider call failed, was refused, or returned output that cannot be used.</summary>
public sealed class AiGenerationFailedException(
    AiGenerationFailureReason reason,
    string message,
    Exception? innerException = null
) : Exception(message, innerException)
{
    public AiGenerationFailureReason Reason { get; } = reason;
}
