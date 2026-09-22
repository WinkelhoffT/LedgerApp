using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using StudyHub.Logic.Integration.Contract.Documents;
using StudyHub.Shared.Courses;
using StudyHub.Shared.Documents;
using StudyHub.Shared.Semesters;

namespace StudyHub.Logic.Integration.Documents;

public sealed class DocumentAccessor(HttpClient httpClient) : IDocumentAccessor
{
    public async Task<IReadOnlyList<DocumentDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync("api/documents", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<IReadOnlyList<DocumentDto>>(cancellationToken))!;
    }

    public async Task<IReadOnlyList<DocumentDto>> GetByCourseIdAsync(Guid courseId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"api/documents/by-course/{courseId}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<IReadOnlyList<DocumentDto>>(cancellationToken))!;
    }

    public async Task<IReadOnlyList<DocumentDto>> GetBySemesterIdAsync(Guid semesterId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"api/documents/by-semester/{semesterId}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<IReadOnlyList<DocumentDto>>(cancellationToken))!;
    }

    public async Task<DocumentContentDto> DownloadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"api/documents/{id}/download", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var content = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName
            ?? "document";
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";

        return new DocumentContentDto(fileName.Trim('"'), contentType, content);
    }

    public async Task<DocumentDto> UploadAsync(UploadDocumentRequest request, CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();

        var fileContent = new ByteArrayContent(request.Content);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(request.ContentType);
        content.Add(fileContent, "file", request.FileName);

        if (request.CourseId is { } courseId)
        {
            content.Add(new StringContent(courseId.ToString()), "courseId");
        }

        if (request.SemesterId is { } semesterId)
        {
            content.Add(new StringContent(semesterId.ToString()), "semesterId");
        }

        using var response = await httpClient.PostAsync("api/documents", content, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<DocumentDto>(cancellationToken))!;
    }

    public async Task<DocumentDto> ArchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsync($"api/documents/{id}/archive", content: null, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<DocumentDto>(cancellationToken))!;
    }

    public async Task<DocumentDto> RestoreAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsync($"api/documents/{id}/restore", content: null, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<DocumentDto>(cancellationToken))!;
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
            DocumentErrorCodes.DocumentNotFound => new DocumentNotFoundException(GetGuid(problemDetails, "documentId")),
            DocumentErrorCodes.UnsupportedDocumentType => new UnsupportedDocumentTypeException(GetString(problemDetails, "contentType") ?? string.Empty),
            DocumentErrorCodes.DocumentTooLarge => new DocumentTooLargeException(GetLong(problemDetails, "sizeBytes"), 0),
            DocumentErrorCodes.DocumentValidationFailed => new DocumentValidationException(problemDetails?.Detail ?? "Document validation failed."),
            CourseErrorCodes.CourseNotFound => new CourseNotFoundException(GetGuid(problemDetails, "courseId")),
            CourseErrorCodes.CourseArchived => new CourseArchivedException(GetGuid(problemDetails, "courseId")),
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

    private static long GetLong(ProblemDetails? problemDetails, string key)
    {
        if (problemDetails is null
            || !problemDetails.Extensions.TryGetValue(key, out var value)
            || value is not JsonElement { ValueKind: JsonValueKind.Number } element
            || !element.TryGetInt64(out var number))
        {
            return 0;
        }

        return number;
    }
}
