namespace StudyHub.Shared.Semesters;

public sealed record UpdateSemesterRequest(Guid Id, string Name, DateOnly StartDate, DateOnly EndDate);
