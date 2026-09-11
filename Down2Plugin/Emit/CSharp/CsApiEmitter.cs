using Down2Plugin.Model;

namespace Down2Plugin.Emit.CSharp;

/// <summary>
/// Emits D2JamApi.g.cs: one method per covered endpoint, over a hand written transport base.
///
/// This assumes two hand written types that do not exist yet (the C# equivalent of the GDScript
/// addon's hand written `lib/` layer, ported separately from this generator change):
///
///   - `D2JamApiBase`, an abstract class providing:
///       Task&lt;D2JamResponse&gt; RequestAsync(string path, HttpMethod method,
///           IReadOnlyDictionary&lt;string, object?&gt;? query, object? jsonBody, bool requiresAuth)
///       Task&lt;D2JamResponse&gt; RequestMultipartAsync(string path, string fileField,
///           byte[] fileBytes, string fileName, bool requiresAuth)
///       string ResolveUrl(string path)
///
///   - `D2JamResult`/`D2JamResult&lt;T&gt;`, each with a static `From` that reads a `D2JamResponse`
///     and (for the generic form) a `Func&lt;JsonElement, T&gt;` to parse the `data` payload.
///
/// C# has real generics, so unlike the GDScript client there is no per-operation result class -
/// `D2JamResult&lt;T&gt;` covers every payload shape, generated or primitive.
/// </summary>
public static class CsApiEmitter
{
    public static string Emit(ApiModel model, string csNamespace)
    {
        var buffer = new CsBuffer();

        CsHeader.WriteNullableDirective(buffer);
        buffer.Line("using System;");
        buffer.Line("using System.Collections.Generic;");
        buffer.Line("using System.Net.Http;");
        buffer.Line("using System.Text.Json;");
        buffer.Line("using System.Threading.Tasks;");
        buffer.Blank();
        CsHeader.Write(buffer);
        buffer.Blank();
        buffer.Line($"namespace {csNamespace};");
        buffer.Blank();
        buffer.Doc(
            $"Typed client for the {model.SpecTitle} ({model.SpecVersion}). Every method is "
            + "asynchronous. Transport, authentication and error handling live in the hand "
            + "written D2JamApiBase, so this class only describes endpoints.");

        using (buffer.Block($"public sealed partial class {model.ClassPrefix}Api : {model.ClassPrefix}ApiBase"))
        {
            var first = true;
            foreach (var operation in model.Operations)
            {
                if (!first)
                {
                    buffer.Blank();
                }

                first = false;
                EmitOperation(buffer, operation);
            }
        }

        return buffer.ToString();
    }

    private static void EmitOperation(CsBuffer buffer, OperationDefinition operation)
    {
        EmitDoc(buffer, operation);
        var returnType = operation.Result.IsVoid
            ? "D2JamResult"
            : $"D2JamResult<{operation.Result.Payload!.CsType}>";

        using (buffer.Block($"public async Task<{returnType}> {operation.CsName}({BuildSignature(operation)})"))
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

    private static void EmitDoc(CsBuffer buffer, OperationDefinition operation)
    {
        var lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(operation.Doc))
        {
            lines.Add(operation.Doc!);
            lines.Add("");
        }

        lines.Add($"{operation.HttpMethod.ToUpperInvariant()} {operation.Path} - {operation.AuthLabel}");
        buffer.Doc(string.Join('\n', lines));
    }

    private static string BuildSignature(OperationDefinition operation)
    {
        var parts = new List<string>();

        if (operation.MultipartFileField is not null)
        {
            parts.Add("byte[] fileBytes");
            parts.Add("string fileName");
        }

        parts.AddRange(operation.PathParams.Select(p => $"{p.CsType} {p.CsName}"));
        parts.AddRange(operation.RequiredQuery.Select(p => $"{p.CsType} {p.CsName}"));

        if (operation.Body is not null)
        {
            parts.Add($"{operation.Body.ClassName} body");
        }

        if (operation.OptionsClassName is not null)
        {
            parts.Add($"{operation.OptionsClassName}? options = null");
        }

        return string.Join(", ", parts);
    }

