namespace StudyHub.Shared.StudySessions;

/// <summary>Marks a session as done, or changes the actual duration of a session that is already done.</summary>
public sealed record CompleteStudySessionRequest(int ActualDurationMinutes);
