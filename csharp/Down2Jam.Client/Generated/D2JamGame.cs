#nullable enable

using System.Collections.Generic;
using System.Text.Json.Serialization;

// GENERATED FILE - do not edit by hand; your changes will be overwritten.
//
// Source: the Jamcore OpenAPI document plus spec/d2jam.overlay.json.
// Regenerate with: dotnet run --project Down2Plugin

namespace Down2Jam.Client;

/// <summary>
/// A game entry. GET /games/{gameSlug} is the only endpoint that returns leaderboards and
/// achievements, so it is also the read path for both.
/// </summary>
public sealed class D2JamGame
{
    [JsonPropertyName("id")]
    public int Id { get; set; } = 0;

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = "";

    /// <summary>
    /// Only populated by the list endpoints, which flatten it from the jam page. GET
    /// /games/{gameSlug} leaves it empty; read jam_page.name there, or call D2JamService.page().
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    /// <summary>
    /// List endpoints only, like name.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    /// <summary>
    /// List endpoints only, like name.
    /// </summary>
    [JsonPropertyName("short")]
    public string Short { get; set; } = "";

    /// <summary>
    /// List endpoints only, like name.
    /// </summary>
    [JsonPropertyName("thumbnail")]
    public string Thumbnail { get; set; } = "";

    /// <summary>
    /// REGULAR, ODA or EXTERNAL.
    /// </summary>
    [JsonPropertyName("category")]
    public string Category { get; set; } = "";

    [JsonPropertyName("published")]
    public bool Published { get; set; } = false;

    [JsonPropertyName("publishedAt")]
    public string PublishedAt { get; set; } = "";

    [JsonPropertyName("sourceUrl")]
    public string SourceUrl { get; set; } = "";

    [JsonPropertyName("sourcePlatform")]
    public string SourcePlatform { get; set; } = "";

    [JsonPropertyName("teamId")]
    public int TeamId { get; set; } = 0;

    [JsonPropertyName("jamId")]
    public int JamId { get; set; } = 0;

    [JsonPropertyName("jam")]
    public D2JamJam? Jam { get; set; } = null;

    [JsonPropertyName("pages")]
    public List<D2JamGamePage> Pages { get; set; } = [];

    /// <summary>
    /// The page as submitted to the jam. Only populated by GET /games/{gameSlug}, and the place its
    /// name, description and artwork actually live.
    /// </summary>
    [JsonPropertyName("jamPage")]
    public D2JamGamePage? JamPage { get; set; } = null;

    /// <summary>
    /// The post-jam page, when the team published one. Only populated by GET /games/{gameSlug}.
    /// </summary>
    [JsonPropertyName("postJamPage")]
    public D2JamGamePage? PostJamPage { get; set; } = null;

    [JsonPropertyName("downloadLinks")]
    public List<D2JamDownloadLink> DownloadLinks { get; set; } = [];

    /// <summary>
    /// Only populated by GET /games/{gameSlug}.
    /// </summary>
    [JsonPropertyName("leaderboards")]
    public List<D2JamLeaderboard> Leaderboards { get; set; } = [];

    /// <summary>
    /// Only populated by GET /games/{gameSlug}.
    /// </summary>
    [JsonPropertyName("achievements")]
    public List<D2JamAchievement> Achievements { get; set; } = [];

    [JsonPropertyName("createdAt")]
    public string CreatedAt { get; set; } = "";

    [JsonPropertyName("updatedAt")]
    public string UpdatedAt { get; set; } = "";
}
