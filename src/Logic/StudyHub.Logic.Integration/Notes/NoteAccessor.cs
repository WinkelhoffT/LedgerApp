using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using StudyHub.Logic.Integration.Contract.Notes;
using StudyHub.Shared.Courses;
using StudyHub.Shared.Documents;
using StudyHub.Shared.Notes;
using StudyHub.Shared.Semesters;

namespace StudyHub.Logic.Integration.Notes;

public sealed class NoteAccessor(HttpClient httpClient) : INoteAccessor
{
    public async Task<IReadOnlyList<NoteDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync("api/notes", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<IReadOnlyList<NoteDto>>(cancellationToken))!;
    }

    public async Task<IReadOnlyList<NoteDto>> GetByCourseIdAsync(Guid courseId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"api/notes/by-course/{courseId}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<IReadOnlyList<NoteDto>>(cancellationToken))!;
    }

    public async Task<IReadOnlyList<NoteDto>> GetBySemesterIdAsync(Guid semesterId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"api/notes/by-semester/{semesterId}", cancellationToken);
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

    public async Task<NoteDto> RestoreAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsync($"api/notes/{id}/restore", content: null, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<NoteDto>(cancellationToken))!;
    }

    public async Task<NoteDto> AttachDocumentAsync(Guid noteId, Guid documentId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsync($"api/notes/{noteId}/documents/{documentId}", content: null, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<NoteDto>(cancellationToken))!;
    }

    public async Task<NoteDto> DetachDocumentAsync(Guid noteId, Guid documentId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.DeleteAsync($"api/notes/{noteId}/documents/{documentId}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<NoteDto>(cancellationToken))!;
    }

    public async Task<IReadOnlyList<NoteBacklinkDto>> GetBacklinksAsync(Guid noteId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"api/notes/{noteId}/backlinks", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<IReadOnlyList<NoteBacklinkDto>>(cancellationToken))!;
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
            NoteErrorCodes.DuplicateNoteTitle => new DuplicateNoteTitleException(GetString(problemDetails, "noteTitle") ?? string.Empty),
            NoteErrorCodes.NoteArchived => new NoteArchivedException(GetGuid(problemDetails, "noteId")),
            NoteErrorCodes.NoteValidationFailed => new NoteValidationException(problemDetails?.Detail ?? "Note validation failed."),
            CourseErrorCodes.CourseNotFound => new CourseNotFoundException(GetGuid(problemDetails, "courseId")),
            CourseErrorCodes.CourseArchived => new CourseArchivedException(GetGuid(problemDetails, "courseId")),
            SemesterErrorCodes.SemesterNotFound => new SemesterNotFoundException(GetGuid(problemDetails, "semesterId")),
            SemesterErrorCodes.SemesterArchived => new SemesterArchivedException(GetGuid(problemDetails, "semesterId")),
            DocumentErrorCodes.DocumentNotFound => new DocumentNotFoundException(GetGuid(problemDetails, "documentId")),
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
