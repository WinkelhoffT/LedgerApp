namespace StudyHub.Shared.Ai;

/// <summary>An AI model the user may choose for an AI feature.</summary>
public sealed record AiModelDto(string Id, string DisplayName, bool IsDefault);
