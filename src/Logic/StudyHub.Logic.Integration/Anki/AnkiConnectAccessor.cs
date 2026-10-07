using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using StudyHub.Shared.Anki;
using StudyHub.Shared.Configuration;

namespace StudyHub.Logic.Integration.Anki;

public sealed class AnkiConnectAccessor(HttpClient httpClient, IOptions<AnkiConnectOptions> options)
    : IAnkiConnectAccessor
{
    private const int ApiVersion = 6;

    public async Task<IReadOnlyList<AnkiDeckCountsDto>> GetDeckCountsAsync(CancellationToken cancellationToken = default)
    {
        // deckNamesAndIds rather than deckNames: getDeckStats reports only a deck's leaf name, so
        // full names ("Parent::Child") are looked up by deck id.
        var deckIdsByName = await SendAsync<Dictionary<string, long>>("deckNamesAndIds", null, cancellationToken);
        if (deckIdsByName.Count == 0)
        {
            return [];
        }

        var statsByDeckId = await SendAsync<Dictionary<string, AnkiDeckStats>>(
            "getDeckStats",
            new AnkiDeckStatsParams(deckIdsByName.Keys.ToList()),
            cancellationToken);

        var deckNamesById = deckIdsByName.ToDictionary(pair => pair.Value, pair => pair.Key);

        return statsByDeckId.Values
            .Where(stats => deckNamesById.ContainsKey(stats.DeckId))
            .Select(stats => new AnkiDeckCountsDto(
                deckNamesById[stats.DeckId],
                stats.NewCount,
                stats.LearnCount,
                stats.ReviewCount))
            .ToList();
    }

    private async Task<TResult> SendAsync<TResult>(string action, object? parameters, CancellationToken cancellationToken)
    {
        var request = new AnkiConnectRequest(action, ApiVersion, parameters, NullIfEmpty(options.Value.ApiKey));

        AnkiConnectResponse<TResult>? response;
        try
        {
            // Buffered StringContent rather than PostAsJsonAsync: AnkiConnect's minimal HTTP server
            // reads the body by Content-Length and doesn't understand chunked transfer encoding.
            using var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            using var httpResponse = await httpClient.PostAsync(string.Empty, content, cancellationToken);
            httpResponse.EnsureSuccessStatusCode();
            response = await httpResponse.Content.ReadFromJsonAsync<AnkiConnectResponse<TResult>>(cancellationToken);
        }
        catch (Exception exception) when (IsConnectionFailure(exception, cancellationToken))
        {
            throw new AnkiConnectUnavailableException($"AnkiConnect is not reachable ({action}).", exception);
        }

        if (response is null || response.Error is not null || response.Result is null)
        {
            throw new AnkiConnectUnavailableException(
                $"AnkiConnect returned an error for {action}: {response?.Error ?? "empty response"}");
        }

        return response.Result;
    }

    // A TaskCanceledException without a cancelled caller token is HttpClient's timeout.
    private static bool IsConnectionFailure(Exception exception, CancellationToken cancellationToken) =>
        exception is HttpRequestException or JsonException
        || (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested);

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
