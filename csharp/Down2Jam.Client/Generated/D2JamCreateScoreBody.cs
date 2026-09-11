#nullable enable

using System.Collections.Generic;
using System.Text.Json.Serialization;

// GENERATED FILE - do not edit by hand; your changes will be overwritten.
//
// Source: the Jamcore OpenAPI document plus spec/d2jam.overlay.json.
// Regenerate with: dotnet run --project Down2Plugin

namespace Down2Jam.Client;

/// <summary>
/// Payload for POST /score.
/// </summary>
public sealed class D2JamCreateScoreBody
{
    /// <summary>
    /// Id of the leaderboard on the game page.
    /// </summary>
    [JsonPropertyName("leaderboardId")]
    public required int LeaderboardId { get; set; }

    /// <summary>
    /// A number. For SCORE and GOLF boards, the value as a player would read it; the server
    /// multiplies it by 10 ^ decimal_places. For SPEEDRUN and ENDURANCE boards, a duration in whole
    /// milliseconds. Send an int unless the board declares decimal places, because the column it
    /// lands in is an integer. D2JamLeaderboards.submit() picks the right form for you.
    /// </summary>
    [JsonPropertyName("score")]
    public required object Score { get; set; }

    /// <summary>
    /// URL of a screenshot backing the score. The website requires one on every submission, so
    /// treat it as effectively mandatory.
    /// </summary>
    [JsonPropertyName("evidence")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Evidence { get; set; }

    /// <summary>
    /// Accepted as an alias for evidence by the server. Prefer evidence.
    /// </summary>
    [JsonPropertyName("evidenceUrl")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? EvidenceUrl { get; set; }
}
