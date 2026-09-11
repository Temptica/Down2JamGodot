#nullable enable

using System.Collections.Generic;

// GENERATED FILE - do not edit by hand; your changes will be overwritten.
//
// Source: the Jamcore OpenAPI document plus spec/d2jam.overlay.json.
// Regenerate with: dotnet run --project Down2Plugin

namespace Down2Jam.Client;

/// <summary>
/// Optional query parameters for GetGameAsync. Only the properties you set are sent, so an
/// untouched instance adds nothing to the request.
/// </summary>
public sealed class D2JamGetGameOptions
{
    public bool? Recap { get; set; }

    public bool? Preview { get; set; }
}
