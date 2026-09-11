using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Down2Jam.Client;

/// <summary>
/// A raw HTTP response from <see cref="D2JamHttpClient"/>, before any Jamcore envelope is
/// unwrapped.
///
/// Read the typed <see cref="D2JamResult{T}"/> a client method returns instead, normally. This is
/// here for what the typed layer cannot express: inspecting rate limit headers, reading the
/// rotated access token, or debugging a request that failed before it reached the API.
/// </summary>
public sealed class D2JamResponse
{
    /// <summary>The URL that was requested, query string included.</summary>
    public required string Url { get; init; }

    public required HttpMethod Method { get; init; }

    /// <summary>HTTP status, or 0 when the request never completed.</summary>
    public int StatusCode { get; init; }

    /// <summary>Response headers in the order the server sent them. A name can repeat (Set-Cookie).</summary>
    public IReadOnlyList<KeyValuePair<string, string>> Headers { get; init; } = [];

    public byte[] Body { get; init; } = [];

    /// <summary>Set when the request could not be made at all: a DNS failure, a timeout, a refused connection.</summary>
    public string TransportError { get; init; } = "";

    /// <summary>
    /// Error code to report when the client refused to send the request, rather than the network
    /// or the API failing. Lets a locally detected problem carry a meaningful code instead of a
    /// generic transport failure.
    /// </summary>
    public string ClientErrorCode { get; init; } = "";

    /// <summary>True when the exchange completed and the status is in the 2xx range.</summary>
    public bool IsSuccess => TransportError.Length == 0 && StatusCode is >= 200 and < 300;

    /// <summary>The body decoded as UTF-8 text.</summary>
    public string Text() => Encoding.UTF8.GetString(Body);

    /// <summary>
    /// The body parsed as JSON, or null when it is empty or malformed.
    ///
    /// A non-JSON body -- a gateway's HTML error page, say -- returns null rather than throwing,
    /// so a malformed response does not crash every retry.
    /// </summary>
    public JsonElement? Json()
    {
        if (Body.Length == 0)
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(Body);
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>First header with the given name, or null. Matching is case-insensitive.</summary>
    public string? Header(string name) => HeaderValues(name).FirstOrDefault();

    /// <summary>Every header with the given name, in the order the server sent them.</summary>
    public IEnumerable<string> HeaderValues(string name) =>
        Headers.Where(header => string.Equals(header.Key, name, StringComparison.OrdinalIgnoreCase))
            .Select(header => header.Value);

    /// <summary>Seconds until the rate limit window resets, or -1 when the server did not say.</summary>
    public int RateLimitReset() =>
        int.TryParse(Header("RateLimit-Reset"), out var value) ? value : -1;

    /// <summary>Requests left in the current rate limit window, or -1 when the server did not say.</summary>
    public int RateLimitRemaining() =>
        int.TryParse(Header("RateLimit-Remaining"), out var value) ? value : -1;

    /// <summary>The request id Jamcore assigns. Worth quoting when reporting an API bug.</summary>
    public string RequestId() => Header("X-Request-Id") ?? "";

    public override string ToString() => TransportError.Length > 0
        ? $"D2JamResponse({Url}, transport error: {TransportError})"
        : $"D2JamResponse({Url}, {StatusCode})";
}
