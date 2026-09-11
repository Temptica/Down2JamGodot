#nullable enable

using System.Collections.Generic;
using System.Text.Json.Serialization;

// GENERATED FILE - do not edit by hand; your changes will be overwritten.
//
// Source: the Jamcore OpenAPI document plus spec/d2jam.overlay.json.
// Regenerate with: dotnet run --project Down2Plugin

namespace Down2Jam.Client;

/// <summary>
/// A Down2Jam user account.
/// </summary>
public sealed class D2JamUser
{
    [JsonPropertyName("id")]
    public int Id { get; set; } = 0;

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    /// <summary>
    /// Absolute URL to the avatar image.
    /// </summary>
    [JsonPropertyName("profilePicture")]
    public string ProfilePicture { get; set; } = "";

    [JsonPropertyName("bannerPicture")]
    public string BannerPicture { get; set; } = "";

    /// <summary>
    /// One line profile tagline.
    /// </summary>
    [JsonPropertyName("short")]
    public string Short { get; set; } = "";

    /// <summary>
    /// Profile biography, may contain HTML.
    /// </summary>
    [JsonPropertyName("bio")]
    public string Bio { get; set; } = "";

    [JsonPropertyName("pronouns")]
    public string Pronouns { get; set; } = "";

    [JsonPropertyName("links")]
    public List<string> Links { get; set; } = [];

    [JsonPropertyName("linkLabels")]
    public List<string> LinkLabels { get; set; } = [];

    /// <summary>
    /// Twitch login name, if the user linked one.
    /// </summary>
    [JsonPropertyName("twitch")]
    public string Twitch { get; set; } = "";

    [JsonPropertyName("emotePrefix")]
    public string EmotePrefix { get; set; } = "";

    [JsonPropertyName("mod")]
    public bool Mod { get; set; } = false;

    [JsonPropertyName("admin")]
    public bool Admin { get; set; } = false;

    /// <summary>
    /// ISO 8601 timestamp.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public string CreatedAt { get; set; } = "";

    /// <summary>
    /// ISO 8601 timestamp.
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public string UpdatedAt { get; set; } = "";
}
