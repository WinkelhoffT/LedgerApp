using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using StudyHub.Shared.Flashcards;

namespace StudyHub.Api.Flashcards;

// Note and course lookups (NoteNotFoundException, CourseArchivedException, ...) are handled by the
// already registered NoteExceptionHandler/CourseExceptionHandler, and AI failures by
// AiExceptionHandler, so this handler only covers flashcard-specific exceptions.
public sealed class FlashcardExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        var problemDetails = exception switch
        {
            FlashcardValidationException ex => Build(
                StatusCodes.Status400BadRequest,
                ex.Message,
                FlashcardErrorCodes.FlashcardValidationFailed
            ),
            FlashcardDeckNotFoundException ex => Build(
                StatusCodes.Status404NotFound,
                ex.Message,
                FlashcardErrorCodes.FlashcardDeckNotFound,
                "deckId",
                ex.DeckId
            ),
            FlashcardDeckArchivedException ex => Build(
                StatusCodes.Status409Conflict,
                ex.Message,
                FlashcardErrorCodes.FlashcardDeckArchived,
                "deckId",
                ex.DeckId
            ),
            DuplicateFlashcardDeckNameException ex => Build(
                StatusCodes.Status409Conflict,
                ex.Message,
                FlashcardErrorCodes.DuplicateFlashcardDeckName,
                "deckName",
                ex.Name
            ),
            FlashcardNotFoundException ex => Build(
                StatusCodes.Status404NotFound,
                ex.Message,
                FlashcardErrorCodes.FlashcardNotFound,
                "flashcardId",
                ex.FlashcardId
            ),
            FlashcardNotDueException ex => Build(
                StatusCodes.Status409Conflict,
                ex.Message,
                FlashcardErrorCodes.FlashcardNotDue,
                "flashcardId",
                ex.FlashcardId
            ),
            FlashcardImportException ex => Build(
                StatusCodes.Status400BadRequest,
                ex.Message,
                FlashcardErrorCodes.FlashcardImportFailed
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
