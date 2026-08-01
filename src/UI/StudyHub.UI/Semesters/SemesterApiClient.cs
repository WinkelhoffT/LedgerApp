using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using StudyHub.Logic.Business.Semesters;
using StudyHub.Logic.Domain.Semesters;

namespace StudyHub.UI.Semesters;

/// <summary>
/// Adapter satisfying <see cref="ISemesterManagement"/> over HTTP against StudyHub.Api instead of
/// running the use case in-process. Transport failures (connection errors, timeouts, malformed
/// responses) are intentionally left untranslated and surface via Blazor's default error handling.
/// </summary>
public sealed class SemesterApiClient(HttpClient httpClient) : ISemesterManagement
{
    public async Task<IReadOnlyList<SemesterDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync("api/semesters", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<IReadOnlyList<SemesterDto>>(cancellationToken))!;
    }

    public async Task<SemesterDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"api/semesters/{id}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<SemesterDto>(cancellationToken))!;
    }

    public async Task<SemesterDto> CreateAsync(CreateSemesterRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync("api/semesters", request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<SemesterDto>(cancellationToken))!;
    }

    public async Task<SemesterDto> UpdateAsync(UpdateSemesterRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync($"api/semesters/{request.Id}", request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<SemesterDto>(cancellationToken))!;
    }

    public async Task<SemesterDto> ArchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsync($"api/semesters/{id}/archive", content: null, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<SemesterDto>(cancellationToken))!;
    }

    public async Task<SemesterDto> RestoreAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsync($"api/semesters/{id}/restore", content: null, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<SemesterDto>(cancellationToken))!;
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
            SemesterErrorCodes.SemesterNotFound => new SemesterNotFoundException(GetGuid(problemDetails, "semesterId")),
            SemesterErrorCodes.DuplicateSemesterName => new DuplicateSemesterNameException(GetString(problemDetails, "semesterName") ?? string.Empty),
            SemesterErrorCodes.SemesterArchived => new SemesterArchivedException(GetGuid(problemDetails, "semesterId")),
            SemesterErrorCodes.SemesterValidationFailed => new SemesterValidationException(problemDetails?.Detail ?? "Semester validation failed."),
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
