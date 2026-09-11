using System.Text.Json;
using System.Text.Json.Serialization;

namespace Down2Plugin.Overlay;

/// <summary>
/// The hand maintained companion to the OpenAPI document.
///
/// Jamcore publishes accurate paths and parameters but types every body as a free form
/// object and every response as an untyped envelope, so the shapes live here instead.
/// The overlay also decides scope: an operation with no entry is not generated.
/// </summary>
public sealed class OverlayDocument
{
    [JsonPropertyName("classPrefix")]
    public string ClassPrefix { get; init; } = "D2Jam";

    [JsonPropertyName("models")]
    public Dictionary<string, OverlayModel> Models { get; init; } = [];

    [JsonPropertyName("bodies")]
    public Dictionary<string, OverlayModel> Bodies { get; init; } = [];

    [JsonPropertyName("operations")]
    public Dictionary<string, OverlayOperation> Operations { get; init; } = [];

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static async Task<OverlayDocument> LoadAsync(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Overlay file not found: {path}");
        }

        var json = await File.ReadAllTextAsync(path, cancellationToken);
        var overlay = JsonSerializer.Deserialize<OverlayDocument>(json, SerializerOptions)
                      ?? throw new InvalidDataException($"Overlay at {path} is empty.");

        overlay.Validate(path);
        return overlay;
    }

    private void Validate(string path)
    {
        foreach (var (name, operation) in Operations)
        {
            if (string.IsNullOrWhiteSpace(operation.Name))
            {
                throw new InvalidDataException($"{path}: operation '{name}' has no 'name'.");
            }

            if (operation.Body is not null && !Bodies.ContainsKey(operation.Body))
            {
                throw new InvalidDataException(
                    $"{path}: operation '{name}' references unknown body '{operation.Body}'.");
            }
        }
    }

    /// <summary>Models and bodies share a namespace; bodies just gain a `create` constructor.</summary>
    public IEnumerable<KeyValuePair<string, OverlayModel>> AllShapes =>
        Models.Concat(Bodies);

    public bool IsShape(string name) => Models.ContainsKey(name) || Bodies.ContainsKey(name);
}

public sealed class OverlayModel
{
    [JsonPropertyName("doc")]
    public string? Doc { get; init; }

    [JsonPropertyName("fields")]
    public List<OverlayField> Fields { get; init; } = [];
}

public sealed class OverlayField
{
    /// <summary>The property name as the API spells it, for example "profilePicture".</summary>
    [JsonPropertyName("json")]
    public required string Json { get; init; }

    /// <summary>An overlay type: a primitive, a model name, or Array[...] of either.</summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("doc")]
    public string? Doc { get; init; }

    /// <summary>Required body fields become parameters of the generated `create` constructor.</summary>
    [JsonPropertyName("required")]
    public bool Required { get; init; }
}

public sealed class OverlayOperation
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("doc")]
    public string? Doc { get; init; }

    [JsonPropertyName("body")]
    public string? Body { get; init; }

    [JsonPropertyName("multipart")]
    public OverlayMultipart? Multipart { get; init; }

    /// <summary>When true the string payload is a server relative path and is resolved against the API host.</summary>
    [JsonPropertyName("resolveUrl")]
    public bool ResolveUrl { get; init; }

    /// <summary>An overlay type, "void", or Array[...] of a model.</summary>
    [JsonPropertyName("returns")]
    public string Returns { get; init; } = "void";
}

public sealed class OverlayMultipart
{
    [JsonPropertyName("fileField")]
    public required string FileField { get; init; }
}
