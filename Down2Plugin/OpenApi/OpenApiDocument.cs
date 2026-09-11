using System.Text.Json;

namespace Down2Plugin.OpenApi;

/// <summary>
/// The slice of an OpenAPI document this generator cares about: operations, their
/// parameters, and the Jamcore `x-jamcore-auth` extension.
/// </summary>
public sealed class OpenApiDocument
{
    public required string Title { get; init; }
    public required string Version { get; init; }
    public required string ServerUrl { get; init; }
    public required IReadOnlyList<OpenApiOperation> Operations { get; init; }

    private static readonly string[] HttpMethods =
        ["get", "post", "put", "patch", "delete", "head", "options"];

    public static OpenApiDocument Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (!root.TryGetProperty("paths", out var paths))
        {
            throw new InvalidDataException("The document has no 'paths' object; is it really an OpenAPI spec?");
        }

        var info = root.TryGetProperty("info", out var infoElement) ? infoElement : default;
        var operations = new List<OpenApiOperation>();

        foreach (var path in paths.EnumerateObject())
        {
            foreach (var verb in path.Value.EnumerateObject())
            {
                if (!HttpMethods.Contains(verb.Name, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                operations.Add(ParseOperation(path.Name, verb.Name.ToLowerInvariant(), verb.Value));
            }
        }

        return new OpenApiDocument
        {
            Title = ReadString(info, "title") ?? "API",
            Version = ReadString(info, "version") ?? "0.0.0",
            ServerUrl = ReadServerUrl(root),
            Operations = operations,
        };
    }

    private static OpenApiOperation ParseOperation(string path, string method, JsonElement element)
    {
        var parameters = new List<OpenApiParameter>();

        if (element.TryGetProperty("parameters", out var parameterArray))
        {
            foreach (var parameter in parameterArray.EnumerateArray())
            {
                var name = ReadString(parameter, "name");
                var location = ReadString(parameter, "in");
                if (name is null || location is null)
                {
                    continue;
                }

                var type = "string";
                if (parameter.TryGetProperty("schema", out var schema))
                {
                    type = ReadString(schema, "type") ?? "string";
                }

                parameters.Add(new OpenApiParameter
                {
                    Name = name,
                    In = location,
                    Type = type,
                    Required = parameter.TryGetProperty("required", out var required)
                               && required.ValueKind == JsonValueKind.True,
                    Description = ReadString(parameter, "description"),
                });
            }
        }

        var authRequired = false;
        var authOptional = false;
        var authLabel = "Public";

        if (element.TryGetProperty("x-jamcore-auth", out var auth))
        {
            authRequired = auth.TryGetProperty("required", out var r) && r.ValueKind == JsonValueKind.True;
            authOptional = auth.TryGetProperty("optional", out var o) && o.ValueKind == JsonValueKind.True;
            authLabel = ReadString(auth, "label") ?? authLabel;
        }

        return new OpenApiOperation
        {
            Path = path,
            Method = method,
            Summary = ReadString(element, "summary"),
            Tags = element.TryGetProperty("tags", out var tags)
                ? tags.EnumerateArray().Select(t => t.GetString() ?? string.Empty).ToArray()
                : [],
            Parameters = parameters,
            RequiresAuth = authRequired,
            OptionalAuth = authOptional,
            AuthLabel = authLabel,
        };
    }

    private static string ReadServerUrl(JsonElement root)
    {
        if (root.TryGetProperty("servers", out var servers)
            && servers.ValueKind == JsonValueKind.Array
            && servers.GetArrayLength() > 0)
        {
            return ReadString(servers[0], "url") ?? "/";
        }

        return "/";
    }

    private static string? ReadString(JsonElement element, string property)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }
}

public sealed class OpenApiOperation
{
    public required string Path { get; init; }
    public required string Method { get; init; }
    public string? Summary { get; init; }
    public required IReadOnlyList<string> Tags { get; init; }
    public required IReadOnlyList<OpenApiParameter> Parameters { get; init; }
    public required bool RequiresAuth { get; init; }
    public required bool OptionalAuth { get; init; }
    public required string AuthLabel { get; init; }

    /// <summary>Key used to look this operation up in the overlay, for example "get /games/{gameSlug}".</summary>
    public string Key => $"{Method} {Path}";
}

public sealed class OpenApiParameter
{
    public required string Name { get; init; }
    public required string In { get; init; }
    public required string Type { get; init; }
    public required bool Required { get; init; }
    public string? Description { get; init; }
}
