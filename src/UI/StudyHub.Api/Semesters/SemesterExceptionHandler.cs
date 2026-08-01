using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using StudyHub.Logic.Business.Semesters;
using StudyHub.Logic.Domain.Semesters;

namespace StudyHub.Api.Semesters;

public sealed class SemesterExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problemDetails = exception switch
        {
            SemesterNotFoundException ex => Build(
                StatusCodes.Status404NotFound, ex.Message, SemesterErrorCodes.SemesterNotFound, "semesterId", ex.SemesterId),
            DuplicateSemesterNameException ex => Build(
                StatusCodes.Status409Conflict, ex.Message, SemesterErrorCodes.DuplicateSemesterName, "semesterName", ex.Name),
            SemesterArchivedException ex => Build(
                StatusCodes.Status409Conflict, ex.Message, SemesterErrorCodes.SemesterArchived, "semesterId", ex.SemesterId),
            SemesterValidationException ex => Build(
                StatusCodes.Status400BadRequest, ex.Message, SemesterErrorCodes.SemesterValidationFailed),
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
