using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using StudyHub.Logic.Business.Courses;
using StudyHub.Logic.Domain.Courses;

namespace StudyHub.Api.Courses;

public sealed class CourseExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problemDetails = exception switch
        {
            CourseNotFoundException ex => Build(
                StatusCodes.Status404NotFound, ex.Message, CourseErrorCodes.CourseNotFound, "courseId", ex.CourseId),
            DuplicateCourseNameException ex => Build(
                StatusCodes.Status409Conflict, ex.Message, CourseErrorCodes.DuplicateCourseName, "courseName", ex.Name),
            CourseArchivedException ex => Build(
                StatusCodes.Status409Conflict, ex.Message, CourseErrorCodes.CourseArchived, "courseId", ex.CourseId),
            CourseValidationException ex => Build(
                StatusCodes.Status400BadRequest, ex.Message, CourseErrorCodes.CourseValidationFailed),
            _ => null,
        };

        if (problemDetails is null)
        {
            return false;
        }

        httpContext.Response.StatusCode = problemDetails.Status!.Value;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }

    private static ProblemDetails Build(int status, string detail, string errorCode, string? extraKey = null, object? extraValue = null)
    {
        var problemDetails = new ProblemDetails
        {
            Status = status,
            Detail = detail,
        };

        problemDetails.Extensions["errorCode"] = errorCode;

        if (extraKey is not null)
        {
            problemDetails.Extensions[extraKey] = extraValue;
        }

        return problemDetails;
    }
}
