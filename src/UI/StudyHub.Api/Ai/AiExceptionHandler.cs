using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using StudyHub.Shared.Ai;

namespace StudyHub.Api.Ai;

/// <summary>Maps the AI failures every AI feature can raise (flashcards, practice exams, …).</summary>
public sealed class AiExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        var problemDetails = exception switch
        {
            AiGenerationFailedException ex => Build(
                StatusCodes.Status502BadGateway,
                ex.Message,
                AiErrorCodes.AiGenerationFailed,
                "reason",
                ex.Reason.ToString()
            ),
            AiNotConfiguredException ex => Build(
                StatusCodes.Status503ServiceUnavailable,
                ex.Message,
                AiErrorCodes.AiNotConfigured
            ),
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

    private static ProblemDetails Build(
        int status,
        string detail,
        string errorCode,
        string? extraKey = null,
        object? extraValue = null
    )
    {
        var problemDetails = new ProblemDetails { Status = status, Detail = detail };

        problemDetails.Extensions["errorCode"] = errorCode;

        if (extraKey is not null)
        {
            problemDetails.Extensions[extraKey] = extraValue;
        }

        return problemDetails;
    }
}
