using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using StudyHub.Shared.Courses;
using StudyHub.Shared.Semesters;
using StudyHub.Shared.StudySessions;

namespace StudyHub.Logic.Integration.StudySessions;

public sealed class StudySessionAccessor(HttpClient httpClient) : IStudySessionAccessor
{
    public async Task<StudySessionDto> CreateAsync(
        CreateStudySessionRequest request,
        CancellationToken cancellationToken = default
    )
    {
        using var response = await httpClient.PostAsJsonAsync(
            "api/study-sessions",
            request,
            cancellationToken
        );
        await EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<StudySessionDto>(cancellationToken))!;
    }

    public async Task<StudySessionDto> UpdateAsync(
        UpdateStudySessionRequest request,
        CancellationToken cancellationToken = default
    )
    {
        using var response = await httpClient.PutAsJsonAsync(
            $"api/study-sessions/{request.Id}",
            request,
            cancellationToken
        );
        await EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<StudySessionDto>(cancellationToken))!;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.DeleteAsync(
            $"api/study-sessions/{id}",
            cancellationToken
        );
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task<StudySessionDto> CompleteAsync(
        Guid id,
        CompleteStudySessionRequest request,
        CancellationToken cancellationToken = default
    )
    {
        using var response = await httpClient.PutAsJsonAsync(
            $"api/study-sessions/{id}/completion",
            request,
            cancellationToken
        );
        await EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<StudySessionDto>(cancellationToken))!;
    }

    public async Task<StudySessionDto> ResetCompletionAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        using var response = await httpClient.DeleteAsync(
            $"api/study-sessions/{id}/completion",
            cancellationToken
        );
        await EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<StudySessionDto>(cancellationToken))!;
    }

    private static async Task EnsureSuccessAsync(
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

        throw errorCode switch
        {
            StudySessionErrorCodes.StudySessionValidationFailed =>
                new StudySessionValidationException(
                    problemDetails?.Detail ?? "Study session validation failed."
                ),
            StudySessionErrorCodes.StudySessionNotFound => new StudySessionNotFoundException(
                GetGuid(problemDetails, "studySessionId")
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
            _ => new HttpRequestException(
                $"StudyHub.Api returned {(int)response.StatusCode} ({response.StatusCode}): {problemDetails?.Detail}",
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
