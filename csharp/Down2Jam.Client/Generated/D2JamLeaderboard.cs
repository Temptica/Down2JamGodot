#nullable enable

using System.Collections.Generic;
using System.Text.Json.Serialization;

// GENERATED FILE - do not edit by hand; your changes will be overwritten.
//
// Source: the Jamcore OpenAPI document plus spec/d2jam.overlay.json.
// Regenerate with: dotnet run --project Down2Plugin

namespace Down2Jam.Client;

/// <summary>
/// A leaderboard attached to a game page. Created by the developer on the Down2Jam website;
/// games submit to it by id.
/// </summary>
public sealed class D2JamLeaderboard
{
    [JsonPropertyName("id")]
    public int Id { get; set; } = 0;

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    /// <summary>
    /// One of SCORE, GOLF, SPEEDRUN or ENDURANCE. See the TYPE_* constants on D2JamLeaderboards.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    /// <summary>
    /// Decimal precision for SCORE and GOLF boards. Ignored for time based boards.
    /// </summary>
    [JsonPropertyName("decimalPlaces")]
    public int DecimalPlaces { get; set; } = 0;

    /// <summary>
    /// How many rows the website shows per page.
    /// </summary>
    [JsonPropertyName("maxUsersShown")]
    public int MaxUsersShown { get; set; } = 0;

    /// <summary>
    /// When true only each user's best entry is ranked.
    /// </summary>
    [JsonPropertyName("onlyBest")]
    public bool OnlyBest { get; set; } = false;

    [JsonPropertyName("gamePageId")]
    public int GamePageId { get; set; } = 0;

    /// <summary>
    /// Every submitted score, unsorted. Use D2JamLeaderboards.rank() to order them.
    /// </summary>
    [JsonPropertyName("scores")]
    public List<D2JamScore> Scores { get; set; } = [];

    [JsonPropertyName("createdAt")]
    public string CreatedAt { get; set; } = "";

    [JsonPropertyName("updatedAt")]
    public string UpdatedAt { get; set; } = "";
}
