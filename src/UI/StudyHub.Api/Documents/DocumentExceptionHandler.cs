using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using StudyHub.Logic.Business.Documents;
using StudyHub.Logic.Domain.Documents;

namespace StudyHub.Api.Documents;

// Course/Semester validation failures raised while assigning a document to a parent
// (CourseNotFoundException, SemesterArchivedException, etc.) are handled by the already
// registered CourseExceptionHandler/SemesterExceptionHandler, so this handler only needs to
// cover exceptions specific to the Documents feature.
public sealed class DocumentExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problemDetails = exception switch
        {
            DocumentNotFoundException ex => Build(
                StatusCodes.Status404NotFound, ex.Message, DocumentErrorCodes.DocumentNotFound, "documentId", ex.DocumentId),
            UnsupportedDocumentTypeException ex => Build(
                StatusCodes.Status400BadRequest, ex.Message, DocumentErrorCodes.UnsupportedDocumentType, "contentType", ex.ContentType),
            DocumentTooLargeException ex => Build(
                StatusCodes.Status400BadRequest, ex.Message, DocumentErrorCodes.DocumentTooLarge, "sizeBytes", ex.SizeBytes),
            DocumentValidationException ex => Build(
                StatusCodes.Status400BadRequest, ex.Message, DocumentErrorCodes.DocumentValidationFailed),
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
