using System.Text.Json;

namespace Down2Jam.Client;

/// <summary>
/// Unwraps Jamcore's envelope: <c>{"success": true, "data": ...}</c> on the way out,
/// <c>{"success": false, "error": {"code": ..., "message": ...}}</c> when something goes wrong.
/// Shared by <see cref="D2JamResult"/> and <see cref="D2JamResult{T}"/> so the two agree on
/// exactly what counts as success.
/// </summary>
internal readonly record struct D2JamEnvelope(
    bool Ok,
    int StatusCode,
    string Message,
    string ErrorCode,
    string ErrorMessage,
    JsonElement? Payload)
{
    public static D2JamEnvelope Unwrap(D2JamResponse? response)
    {
        if (response is null)
        {
            return new D2JamEnvelope(false, 0, "", "ERR_NO_RESPONSE", "The request produced no response.", null);
        }

        var statusCode = response.StatusCode;

        if (response.TransportError.Length > 0)
        {
            var errorCode = response.ClientErrorCode.Length > 0 ? response.ClientErrorCode : "ERR_TRANSPORT";
            return new D2JamEnvelope(false, statusCode, "", errorCode, response.TransportError, null);
        }

        var envelope = response.Json();

        if (envelope is null || envelope.Value.ValueKind != JsonValueKind.Object)
        {
            // Non-JSON bodies happen on gateway errors and on the odd HTML error page.
            if (response.IsSuccess)
            {
                return new D2JamEnvelope(true, statusCode, "", "", "", ToPayloadElement(response.Text()));
            }

            var text = response.Text().Trim();
            return new D2JamEnvelope(false, statusCode, "", $"ERR_HTTP_{statusCode}",
                text.Length > 300 ? text[..300] : text, null);
        }

        var body = envelope.Value;

        if (body.TryGetProperty("success", out var successProperty)
            && successProperty.ValueKind == JsonValueKind.True)
        {
            var message = body.TryGetProperty("message", out var messageProperty)
                ? messageProperty.ToString() : "";
            var data = body.TryGetProperty("data", out var dataProperty) ? dataProperty : (JsonElement?)null;
            return new D2JamEnvelope(true, statusCode, message, "", "", data);
        }

        // Some routes answer with a bare {"message": ...} and no success flag.
        if (!body.TryGetProperty("success", out _) && !body.TryGetProperty("error", out _) && response.IsSuccess)
        {
            var message = body.TryGetProperty("message", out var messageProperty)
                ? messageProperty.ToString() : "";
            var data = body.TryGetProperty("data", out var dataProperty) ? dataProperty : body;
            return new D2JamEnvelope(true, statusCode, message, "", "", data);
        }

        if (body.TryGetProperty("error", out var errorProperty) && errorProperty.ValueKind == JsonValueKind.Object)
        {
            var errorCode = errorProperty.TryGetProperty("code", out var codeProperty) ? codeProperty.ToString() : "";
            var errorMessage = errorProperty.TryGetProperty("message", out var msgProperty) ? msgProperty.ToString() : "";
            return new D2JamEnvelope(false, statusCode, "", errorCode, errorMessage, null);
        }

        var fallbackMessage = body.TryGetProperty("message", out var fallbackProperty)
            ? fallbackProperty.ToString() : "The request failed.";
        return new D2JamEnvelope(false, statusCode, "", $"ERR_HTTP_{statusCode}", fallbackMessage, null);
    }

    private static JsonElement? ToPayloadElement(string text)
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(text));
        return document.RootElement.Clone();
    }
}

/// <summary>Result of a call that returns no payload. Check <see cref="Ok"/>; on failure <see cref="ErrorMessage"/> says why.</summary>
public sealed class D2JamResult
{
    public bool Ok { get; private init; }

    /// <summary>HTTP status, or 0 when the request never completed.</summary>
    public int StatusCode { get; private init; }

    /// <summary>Human readable message the API attached to a successful response, if any.</summary>
    public string Message { get; private init; } = "";

    /// <summary>Machine readable failure code, for example "ERR_VALIDATION" or "ERR_UNAUTHORIZED".</summary>
    public string ErrorCode { get; private init; } = "";

