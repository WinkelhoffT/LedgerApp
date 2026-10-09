namespace StudyHub.Logic.Integration.Ai;

/// <param name="StopReason">The response's wire-level stop reason (<c>end_turn</c>, <c>max_tokens</c>, <c>refusal</c>, …).</param>
/// <param name="Text">The concatenated text content of the response.</param>
internal sealed record ClaudeStructuredOutput(string? StopReason, string Text);
