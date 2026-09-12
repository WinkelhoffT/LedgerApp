using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using StudyHub.Shared.Courses;
using StudyHub.Shared.Semesters;

namespace StudyHub.Logic.Integration.Courses;

public sealed class CourseAccessor(HttpClient httpClient) : ICourseAccessor
{
    public async Task<IReadOnlyList<CourseDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync("api/courses", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<IReadOnlyList<CourseDto>>(cancellationToken))!;
    }

    public async Task<CourseDto> CreateAsync(CreateCourseRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync("api/courses", request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<CourseDto>(cancellationToken))!;
    }

    public async Task<CourseDto> UpdateAsync(UpdateCourseRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync($"api/courses/{request.Id}", request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<CourseDto>(cancellationToken))!;
    }

    public async Task<CourseDto> ArchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsync($"api/courses/{id}/archive", content: null, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<CourseDto>(cancellationToken))!;
    }

    public async Task<CourseDto> RestoreAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsync($"api/courses/{id}/restore", content: null, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<CourseDto>(cancellationToken))!;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken);
        var errorCode = GetString(problemDetails, "errorCode");

        throw errorCode switch
        {
            CourseErrorCodes.CourseNotFound => new CourseNotFoundException(GetGuid(problemDetails, "courseId")),
            CourseErrorCodes.DuplicateCourseName => new DuplicateCourseNameException(GetString(problemDetails, "courseName") ?? string.Empty),
            CourseErrorCodes.CourseArchived => new CourseArchivedException(GetGuid(problemDetails, "courseId")),
            CourseErrorCodes.CourseValidationFailed => new CourseValidationException(problemDetails?.Detail ?? "Course validation failed."),
            SemesterErrorCodes.SemesterNotFound => new SemesterNotFoundException(GetGuid(problemDetails, "semesterId")),
            SemesterErrorCodes.SemesterArchived => new SemesterArchivedException(GetGuid(problemDetails, "semesterId")),
            _ => new HttpRequestException(
                $"StudyHub.Api returned {(int)response.StatusCode} ({response.StatusCode}): {problemDetails?.Detail}",
                inner: null,
                response.StatusCode),
        };
    }

    private static string? GetString(ProblemDetails? problemDetails, string key)
    {
        if (problemDetails is null
            || !problemDetails.Extensions.TryGetValue(key, out var value)
            || value is not JsonElement { ValueKind: JsonValueKind.String } element)
        {
            return null;
        }

        return element.GetString();
    }

    private static Guid GetGuid(ProblemDetails? problemDetails, string key)
    {
        if (problemDetails is null
            || !problemDetails.Extensions.TryGetValue(key, out var value)
            || value is not JsonElement { ValueKind: JsonValueKind.String } element
            || !element.TryGetGuid(out var guid))
        {
            return Guid.Empty;
        }

        return guid;
    }
}
