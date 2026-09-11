using System.Text;
using Down2Plugin.Model;

namespace Down2Plugin.Emit;

/// <summary>Emits d2jam_api.gd: one method per covered endpoint, over the hand written transport base.</summary>
public static class ApiEmitter
{
    public static string Emit(ApiModel model)
    {
        var buffer = new GdBuffer();

        buffer.Line("@tool");
        buffer.Line("extends D2JamAPIBase");
        buffer.Blank();
        Header.Write(buffer);
        buffer.Blank();
        buffer.Doc($"Typed client for the {model.SpecTitle} ({model.SpecVersion}).");
        buffer.DocBlank();
        buffer.Doc(
            "Every method is asynchronous; await the call and inspect the returned result. "
            + "Transport, authentication and error handling live in the hand written "
            + "[D2JamAPIBase], so this file only describes endpoints.");
        buffer.DocBlank();
        buffer.Doc(
            "For leaderboards and achievements prefer the [D2JamLeaderboards] and "
            + "[D2JamAchievements] nodes, which handle value conversion, evidence uploads and "
            + "name based lookup on top of these calls.");
        buffer.Doc(
            "The client of the most recently added [D2JamAPI] node, so code that only needs the raw "
            + "API can reach it without a reference. [D2JamService] adds one as a child of itself.");
        buffer.Line("class_name D2JamAPI");
        buffer.Blank();
        EmitSingleton(buffer);

        foreach (var operation in model.Operations)
        {
            buffer.Blank(2);
            EmitOperation(buffer, operation);
        }

        return buffer.ToString();
    }

    /// <summary>
    /// The singleton accessor, matching the rest of the addon: the node registers itself as it
    /// enters the tree and stands down as it leaves, so there is no autoload to configure.
    /// </summary>
    private static void EmitSingleton(GdBuffer buffer)
    {
        buffer.Doc("The API client in the current scene, or null when none is in the tree.");
        buffer.Line("static var instance: D2JamAPI");
        buffer.Blank(2);
        // No super() calls: GDScript rejects calling a parent virtual the parent script does not
        // itself define, and D2JamAPIBase does not define these.
        buffer.Line("func _enter_tree() -> void:");
        using (buffer.Indented())
        {
            buffer.Line("if instance == null:");
            using (buffer.Indented())
            {
                buffer.Line("instance = self");
            }
        }

        buffer.Blank(2);
        buffer.Line("func _exit_tree() -> void:");
        using (buffer.Indented())
        {
            buffer.Line("if instance == self:");
            using (buffer.Indented())
            {
                buffer.Line("instance = null");
            }
        }
    }


    private static void EmitOperation(GdBuffer buffer, OperationDefinition operation)
    {
        EmitDoc(buffer, operation);
        buffer.Line($"func {operation.Name}({BuildSignature(operation)}) -> {operation.Result.ClassName}:");

        using (buffer.Indented())
        {
            if (operation.MultipartFileField is not null)
            {
                EmitMultipartBody(buffer, operation);
            }
            else
            {
                EmitStandardBody(buffer, operation);
            }

            EmitResultHandling(buffer, operation);
        }
    }

    private static void EmitDoc(GdBuffer buffer, OperationDefinition operation)
    {
        buffer.Doc(operation.Doc);

        var parameterDocs = CollectParameterDocs(operation);
        if (parameterDocs.Count > 0)
        {
            buffer.DocBlank();
            foreach (var line in parameterDocs)
            {
                buffer.Doc(line);
            }
        }

        buffer.DocBlank();
        buffer.Doc($"{operation.HttpMethod.ToUpperInvariant()} {operation.Path} - {operation.AuthLabel}");
    }

    private static List<string> CollectParameterDocs(OperationDefinition operation)
    {
        var lines = new List<string>();

        if (operation.MultipartFileField is not null)
        {
            lines.Add("file_bytes - Raw image data, for example from Image.save_png_to_buffer().");
            lines.Add("file_name - File name to send, including an extension the server accepts.");
        }

        lines.AddRange(operation.PathParams
            .Select(parameter => $"{parameter.GdName} - {Describe(parameter, "Path parameter.")}"));

        lines.AddRange(operation.RequiredQuery
            .Select(parameter => $"{parameter.GdName} - {Describe(parameter, "Required query parameter.")}"));

        if (operation.Body is not null)
        {
            lines.Add($"body - Request payload; see [{operation.Body.ClassName}].");
        }

        if (operation.OptionsClassName is not null)
        {
            lines.Add($"options - Optional query parameters; see [{operation.OptionsClassName}]. May be null.");
        }

        return lines;
    }

    private static string Describe(ParameterDefinition parameter, string fallback) =>
        string.IsNullOrWhiteSpace(parameter.Doc) ? fallback : parameter.Doc!.ReplaceLineEndings(" ");

