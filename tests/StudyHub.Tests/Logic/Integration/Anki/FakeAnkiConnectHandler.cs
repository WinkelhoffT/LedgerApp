using System.Net;
using System.Text;
using System.Text.Json;

namespace StudyHub.Tests.Logic.Integration.Anki;

/// <summary>
/// Answers AnkiConnect requests by action name and records the request bodies it received.
/// </summary>
public sealed class FakeAnkiConnectHandler : HttpMessageHandler
{
    private readonly Dictionary<string, Func<HttpResponseMessage>> _responses = new();

    public List<JsonElement> Requests { get; } = [];

    public List<long?> ContentLengths { get; } = [];

    public void RespondTo(string action, string json, HttpStatusCode statusCode = HttpStatusCode.OK) =>
        _responses[action] = () => new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

    public void ThrowOn(string action, Exception exception) =>
        _responses[action] = () => throw exception;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ContentLengths.Add(request.Content!.Headers.ContentLength);
        var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken)).RootElement.Clone();
        Requests.Add(body);

        var action = body.GetProperty("action").GetString()!;
        return _responses.TryGetValue(action, out var respond)
            ? respond()
            : throw new InvalidOperationException($"Unexpected AnkiConnect action '{action}'.");
    }
}
