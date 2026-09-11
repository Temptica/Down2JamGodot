#nullable enable

using System.Collections.Generic;
using System.Text.Json.Serialization;

// GENERATED FILE - do not edit by hand; your changes will be overwritten.
//
// Source: the Jamcore OpenAPI document plus spec/d2jam.overlay.json.
// Regenerate with: dotnet run --project Down2Plugin

namespace Down2Jam.Client;

/// <summary>
/// An achievement defined on a game page. Created by the developer on the Down2Jam website;
/// games unlock it by id.
/// </summary>
public sealed class D2JamAchievement
{
    [JsonPropertyName("id")]
    public int Id { get; set; } = 0;

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    /// <summary>
    /// Absolute URL to the achievement icon.
    /// </summary>
    [JsonPropertyName("image")]
    public string Image { get; set; } = "";

    [JsonPropertyName("gamePageId")]
    public int GamePageId { get; set; } = 0;

    /// <summary>
    /// Everyone who has unlocked this achievement.
    /// </summary>
    [JsonPropertyName("users")]
    public List<D2JamUser> Users { get; set; } = [];

    [JsonPropertyName("createdAt")]
    public string CreatedAt { get; set; } = "";

    [JsonPropertyName("updatedAt")]
    public string UpdatedAt { get; set; } = "";
}
