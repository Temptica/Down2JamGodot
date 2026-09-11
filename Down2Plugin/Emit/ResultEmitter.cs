using Down2Plugin.Model;

namespace Down2Plugin.Emit;

/// <summary>Emits the typed result wrapper each generated method returns.</summary>
public static class ResultEmitter
{
    public static string Emit(ResultType result)
    {
        var buffer = new GdBuffer();

        buffer.Line("@tool");
        buffer.Line("extends D2JamResult");
        buffer.Blank();
        Header.Write(buffer);
        buffer.Blank();

        if (result.IsVoid)
        {
            buffer.Doc("Result of a call that returns no payload.");
            buffer.Doc("");
            buffer.Doc("Check [member D2JamResult.ok]; on failure [member D2JamResult.error_message] says why.");
            buffer.Line($"class_name {result.ClassName}");
            return buffer.ToString();
        }

        var payload = result.Payload!;

        buffer.Doc($"Result carrying {(payload.IsArray ? "an array of " : "a ")}[{payload.ElementGdType}].");
        buffer.Line($"class_name {result.ClassName}");
        buffer.Blank();

        if (payload.IsArray)
        {
            buffer.Doc("The parsed payload. Empty when the request failed.");
            buffer.Line($"var data: {payload.GdType} = []");
        }
        else if (payload.ElementIsShape)
        {
            buffer.Doc("The parsed payload, or null when the request failed.");
            buffer.Line($"var data: {payload.ElementGdType}");
        }
        else
        {
            buffer.Doc("The parsed payload. Empty when the request failed.");
            buffer.Line($"var data: {payload.ElementGdType} = {payload.DefaultValue}");
        }

        return buffer.ToString();
    }
}
