using System.Net.Http;

namespace Down2Jam.Client;

/// <summary>
/// Thin async wrapper around <see cref="HttpClient"/>.
///
/// One shared <see cref="HttpClient"/> handles every call, which is the .NET idiom (unlike
/// Godot's <c>HTTPRequest</c> node, a fresh one per call has no connection-reuse benefit here).
/// Every call resolves to a <see cref="D2JamResponse"/>; transport failures come back as a
/// response with <see cref="D2JamResponse.TransportError"/> set rather than as an exception to
/// catch.
/// </summary>
public sealed class D2JamHttpClient : IDisposable
{
    private readonly HttpClient _client;

    public D2JamHttpClient(TimeSpan? timeout = null)
    {
        _client = new HttpClient
        {
            Timeout = timeout ?? TimeSpan.FromSeconds(20),
        };
    }

    /// <summary>Print every request and its status. Useful while wiring a game up.</summary>
    public bool Verbose { get; set; }

    /// <summary>Perform a request with a text body (or no body at all).</summary>
    public Task<D2JamResponse> RequestAsync(
        string url,
        HttpMethod method,
        IEnumerable<KeyValuePair<string, string>>? headers = null,
        string body = "",
        CancellationToken cancellationToken = default) =>
        PerformAsync(url, method, headers, body.Length > 0 ? new StringContent(body) : null, cancellationToken);

    /// <summary>Perform a request with a raw binary body, for uploads.</summary>
    public Task<D2JamResponse> RequestRawAsync(
        string url,
        HttpMethod method,
        IEnumerable<KeyValuePair<string, string>> headers,
        HttpContent content,
        CancellationToken cancellationToken = default) =>
        PerformAsync(url, method, headers, content, cancellationToken);

    private async Task<D2JamResponse> PerformAsync(
        string url,
        HttpMethod method,
        IEnumerable<KeyValuePair<string, string>>? headers,
        HttpContent? content,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, url) { Content = content };

        foreach (var (name, value) in headers ?? [])
        {
            // Content-Type/Content-Length live on HttpContent.Headers, not the request's own
            // header collection. TryAddWithoutValidation on HttpRequestHeaders accepts a
            // content header without error but without it taking effect either, so route it
            // explicitly rather than relying on that to fail loudly.
            if (content is not null && IsContentHeader(name))
            {
                content.Headers.Remove(name);
                content.Headers.TryAddWithoutValidation(name, value);
            }
            else
            {
                request.Headers.TryAddWithoutValidation(name, value);
            }
        }

        D2JamResponse response;

        try
        {
            using var httpResponse = await _client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var body = await httpResponse.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);

            response = new D2JamResponse
            {
                Url = url,
                Method = method,
                StatusCode = (int)httpResponse.StatusCode,
                Headers = CollectHeaders(httpResponse),
                Body = body,
            };
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            response = new D2JamResponse { Url = url, Method = method, TransportError = "The request timed out." };
        }
        catch (HttpRequestException exception)
        {
            response = new D2JamResponse
            {
                Url = url,
                Method = method,
                TransportError = exception.Message,
            };
        }

        Log(response);
        return response;
    }

    private static bool IsContentHeader(string name) => name switch
    {
        "Content-Type" or "Content-Length" or "Content-Encoding" or "Content-Disposition" => true,
        _ => false,
    };

    private static List<KeyValuePair<string, string>> CollectHeaders(HttpResponseMessage response)
    {
        var headers = new List<KeyValuePair<string, string>>();

        foreach (var header in response.Headers)
        {
            headers.AddRange(header.Value.Select(value => new KeyValuePair<string, string>(header.Key, value)));
        }

        foreach (var header in response.Content.Headers)
        {
            headers.AddRange(header.Value.Select(value => new KeyValuePair<string, string>(header.Key, value)));
        }

        return headers;
    }

    private void Log(D2JamResponse response)
    {
        if (Verbose)
        {
            Console.WriteLine($"[d2jam] {response}");
        }
    }

    public void Dispose() => _client.Dispose();
}
