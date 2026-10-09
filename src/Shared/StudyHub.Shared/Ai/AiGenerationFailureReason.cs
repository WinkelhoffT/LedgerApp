namespace StudyHub.Shared.Ai;

public enum AiGenerationFailureReason
{
    Unknown,
    Refused,
    Truncated,
    InvalidResponse,
    RateLimited,
    ServiceUnavailable,
    Unauthorized,
}
