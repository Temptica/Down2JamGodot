using System.Collections;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Down2Jam.Client;

/// <summary>
/// Transport, authentication and error plumbing the generated <c>D2JamApi</c> partial class
/// builds on.
///
/// Everything here is hand written and safe to edit; regenerating the client only touches the
/// other half of the <c>partial class</c>. Construct a <c>D2JamApi</c> through
/// <see cref="D2JamService"/> rather than directly.
/// </summary>
public class D2JamApiBase : IDisposable
{
    /// <summary>The public Down2Jam instance.</summary>
    public const string DefaultHost = "https://d2jam.com/api/v1";

    /// <summary>Raised when a call fails, before the result is returned. Handy for a global error toast.</summary>
    public event Action<D2JamResult>? RequestFailed;

    /// <summary>Raised when the API rejects the stored token. The player has to log in again; <see cref="D2JamAuth"/> clears its session when this fires.</summary>
    public event Action? Unauthenticated;

    /// <summary>Raised when the API asks the client to slow down, with the seconds it wants you to wait.</summary>
    public event Action<int>? RateLimited;

    /// <summary>Base URL including the version prefix. Point this at a local Jamcore instance to test.</summary>
    public string ApiHost { get; set; } = DefaultHost;

    /// <summary>Supplies the game token as a bearer credential. Optional: public endpoints work without it.</summary>
    public D2JamAuth? Auth { get; set; }

    /// <summary>Log failed calls to the console. Turn off if you handle every result yourself.</summary>
    public bool WarnOnFailure { get; set; } = true;

    private readonly D2JamHttpClient _client = new();

    /// <summary>
    /// Perform a request against the API. Generated methods call this; call it yourself for an
    /// endpoint the overlay does not cover yet.
    ///
    /// <paramref name="body"/> is JSON-encoded (via its own runtime type) when it is not null.
    /// <paramref name="requiresAuth"/> reflects what the spec says about the endpoint; credentials
    /// are attached whenever they are available regardless, because several endpoints change
    /// their answer for a logged in user.
    /// </summary>
    protected async Task<D2JamResponse> RequestAsync(
        string path,
        HttpMethod method,
        IReadOnlyDictionary<string, object?>? query,
        object? body,
        bool requiresAuth)
    {
        var headers = new List<KeyValuePair<string, string>> { new("Accept", "application/json") };
        var payload = "";

        if (body is not null)
        {
            headers.Add(new("Content-Type", "application/json"));
            payload = JsonSerializer.Serialize(body);
        }

        if (!TryAttachCredentials(headers, path, requiresAuth))
        {
            return MissingCredentialsResponse(path, method);
        }

        var url = ApiHost + path + EncodeQuery(query);
        var response = await _client.RequestAsync(url, method, headers, payload).ConfigureAwait(false);

        AfterResponse(response);
        return response;
    }

    /// <summary>Upload a single file as multipart/form-data.</summary>
    protected async Task<D2JamResponse> RequestMultipartAsync(
        string path,
        string fieldName,
        byte[] fileBytes,
        string fileName,
        bool requiresAuth)
    {
        var headers = new List<KeyValuePair<string, string>> { new("Accept", "application/json") };

        if (!TryAttachCredentials(headers, path, requiresAuth))
        {
            return MissingCredentialsResponse(path, HttpMethod.Post);
        }

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(GuessMimeType(fileName));
        content.Add(fileContent, fieldName, fileName);

        var url = ApiHost + path;
        var response = await _client.RequestRawAsync(url, HttpMethod.Post, headers, content).ConfigureAwait(false);

        AfterResponse(response);
        return response;
    }

    /// <summary>
    /// Turn a server-relative path such as "/api/v1/image/abc.png" into an absolute URL. Values
    /// that are already absolute are returned untouched.
    /// </summary>
    protected string ResolveUrl(string path)
    {
        if (path.Length == 0 || path.StartsWith("http://", StringComparison.Ordinal)
            || path.StartsWith("https://", StringComparison.Ordinal))
        {
            return path;
        }

        // ApiHost carries the version prefix that server-relative paths already include, so strip
        // it back to the origin before joining.
        var origin = ApiHost;
        var schemeEnd = origin.IndexOf("://", StringComparison.Ordinal);
        var hostEnd = schemeEnd >= 0 ? origin.IndexOf('/', schemeEnd + 3) : origin.IndexOf('/');
        if (hostEnd > 0)
        {
            origin = origin[..hostEnd];
        }

        return origin + (path.StartsWith('/') ? "" : "/") + path;
    }

