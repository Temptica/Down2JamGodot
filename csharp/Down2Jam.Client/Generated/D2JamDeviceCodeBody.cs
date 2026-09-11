#nullable enable

using System.Collections.Generic;
using System.Text.Json.Serialization;

// GENERATED FILE - do not edit by hand; your changes will be overwritten.
//
// Source: the Jamcore OpenAPI document plus spec/d2jam.overlay.json.
// Regenerate with: dotnet run --project Down2Plugin

namespace Down2Jam.Client;

/// <summary>
/// Payload for POST /device/code.
/// </summary>
public sealed class D2JamDeviceCodeBody
{
    /// <summary>
    /// Human readable name for this device or install, shown to the player when they approve it on
    /// the website.
    /// </summary>
    [JsonPropertyName("clientName")]
    public required string ClientName { get; set; }

    /// <summary>
    /// The game this token will be scoped to. The issued game token only ever works for this one
    /// game -- see D2JamService.game_slug.
    /// </summary>
    [JsonPropertyName("gameSlug")]
    public required string GameSlug { get; set; }
}
