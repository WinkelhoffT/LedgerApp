namespace StudyHub.Logic.Business.Semesters;

public sealed record UpdateSemesterRequest(Guid Id, string Name, DateOnly StartDate, DateOnly EndDate);
