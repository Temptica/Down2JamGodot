#nullable enable

using System.Collections.Generic;
using System.Text.Json.Serialization;

// GENERATED FILE - do not edit by hand; your changes will be overwritten.
//
// Source: the Jamcore OpenAPI document plus spec/d2jam.overlay.json.
// Regenerate with: dotnet run --project Down2Plugin

namespace Down2Jam.Client;

/// <summary>
/// A Down2Jam jam event.
/// </summary>
public sealed class D2JamJam
{
    [JsonPropertyName("id")]
    public int Id { get; set; } = 0;

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = "";

    /// <summary>
    /// ISO 8601 timestamp for the start of the jamming phase.
    /// </summary>
    [JsonPropertyName("startTime")]
    public string StartTime { get; set; } = "";

    [JsonPropertyName("jammingHours")]
    public int JammingHours { get; set; } = 0;

    [JsonPropertyName("submissionHours")]
    public int SubmissionHours { get; set; } = 0;

    [JsonPropertyName("ratingHours")]
    public int RatingHours { get; set; } = 0;

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = false;

    [JsonPropertyName("icon")]
    public string Icon { get; set; } = "";

    [JsonPropertyName("color")]
    public string Color { get; set; } = "";
}
