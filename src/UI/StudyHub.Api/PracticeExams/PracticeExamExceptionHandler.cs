using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using StudyHub.Shared.PracticeExams;

namespace StudyHub.Api.PracticeExams;

// Course, note and deck lookups are handled by their own exception handlers, and AI failures by
// AiExceptionHandler, so this handler only covers practice-exam-specific exceptions.
public sealed class PracticeExamExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        var problemDetails = exception switch
        {
            PracticeExamValidationException ex => Build(
                StatusCodes.Status400BadRequest,
                ex.Message,
                PracticeExamErrorCodes.PracticeExamValidationFailed
            ),
            PracticeExamNotFoundException ex => Build(
                StatusCodes.Status404NotFound,
                ex.Message,
                PracticeExamErrorCodes.PracticeExamNotFound,
                "examId",
                ex.ExamId
            ),
            PracticeExamArchivedException ex => Build(
                StatusCodes.Status409Conflict,
                ex.Message,
                PracticeExamErrorCodes.PracticeExamArchived,
                "examId",
                ex.ExamId
            ),
            PracticeExamTaskNotFoundException ex => Build(
                StatusCodes.Status404NotFound,
                ex.Message,
                PracticeExamErrorCodes.PracticeExamTaskNotFound,
                "taskId",
                ex.TaskId
            ),
            PracticeExamAttemptNotFoundException ex => Build(
                StatusCodes.Status404NotFound,
                ex.Message,
                PracticeExamErrorCodes.PracticeExamAttemptNotFound,
                "attemptId",
                ex.AttemptId
            ),
            PracticeExamAttemptSubmittedException ex => Build(
                StatusCodes.Status409Conflict,
                ex.Message,
                PracticeExamErrorCodes.PracticeExamAttemptSubmitted,
                "attemptId",
                ex.AttemptId
            ),
            PracticeExamAttemptNotSubmittedException ex => Build(
                StatusCodes.Status409Conflict,
                ex.Message,
                PracticeExamErrorCodes.PracticeExamAttemptNotSubmitted,
                "attemptId",
                ex.AttemptId
            ),
            PracticeExamTimeOverException ex => Build(
                StatusCodes.Status409Conflict,
                ex.Message,
                PracticeExamErrorCodes.PracticeExamTimeOver,
                "attemptId",
                ex.AttemptId
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
