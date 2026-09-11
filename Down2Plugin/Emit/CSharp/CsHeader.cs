namespace Down2Plugin.Emit.CSharp;

/// <summary>The banner every generated C# file carries.</summary>
public static class CsHeader
{
    public const string Text =
        """
        // GENERATED FILE - do not edit by hand; your changes will be overwritten.
        //
        // Source: the Jamcore OpenAPI document plus spec/d2jam.overlay.json.
        // Regenerate with: dotnet run --project Down2Plugin
        """;

    /// <summary>
    /// Roslyn treats "*.g.cs" (and anything marked &lt;auto-generated/&gt;) as generated code and
    /// defaults its nullable context to oblivious regardless of the project's Nullable setting,
    /// unless the file says otherwise itself. Must be the first thing in the file.
    /// </summary>
    public static void WriteNullableDirective(CsBuffer buffer)
    {
        buffer.Line("#nullable enable");
        buffer.Blank();
    }

    public static void Write(CsBuffer buffer)
    {
        foreach (var line in Text.Split('\n'))
        {
            buffer.Line(line);
        }
    }
}
