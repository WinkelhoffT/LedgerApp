using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using StudyHub.Logic.Business.Courses;
using StudyHub.Logic.Business.Notes;
using StudyHub.Logic.Domain.Courses;
using StudyHub.Logic.Domain.Notes;

namespace StudyHub.UI.Notes;

/// <summary>
/// Adapter satisfying <see cref="INoteManagement"/> over HTTP against StudyHub.Api instead of
/// running the use case in-process. Transport failures (connection errors, timeouts, malformed
/// responses) are intentionally left untranslated and surface via Blazor's default error handling.
/// </summary>
public sealed class NoteApiClient(HttpClient httpClient) : INoteManagement
{
    public async Task<IReadOnlyList<NoteDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync("api/notes", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<IReadOnlyList<NoteDto>>(cancellationToken))!;
    }

    public async Task<IReadOnlyList<NoteDto>> GetByCourseIdAsync(Guid courseId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"api/notes/course/{courseId}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<IReadOnlyList<NoteDto>>(cancellationToken))!;
    }

    public async Task<NoteDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"api/notes/{id}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<NoteDto>(cancellationToken))!;
    }

    public async Task<NoteDto> CreateAsync(CreateNoteRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync("api/notes", request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<NoteDto>(cancellationToken))!;
    }

    public async Task<NoteDto> UpdateAsync(UpdateNoteRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync($"api/notes/{request.Id}", request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<NoteDto>(cancellationToken))!;
    }

    public async Task<NoteDto> ArchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsync($"api/notes/{id}/archive", content: null, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<NoteDto>(cancellationToken))!;
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
            NoteErrorCodes.NoteNotFound => new NoteNotFoundException(GetGuid(problemDetails, "noteId")),
            NoteErrorCodes.NoteArchived => new NoteArchivedException(GetGuid(problemDetails, "noteId")),
            NoteErrorCodes.NoteValidationFailed => new NoteValidationException(problemDetails?.Detail ?? "Note validation failed."),
            CourseErrorCodes.CourseNotFound => new CourseNotFoundException(GetGuid(problemDetails, "courseId")),
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
