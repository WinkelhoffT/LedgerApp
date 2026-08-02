namespace StudyHub.Logic.Business.Semesters;

public sealed record CreateSemesterRequest(string Name, DateOnly StartDate, DateOnly EndDate);
