#nullable enable

using System.Collections.Generic;
using System.Text.Json.Serialization;

// GENERATED FILE - do not edit by hand; your changes will be overwritten.
//
// Source: the Jamcore OpenAPI document plus spec/d2jam.overlay.json.
// Regenerate with: dotnet run --project Down2Plugin

namespace Down2Jam.Client;

/// <summary>
/// A single leaderboard entry.
/// </summary>
public sealed class D2JamScore
{
    [JsonPropertyName("id")]
    public int Id { get; set; } = 0;

    /// <summary>
    /// The stored raw value. For SCORE and GOLF boards this is the submitted value multiplied by 10
    /// ^ decimal_places; for SPEEDRUN and ENDURANCE boards it is a duration in milliseconds. Use
    /// D2JamLeaderboards.score_to_display() rather than reading this directly.
    /// </summary>
    [JsonPropertyName("data")]
    public int Data { get; set; } = 0;

    /// <summary>
    /// URL of the screenshot backing this score. May be empty.
    /// </summary>
    [JsonPropertyName("evidence")]
    public string Evidence { get; set; } = "";

    [JsonPropertyName("userId")]
    public int UserId { get; set; } = 0;

    [JsonPropertyName("leaderboardId")]
    public int LeaderboardId { get; set; } = 0;

    [JsonPropertyName("user")]
    public D2JamUser? User { get; set; } = null;

    [JsonPropertyName("createdAt")]
    public string CreatedAt { get; set; } = "";

    [JsonPropertyName("updatedAt")]
    public string UpdatedAt { get; set; } = "";
}
