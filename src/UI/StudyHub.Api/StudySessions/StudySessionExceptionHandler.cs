using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using StudyHub.Shared.StudySessions;

namespace StudyHub.Api.StudySessions;

// Course and semester lookups (CourseNotFoundException, SemesterArchivedException, ...) are handled
// by the already registered CourseExceptionHandler/SemesterExceptionHandler, so this handler only
// covers session-specific exceptions.
public sealed class StudySessionExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problemDetails = exception switch
        {
            StudySessionValidationException ex => Build(
                StatusCodes.Status400BadRequest, ex.Message, StudySessionErrorCodes.StudySessionValidationFailed),
            StudySessionNotFoundException ex => Build(
                StatusCodes.Status404NotFound, ex.Message, StudySessionErrorCodes.StudySessionNotFound, "studySessionId", ex.StudySessionId),
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
