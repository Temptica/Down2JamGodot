#nullable enable

using System.Collections.Generic;
using System.Text.Json.Serialization;

// GENERATED FILE - do not edit by hand; your changes will be overwritten.
//
// Source: the Jamcore OpenAPI document plus spec/d2jam.overlay.json.
// Regenerate with: dotnet run --project Down2Plugin

namespace Down2Jam.Client;

/// <summary>
/// Payload for DELETE /score.
/// </summary>
public sealed class D2JamDeleteScoreBody
{
    [JsonPropertyName("scoreId")]
    public required int ScoreId { get; set; }
}
