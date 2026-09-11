#nullable enable

using System.Collections.Generic;
using System.Text.Json.Serialization;

// GENERATED FILE - do not edit by hand; your changes will be overwritten.
//
// Source: the Jamcore OpenAPI document plus spec/d2jam.overlay.json.
// Regenerate with: dotnet run --project Down2Plugin

namespace Down2Jam.Client;

/// <summary>
/// The result of one poll of a pending device authorization request. A denied or expired
/// request comes back as a failed D2JamResult instead of a status here.
/// </summary>
public sealed class D2JamDeviceToken
{
    /// <summary>
    /// authorization_pending, slow_down, or approved.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = "";

    /// <summary>
    /// The game's access token. Only present when status is approved; empty otherwise.
    /// </summary>
    [JsonPropertyName("token")]
    public string Token { get; set; } = "";

    /// <summary>
    /// The player who approved the request. Only present when status is approved; /self does not
    /// accept a game token, so this is the only way to learn who just linked their account.
    /// </summary>
    [JsonPropertyName("user")]
    public D2JamUser? User { get; set; } = null;
}
