#nullable enable

using System.Collections.Generic;
using System.Text.Json.Serialization;

// GENERATED FILE - do not edit by hand; your changes will be overwritten.
//
// Source: the Jamcore OpenAPI document plus spec/d2jam.overlay.json.
// Regenerate with: dotnet run --project Down2Plugin

namespace Down2Jam.Client;

/// <summary>
/// Payload for POST and DELETE /achievement.
/// </summary>
public sealed class D2JamAchievementBody
{
    /// <summary>
    /// Id of the achievement on the game page.
    /// </summary>
    [JsonPropertyName("achievementId")]
    public required int AchievementId { get; set; }
}