    private static string BuildSignature(OperationDefinition operation)
    {
        var parts = new List<string>();

        if (operation.MultipartFileField is not null)
        {
            parts.Add("file_bytes: PackedByteArray");
            parts.Add("file_name: String");
        }

        parts.AddRange(operation.PathParams.Select(p => $"{p.GdName}: {p.GdType}"));
        parts.AddRange(operation.RequiredQuery.Select(p => $"{p.GdName}: {p.GdType}"));

        if (operation.Body is not null)
        {
            parts.Add($"body: {operation.Body.ClassName}");
        }

        if (operation.OptionsClassName is not null)
        {
            parts.Add($"options: {operation.OptionsClassName} = null");
        }

        return string.Join(", ", parts);
    }

    private static void EmitStandardBody(GdBuffer buffer, OperationDefinition operation)
    {
        buffer.Line($"var path: String = {BuildPathExpression(operation)}");

        if (operation.RequiredQuery.Count > 0 || operation.OptionsClassName is not null)
        {
            buffer.Line("var query: Dictionary = {}");

            foreach (var parameter in operation.RequiredQuery)
            {
                buffer.Line($"query[\"{parameter.JsonName}\"] = {parameter.GdName}");
            }

            if (operation.OptionsClassName is not null)
            {
                buffer.Line("if options != null:");
                using (buffer.Indented())
                {
                    buffer.Line("query.merge(options.to_dict(), true)");
                }
            }
        }

        var queryArgument = operation.RequiredQuery.Count > 0 || operation.OptionsClassName is not null
            ? "query"
            : "{}";
        var bodyArgument = operation.Body is not null ? "body.to_dict()" : "null";
        var authArgument = operation.RequiresAuth ? "true" : "false";

        buffer.Line(
            $"var response: D2JamResponse = await request(path, {operation.GdHttpMethod}, "
            + $"{queryArgument}, {bodyArgument}, {authArgument})");
    }

    private static void EmitMultipartBody(GdBuffer buffer, OperationDefinition operation)
    {
        buffer.Line($"var path: String = {BuildPathExpression(operation)}");
        buffer.Line(
            $"var response: D2JamResponse = await request_multipart(path, "
            + $"\"{operation.MultipartFileField}\", file_bytes, file_name, "
            + $"{(operation.RequiresAuth ? "true" : "false")})");
    }

    private static void EmitResultHandling(GdBuffer buffer, OperationDefinition operation)
    {
        var result = operation.Result;
        buffer.Line($"var result: {result.ClassName} = {result.ClassName}.new()");
        buffer.Line("result._apply(response)");

        if (result.IsVoid)
        {
            buffer.Line("return result");
            return;
        }

        var payload = result.Payload!;
        buffer.Line("if result.ok:");

        using (buffer.Indented())
        {
            if (payload.IsArray && payload.ElementIsShape)
            {
                buffer.Line($"result.data = {payload.ElementGdType}.list_from_json(result.payload)");
            }
            else if (payload.ElementIsShape)
            {
                buffer.Line($"result.data = {payload.ElementGdType}.from_json(result.payload)");
            }
            else if (operation.ResolveUrl)
            {
                buffer.Line("result.data = resolve_url(str(result.payload))");
            }
            else
            {
                var cast = payload.ElementGdType switch
                {
                    "int" => "int(result.payload)",
                    "float" => "float(result.payload)",
                    "bool" => "bool(result.payload)",
                    "String" => "str(result.payload)",
                    _ => "result.payload",
                };

                buffer.Line($"result.data = {cast}");
            }
        }

        buffer.Line("return result");
    }

    /// <summary>Turns "/games/{gameSlug}" into a literal, or a format expression when it has parameters.</summary>
    private static string BuildPathExpression(OperationDefinition operation)
    {
        if (operation.PathParams.Count == 0)
        {
            return $"\"{operation.Path}\"";
        }

        var template = new StringBuilder(operation.Path);
        var arguments = new List<string>();

        // Arguments must be listed in the order their placeholders appear in the path,
        // which is not necessarily the order the spec lists the parameters in.
        var ordered = operation.PathParams
            .Select(parameter => (parameter, index: operation.Path.IndexOf(
                "{" + parameter.JsonName + "}", StringComparison.Ordinal)))
            .Where(entry => entry.index >= 0)
            .OrderBy(entry => entry.index);

        foreach (var (parameter, _) in ordered)
        {
            var placeholder = "{" + parameter.JsonName + "}";
            var index = template.ToString().IndexOf(placeholder, StringComparison.Ordinal);
            template.Remove(index, placeholder.Length).Insert(index, "%s");
            arguments.Add($"encode_path_segment(str({parameter.GdName}))");
        }

        return $"\"{template}\" % [{string.Join(", ", arguments)}]";
    }
}