    /// <summary>Human readable failure reason. Safe to show to a player.</summary>
    public string ErrorMessage { get; private init; } = "";

    /// <summary>The underlying HTTP response, for headers and diagnostics.</summary>
    public D2JamResponse? Response { get; private init; }

    /// <summary>True when the access token was rejected, meaning the player needs to log in again.</summary>
    public bool IsUnauthenticated => StatusCode == 401 || ErrorCode == "ERR_UNAUTHORIZED";

    /// <summary>True when the API refused because of its rate limit. <see cref="RetryAfter"/> says for how long.</summary>
    public bool IsRateLimited => StatusCode == 429;

    /// <summary>Seconds to wait before retrying a rate limited call, or -1 when the server did not say.</summary>
    public int RetryAfter()
    {
        if (Response is null)
        {
            return -1;
        }

        var value = Response.Header("Retry-After");
        return int.TryParse(value, out var seconds) ? seconds : Response.RateLimitReset();
    }

    /// <summary>A one line description suitable for a log or an error toast.</summary>
    public string Describe()
    {
        if (Ok)
        {
            return $"ok ({StatusCode})";
        }

        var reason = ErrorMessage.Length > 0 ? ErrorMessage : "unknown error";
        var code = ErrorCode.Length > 0 ? ErrorCode : $"HTTP {StatusCode}";
        return $"{code}: {reason}";
    }

    /// <summary>Build a result from a raw response. Called by generated client methods.</summary>
    public static D2JamResult From(D2JamResponse? response)
    {
        var envelope = D2JamEnvelope.Unwrap(response);
        return new D2JamResult
        {
            Ok = envelope.Ok,
            StatusCode = envelope.StatusCode,
            Message = envelope.Message,
            ErrorCode = envelope.ErrorCode,
            ErrorMessage = envelope.ErrorMessage,
            Response = response,
        };
    }
}

/// <summary>
/// Result carrying a typed payload. C# has real generics, so unlike the GDScript client there is
/// no per-endpoint result class -- this covers every payload shape, generated model or primitive.
/// </summary>
/// <example>
/// <code>
/// var result = await api.GetGameAsync("weldroot");
/// if (!result.Ok)
/// {
///     Log.Warn(result.Describe());
///     return;
/// }
/// Console.WriteLine(result.Data!.Name);
/// </code>
/// </example>
public sealed class D2JamResult<T>
{
    public bool Ok { get; private init; }
    public int StatusCode { get; private init; }
    public string Message { get; private init; } = "";
    public string ErrorCode { get; private init; } = "";
    public string ErrorMessage { get; private init; } = "";
    public D2JamResponse? Response { get; private init; }

    /// <summary>The parsed payload, or the default for <typeparamref name="T"/> when the request failed.</summary>
    public T? Data { get; private init; }

    public bool IsUnauthenticated => StatusCode == 401 || ErrorCode == "ERR_UNAUTHORIZED";
    public bool IsRateLimited => StatusCode == 429;

    public int RetryAfter()
    {
        if (Response is null)
        {
            return -1;
        }

        var value = Response.Header("Retry-After");
        return int.TryParse(value, out var seconds) ? seconds : Response.RateLimitReset();
    }

    public string Describe()
    {
        if (Ok)
        {
            return $"ok ({StatusCode})";
        }

        var reason = ErrorMessage.Length > 0 ? ErrorMessage : "unknown error";
        var code = ErrorCode.Length > 0 ? ErrorCode : $"HTTP {StatusCode}";
        return $"{code}: {reason}";
    }

    /// <summary>Build a result from a raw response, parsing the payload with <paramref name="parse"/> on success.</summary>
    public static D2JamResult<T> From(D2JamResponse? response, Func<JsonElement, T> parse)
    {
        var envelope = D2JamEnvelope.Unwrap(response);
        var data = envelope.Ok && envelope.Payload.HasValue ? parse(envelope.Payload.Value) : default;

        return new D2JamResult<T>
        {
            Ok = envelope.Ok,
            StatusCode = envelope.StatusCode,
            Message = envelope.Message,
            ErrorCode = envelope.ErrorCode,
            ErrorMessage = envelope.ErrorMessage,
            Response = response,
            Data = data,
        };
    }
}
