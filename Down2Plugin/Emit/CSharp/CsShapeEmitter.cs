using Down2Plugin.Model;

namespace Down2Plugin.Emit.CSharp;

/// <summary>
/// Emits the C# data classes: response models and request bodies.
///
/// Unlike the GDScript emitter these need no hand-rolled `from_json`/`to_dict` -
/// <c>System.Text.Json</c> reads the <c>[JsonPropertyName]</c> attributes directly, so a shape is
/// just a POCO.
/// </summary>
public static class CsShapeEmitter
{
    public static string Emit(ShapeDefinition shape, string csNamespace)
    {
        var buffer = new CsBuffer();

        CsHeader.WriteNullableDirective(buffer);
        buffer.Line("using System.Collections.Generic;");
        buffer.Line("using System.Text.Json.Serialization;");
        buffer.Blank();
        CsHeader.Write(buffer);
        buffer.Blank();
        buffer.Line($"namespace {csNamespace};");
        buffer.Blank();
        buffer.Doc(shape.Doc);

        using (buffer.Block($"public sealed class {shape.ClassName}"))
        {
            var first = true;
            foreach (var field in shape.Fields)
            {
                if (!first)
                {
                    buffer.Blank();
                }

                first = false;
                EmitField(buffer, field, shape.IsBody);
            }
        }

        return buffer.ToString();
    }

    /// <summary>
    /// Emits the query option holder for one operation. Every field is nullable regardless of its
    /// underlying type, because "not provided" (omit the query parameter) must be distinguishable
    /// from "provided as the type's default" (e.g. an explicit 0).
    /// </summary>
    public static string EmitOptions(OperationDefinition operation, string csNamespace)
    {
        var buffer = new CsBuffer();

        CsHeader.WriteNullableDirective(buffer);
        buffer.Line("using System.Collections.Generic;");
        buffer.Blank();
        CsHeader.Write(buffer);
        buffer.Blank();
        buffer.Line($"namespace {csNamespace};");
        buffer.Blank();
        buffer.Doc(
            $"Optional query parameters for {operation.CsName}. Only the properties you set are "
            + "sent, so an untouched instance adds nothing to the request.");

        using (buffer.Block($"public sealed class {operation.OptionsClassName}"))
        {
            var first = true;
            foreach (var parameter in operation.OptionalQuery)
            {
                if (!first)
                {
                    buffer.Blank();
                }

                first = false;
                buffer.Doc(parameter.Doc);
                var propertyName = Naming.ToPascalCase(parameter.JsonName);
                buffer.Line($"public {parameter.CsType}? {propertyName} {{ get; set; }}");
            }
        }

        return buffer.ToString();
    }

    private static void EmitField(CsBuffer buffer, FieldDefinition field, bool isBody)
    {
        buffer.Doc(field.Doc);
        buffer.Line($"[JsonPropertyName(\"{field.JsonName}\")]");

        // A body sends only the fields the caller actually set - to_dict() on the GDScript side,
        // "required" plus JsonIgnore-on-null here. A required field must be supplied and is
        // always sent; an optional one is nullable, and null (unset) means "leave it out" rather
        // than "send the type's default", which is not the same thing to the server (an unset
        // evidence URL is not the same as one explicitly sent as "").
        if (isBody && !field.Required)
        {
            buffer.Line("[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]");
            buffer.Line($"public {field.Type.CsType}? {field.CsName} {{ get; set; }}");
            return;
        }

        if (isBody && field.Required)
        {
            buffer.Line($"public required {field.Type.CsType} {field.CsName} {{ get; set; }}");
            return;
        }

        var type = field.Type.NeedsCsNullable ? field.Type.CsType + "?" : field.Type.CsType;
        var defaultValue = field.Type.NeedsCsNullable ? "null" : field.Type.CsDefaultValue;

        buffer.Line($"public {type} {field.CsName} {{ get; set; }} = {defaultValue};");
    }
}
