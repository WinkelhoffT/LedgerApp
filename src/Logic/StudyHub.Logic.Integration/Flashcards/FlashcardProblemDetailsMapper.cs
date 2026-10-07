using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using StudyHub.Shared.Courses;
using StudyHub.Shared.Flashcards;
using StudyHub.Shared.Notes;
using StudyHub.Shared.Semesters;

namespace StudyHub.Logic.Integration.Flashcards;

/// <summary>Turns StudyHub.Api's problem details for the flashcard endpoints back into the shared exceptions.</summary>
internal static class FlashcardProblemDetailsMapper
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
            FlashcardErrorCodes.FlashcardValidationFailed => new FlashcardValidationException(
                detail ?? "Flashcard validation failed."
            ),
            FlashcardErrorCodes.FlashcardDeckNotFound => new FlashcardDeckNotFoundException(
                GetGuid(problemDetails, "deckId")
            ),
            FlashcardErrorCodes.FlashcardDeckArchived => new FlashcardDeckArchivedException(
                GetGuid(problemDetails, "deckId")
            ),
            FlashcardErrorCodes.DuplicateFlashcardDeckName =>
                new DuplicateFlashcardDeckNameException(
                    GetString(problemDetails, "deckName") ?? string.Empty
                ),
            FlashcardErrorCodes.FlashcardNotFound => new FlashcardNotFoundException(
                GetGuid(problemDetails, "flashcardId")
            ),
            FlashcardErrorCodes.FlashcardNotDue => new FlashcardNotDueException(
                GetGuid(problemDetails, "flashcardId")
            ),
            FlashcardErrorCodes.FlashcardImportFailed => new FlashcardImportException(
                detail ?? "The file could not be imported."
            ),
            CourseErrorCodes.CourseNotFound => new CourseNotFoundException(
                GetGuid(problemDetails, "courseId")
            ),
            CourseErrorCodes.CourseArchived => new CourseArchivedException(
                GetGuid(problemDetails, "courseId")
            ),
            SemesterErrorCodes.SemesterNotFound => new SemesterNotFoundException(
                GetGuid(problemDetails, "semesterId")
            ),
            SemesterErrorCodes.SemesterArchived => new SemesterArchivedException(
                GetGuid(problemDetails, "semesterId")
            ),
            NoteErrorCodes.NoteNotFound => new NoteNotFoundException(
                GetGuid(problemDetails, "noteId")
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