    /// <summary>
    /// Report a failed result through <see cref="RequestFailed"/> and the log. Generated methods
    /// do not call this; <see cref="D2JamService"/> wires it up so a single handler can watch
    /// every call.
    /// </summary>
    public void Report(D2JamResult result)
    {
        if (result.Ok)
        {
            return;
        }

        if (WarnOnFailure)
        {
            Console.Error.WriteLine($"[d2jam] {result.Describe()}");
        }

        RequestFailed?.Invoke(result);
    }

    /// <summary>Overload for a typed result, since C#'s generics mean there is one <see cref="D2JamResult{T}"/> for every payload shape rather than a distinct class per endpoint.</summary>
    public void Report<T>(D2JamResult<T> result)
    {
        if (result.Ok)
        {
            return;
        }

        Report(D2JamResult.From(result.Response));
    }

    private bool TryAttachCredentials(List<KeyValuePair<string, string>> headers, string path, bool requiresAuth)
    {
        if (Auth is null || !Auth.IsLoggedIn)
        {
            if (requiresAuth)
            {
                // A warning rather than a thrown exception: a session can expire mid-session
                // through no fault of the caller, and the returned result already says what
                // happened.
                if (WarnOnFailure)
                {
                    Console.Error.WriteLine($"[d2jam] {path} needs a logged in user; no session is available.");
                }

                return false;
            }

            return true;
        }

        headers.AddRange(Auth.AuthorizationHeaders());
        return true;
    }

    private D2JamResponse MissingCredentialsResponse(string path, HttpMethod method) => new()
    {
        Url = ApiHost + path,
        Method = method,
        StatusCode = 401,
        ClientErrorCode = "ERR_NOT_LOGGED_IN",
        TransportError = "Not logged in. Call D2JamService.LinkDeviceAsync() first.",
    };

    private void AfterResponse(D2JamResponse response)
    {
        if (response.StatusCode == 401)
        {
            Auth?.Invalidate();
            Unauthenticated?.Invoke();
        }
        else if (response.StatusCode == 429)
        {
            var retry = response.Header("Retry-After");
            RateLimited?.Invoke(int.TryParse(retry, out var seconds) ? seconds : response.RateLimitReset());
        }
    }

    /// <summary>
    /// Build a query string, leading "?" included, from a dictionary. Returns "" for a null or
    /// empty dictionary. Null values are skipped; enumerables are repeated as "key=a&amp;key=b",
    /// which is how Jamcore reads multi-valued parameters.
    /// </summary>
    private static string EncodeQuery(IReadOnlyDictionary<string, object?>? query)
    {
        if (query is null || query.Count == 0)
        {
            return "";
        }

        var parts = new List<string>();

        foreach (var (key, value) in query)
        {
            if (value is null)
            {
                continue;
            }

            var encodedKey = Uri.EscapeDataString(key);

            if (value is IEnumerable enumerable && value is not string)
            {
                foreach (var entry in enumerable)
                {
                    parts.Add($"{encodedKey}={EncodeValue(entry)}");
                }
            }
            else
            {
                parts.Add($"{encodedKey}={EncodeValue(value)}");
            }
        }

        return parts.Count == 0 ? "" : "?" + string.Join("&", parts);
    }

    private static string EncodeValue(object? value)
    {
        if (value is bool flag)
        {
            return flag ? "true" : "false";
        }

        return Uri.EscapeDataString(Convert.ToString(value, CultureInfo.InvariantCulture) ?? "");
    }

    /// <summary>Best-effort MIME type from a file name. Only the formats the API accepts are recognised.</summary>
    private static string GuessMimeType(string fileName) => Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant() switch
    {
        "png" => "image/png",
        "jpg" or "jpeg" => "image/jpeg",
        "gif" => "image/gif",
        "webp" => "image/webp",
        _ => "application/octet-stream",
    };

    public void Dispose() => _client.Dispose();
}