    private static void EmitStandardBody(CsBuffer buffer, OperationDefinition operation)
    {
        buffer.Line($"var path = {BuildPathExpression(operation)};");

        var hasQuery = operation.RequiredQuery.Count > 0 || operation.OptionsClassName is not null;
        if (hasQuery)
        {
            buffer.Line("var query = new Dictionary<string, object?>();");

            foreach (var parameter in operation.RequiredQuery)
            {
                buffer.Line($"query[\"{parameter.JsonName}\"] = {parameter.CsName};");
            }

            if (operation.OptionsClassName is not null)
            {
                using (buffer.Block("if (options != null)"))
                {
                    foreach (var parameter in operation.OptionalQuery)
                    {
                        var propertyName = Naming.ToPascalCase(parameter.JsonName);
                        buffer.Line($"if (options.{propertyName} != null) query[\"{parameter.JsonName}\"] = options.{propertyName};");
                    }
                }
            }
        }

        var queryArgument = hasQuery ? "query" : "null";
        var bodyArgument = operation.Body is not null ? "body" : "null";
        var authArgument = operation.RequiresAuth ? "true" : "false";

        buffer.Line(
            $"var response = await RequestAsync(path, {operation.CsHttpMethod}, "
            + $"{queryArgument}, {bodyArgument}, {authArgument});");
    }

    private static void EmitMultipartBody(CsBuffer buffer, OperationDefinition operation)
    {
        buffer.Line($"var path = {BuildPathExpression(operation)};");
        buffer.Line(
            $"var response = await RequestMultipartAsync(path, "
            + $"\"{operation.MultipartFileField}\", fileBytes, fileName, "
            + $"{(operation.RequiresAuth ? "true" : "false")});");
    }

    private static void EmitResultHandling(CsBuffer buffer, OperationDefinition operation)
    {
        if (operation.Result.IsVoid)
        {
            buffer.Line("return D2JamResult.From(response);");
            return;
        }

        var payload = operation.Result.Payload!;
        buffer.Line($"return D2JamResult<{payload.CsType}>.From(response, {BuildParseExpression(operation, payload)});");
    }

    private static string BuildParseExpression(OperationDefinition operation, TypeReference payload)
    {
        if (payload.IsArray && payload.ElementIsShape)
        {
            return $"payload => JsonSerializer.Deserialize<List<{payload.ElementCsType}>>(payload, D2JamJson.Options) ?? []";
        }

        if (payload.IsArray)
        {
            return $"payload => JsonSerializer.Deserialize<List<{payload.ElementCsType}>>(payload, D2JamJson.Options) ?? []";
        }

        if (payload.ElementIsShape)
        {
            return $"payload => JsonSerializer.Deserialize<{payload.ElementCsType}>(payload, D2JamJson.Options)!";
        }

        if (operation.ResolveUrl)
        {
            return "payload => ResolveUrl(payload.GetString() ?? \"\")";
        }

        return payload.ElementCsType switch
        {
            "int" => "payload => payload.GetInt32()",
            "double" => "payload => payload.GetDouble()",
            "bool" => "payload => payload.GetBoolean()",
            "string" => "payload => payload.GetString() ?? \"\"",
            _ => "payload => (object)payload.Clone()",
        };
    }

    /// <summary>Turns "/games/{gameSlug}" into a C# interpolated string literal.</summary>
    private static string BuildPathExpression(OperationDefinition operation)
    {
        if (operation.PathParams.Count == 0)
        {
            return $"\"{operation.Path}\"";
        }

        var template = operation.Path;

        foreach (var parameter in operation.PathParams)
        {
            var placeholder = "{" + parameter.JsonName + "}";
            template = template.Replace(
                placeholder,
                $"{{Uri.EscapeDataString({parameter.CsName}.ToString()!)}}");
        }

        return $"$\"{template}\"";
    }
}
