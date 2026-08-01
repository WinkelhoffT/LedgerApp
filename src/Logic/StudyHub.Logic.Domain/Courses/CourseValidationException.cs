namespace StudyHub.Logic.Domain.Courses;

public sealed class CourseValidationException(string message) : Exception(message);
