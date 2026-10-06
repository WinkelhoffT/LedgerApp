using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using StudyHub.Shared.Flashcards;

namespace StudyHub.Api.Flashcards;

// Note lookups during generation (NoteNotFoundException, NoteArchivedException) are handled by the
// already registered NoteExceptionHandler, so this handler only covers flashcard-specific exceptions.
public sealed class FlashcardExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problemDetails = exception switch
        {
            FlashcardValidationException ex => Build(
                StatusCodes.Status400BadRequest, ex.Message, FlashcardErrorCodes.FlashcardValidationFailed),
            FlashcardGenerationFailedException ex => Build(
                StatusCodes.Status502BadGateway, ex.Message, FlashcardErrorCodes.FlashcardGenerationFailed, "reason", ex.Reason.ToString()),
            AiNotConfiguredException ex => Build(
                StatusCodes.Status503ServiceUnavailable, ex.Message, FlashcardErrorCodes.AiNotConfigured),
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
