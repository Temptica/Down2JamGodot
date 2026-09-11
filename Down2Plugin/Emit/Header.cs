namespace Down2Plugin.Emit;

/// <summary>The banner every generated file carries.</summary>
public static class Header
{
    public const string Text =
        """
        # GENERATED FILE - do not edit by hand; your changes will be overwritten.
        #
        # Source: the Jamcore OpenAPI document plus spec/d2jam.overlay.json.
        # Regenerate with: dotnet run --project Down2Plugin
        """;

    public static void Write(GdBuffer buffer)
    {
        foreach (var line in Text.Split('\n'))
        {
            buffer.Line(line);
        }
    }
}
