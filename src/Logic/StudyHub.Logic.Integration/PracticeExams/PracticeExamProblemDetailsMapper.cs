using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using StudyHub.Shared.Ai;
using StudyHub.Shared.Courses;
using StudyHub.Shared.Flashcards;
using StudyHub.Shared.PracticeExams;

namespace StudyHub.Logic.Integration.PracticeExams;

/// <summary>Turns StudyHub.Api's problem details for the practice exam endpoints back into the shared exceptions.</summary>
internal static class PracticeExamProblemDetailsMapper
{
    public static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken
    )
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(
            cancellationToken
        );
        var errorCode = GetString(problemDetails, "errorCode");
        var detail = problemDetails?.Detail;

        throw errorCode switch
        {
            PracticeExamErrorCodes.PracticeExamValidationFailed =>
                new PracticeExamValidationException(detail ?? "Practice exam validation failed."),
            PracticeExamErrorCodes.PracticeExamNotFound => new PracticeExamNotFoundException(
                GetGuid(problemDetails, "examId")
            ),
            PracticeExamErrorCodes.PracticeExamArchived => new PracticeExamArchivedException(
                GetGuid(problemDetails, "examId")
            ),
            PracticeExamErrorCodes.PracticeExamTaskNotFound =>
                new PracticeExamTaskNotFoundException(GetGuid(problemDetails, "taskId")),
            PracticeExamErrorCodes.PracticeExamAttemptNotFound =>
                new PracticeExamAttemptNotFoundException(GetGuid(problemDetails, "attemptId")),
            PracticeExamErrorCodes.PracticeExamAttemptSubmitted =>
                new PracticeExamAttemptSubmittedException(GetGuid(problemDetails, "attemptId")),
            PracticeExamErrorCodes.PracticeExamAttemptNotSubmitted =>
                new PracticeExamAttemptNotSubmittedException(GetGuid(problemDetails, "attemptId")),
            PracticeExamErrorCodes.PracticeExamTimeOver => new PracticeExamTimeOverException(
                GetGuid(problemDetails, "attemptId")
            ),
            AiErrorCodes.AiGenerationFailed => new AiGenerationFailedException(
                Enum.TryParse<AiGenerationFailureReason>(
                    GetString(problemDetails, "reason"),
                    out var reason
                )
                    ? reason
                    : AiGenerationFailureReason.Unknown,
                detail ?? "Practice exam generation failed."
            ),
            AiErrorCodes.AiNotConfigured => new AiNotConfiguredException(),
            CourseErrorCodes.CourseNotFound => new CourseNotFoundException(
                GetGuid(problemDetails, "courseId")
            ),
            CourseErrorCodes.CourseArchived => new CourseArchivedException(
                GetGuid(problemDetails, "courseId")
            ),
            FlashcardErrorCodes.FlashcardDeckNotFound => new FlashcardDeckNotFoundException(
                GetGuid(problemDetails, "deckId")
            ),
            FlashcardErrorCodes.FlashcardDeckArchived => new FlashcardDeckArchivedException(
                GetGuid(problemDetails, "deckId")
            ),
            _ => new HttpRequestException(
                $"StudyHub.Api returned {(int)response.StatusCode} ({response.StatusCode}): {detail}",
                inner: null,
                response.StatusCode
            ),
        };
    }

    private static string? GetString(ProblemDetails? problemDetails, string key)
    {
        if (
            problemDetails is null
            || !problemDetails.Extensions.TryGetValue(key, out var value)
            || value is not JsonElement { ValueKind: JsonValueKind.String } element
        )
        {
            return null;
        }

        return element.GetString();
    }

    private static Guid GetGuid(ProblemDetails? problemDetails, string key) =>
        Guid.TryParse(GetString(problemDetails, key), out var guid) ? guid : Guid.Empty;
}
