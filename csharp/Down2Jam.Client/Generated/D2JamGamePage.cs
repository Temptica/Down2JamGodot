#nullable enable

using System.Collections.Generic;
using System.Text.Json.Serialization;

// GENERATED FILE - do not edit by hand; your changes will be overwritten.
//
// Source: the Jamcore OpenAPI document plus spec/d2jam.overlay.json.
// Regenerate with: dotnet run --project Down2Plugin

namespace Down2Jam.Client;

/// <summary>
/// One version of a game's page. A game has a JAM page and optionally a POST_JAM page.
/// </summary>
public sealed class D2JamGamePage
{
    [JsonPropertyName("id")]
    public int Id { get; set; } = 0;

    /// <summary>
    /// JAM or POST_JAM.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("short")]
    public string Short { get; set; } = "";

    [JsonPropertyName("thumbnail")]
    public string Thumbnail { get; set; } = "";

    [JsonPropertyName("banner")]
    public string Banner { get; set; } = "";

    [JsonPropertyName("screenshots")]
    public List<string> Screenshots { get; set; } = [];

    [JsonPropertyName("trailerUrl")]
    public string TrailerUrl { get; set; } = "";

    [JsonPropertyName("emotePrefix")]
    public string EmotePrefix { get; set; } = "";

    [JsonPropertyName("gameId")]
    public int GameId { get; set; } = 0;

    [JsonPropertyName("downloadLinks")]
    public List<D2JamDownloadLink> DownloadLinks { get; set; } = [];
}
