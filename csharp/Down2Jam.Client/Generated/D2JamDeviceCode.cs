#nullable enable

using System.Collections.Generic;
using System.Text.Json.Serialization;

// GENERATED FILE - do not edit by hand; your changes will be overwritten.
//
// Source: the Jamcore OpenAPI document plus spec/d2jam.overlay.json.
// Regenerate with: dotnet run --project Down2Plugin

namespace Down2Jam.Client;

/// <summary>
/// The result of starting a device authorization request. Show user_code to the player and open
/// verification_uri in their browser, then poll poll_device_link() with device_code until it
/// resolves. Prefer D2JamAuth.start_device_link(), which drives the whole thing.
/// </summary>
public sealed class D2JamDeviceCode
{
    /// <summary>
    /// Secret. Poll poll_device_link() with this until the player approves or denies the request.
    /// </summary>
    [JsonPropertyName("deviceCode")]
    public string DeviceCode { get; set; } = "";

    /// <summary>
    /// Short code to show the player, e.g. ABCD-1234. Already embedded in verification_uri.
    /// </summary>
    [JsonPropertyName("userCode")]
    public string UserCode { get; set; } = "";

    /// <summary>
    /// Absolute URL to open in the player's browser.
    /// </summary>
    [JsonPropertyName("verificationUri")]
    public string VerificationUri { get; set; } = "";

    /// <summary>
    /// Seconds until the request expires unapproved.
    /// </summary>
    [JsonPropertyName("expiresIn")]
    public int ExpiresIn { get; set; } = 0;

    /// <summary>
    /// Minimum seconds to wait between polls.
    /// </summary>
    [JsonPropertyName("interval")]
    public int Interval { get; set; } = 0;
}
