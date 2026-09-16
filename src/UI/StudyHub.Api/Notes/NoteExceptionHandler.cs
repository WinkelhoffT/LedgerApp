using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using StudyHub.Logic.Business.Notes;
using StudyHub.Logic.Domain.Notes;

namespace StudyHub.Api.Notes;

// Course/Semester/Document validation failures raised while assigning a note to a parent or
// attaching a document (CourseNotFoundException, DocumentNotFoundException, etc.) are handled by
// the already registered CourseExceptionHandler/SemesterExceptionHandler/DocumentExceptionHandler,
// so this handler only needs to cover exceptions specific to the Notes feature.
public sealed class NoteExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problemDetails = exception switch
        {
            NoteNotFoundException ex => Build(
                StatusCodes.Status404NotFound, ex.Message, NoteErrorCodes.NoteNotFound, "noteId", ex.NoteId),
            DuplicateNoteTitleException ex => Build(
                StatusCodes.Status409Conflict, ex.Message, NoteErrorCodes.DuplicateNoteTitle, "noteTitle", ex.Title),
            NoteArchivedException ex => Build(
                StatusCodes.Status409Conflict, ex.Message, NoteErrorCodes.NoteArchived, "noteId", ex.NoteId),
            NoteValidationException ex => Build(
                StatusCodes.Status400BadRequest, ex.Message, NoteErrorCodes.NoteValidationFailed),
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
