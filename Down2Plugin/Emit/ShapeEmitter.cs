using Down2Plugin.Model;

namespace Down2Plugin.Emit;

/// <summary>Emits the data classes: response models, request bodies, and query option holders.</summary>
public static class ShapeEmitter
{
    public static string Emit(ShapeDefinition shape)
    {
        var buffer = new GdBuffer();

        buffer.Line("@tool");
        buffer.Line("extends D2JamData");
        buffer.Blank();
        Header.Write(buffer);
        buffer.Blank();
        buffer.Doc(shape.Doc);
        buffer.Line($"class_name {shape.ClassName}");
        buffer.Blank();

        foreach (var field in shape.Fields)
        {
            EmitField(buffer, field);
        }

        buffer.Blank();
        EmitFromJson(buffer, shape);

        if (shape.IsBody)
        {
            var required = shape.RequiredFields.ToArray();
            if (required.Length > 0)
            {
                buffer.Blank(2);
                EmitCreate(buffer, shape, required);
            }
        }
        else
        {
            buffer.Blank(2);
            EmitListFromJson(buffer, shape);
        }

        return buffer.ToString();
    }

    /// <summary>Emits the query option holder for one operation.</summary>
    public static string EmitOptions(OperationDefinition operation)
    {
        var buffer = new GdBuffer();

        buffer.Line("@tool");
        buffer.Line("extends D2JamData");
        buffer.Blank();
        Header.Write(buffer);
        buffer.Blank();
        buffer.Doc($"Optional query parameters for [method D2JamAPI.{operation.Name}].");
        buffer.DocBlank();
        buffer.Doc("Only the properties you assign are sent, so an untouched instance adds nothing to the request.");
        buffer.Line($"class_name {operation.OptionsClassName}");
        buffer.Blank();

        foreach (var parameter in operation.OptionalQuery)
        {
            var field = new FieldDefinition
            {
                JsonName = parameter.JsonName,
                GdName = parameter.GdName,
                Type = new TypeReference
                {
                    Raw = parameter.GdType,
                    IsArray = false,
                    ElementGdType = parameter.GdType,
                    ElementIsShape = false,
                },
                Doc = parameter.Doc,
                Required = false,
            };

            EmitField(buffer, field);
        }

        return buffer.ToString();
    }

    private static void EmitField(GdBuffer buffer, FieldDefinition field)
    {
        if (!string.IsNullOrWhiteSpace(field.Doc))
        {
            buffer.Doc(field.Doc);
        }

        // Variant is not an exportable type, so those properties stay off the inspector.
        var export = field.Type.ElementGdType == "Variant" ? string.Empty : "@export ";

        var declaration = field.Type.NeedsNullableDeclaration
            ? $"{export}var {field.GdName}: {field.Type.GdType}:"
            : $"{export}var {field.GdName}: {field.Type.GdType} = {field.Type.DefaultValue}:";

        buffer.Line(declaration);
        using (buffer.Indented())
        {
            buffer.Line("set(value):");
            using (buffer.Indented())
            {
                buffer.Line($"{field.GdName} = value");
                buffer.Line($"_track(&\"{field.JsonName}\", value)");
            }
        }

        buffer.Blank();
    }

    private static void EmitFromJson(GdBuffer buffer, ShapeDefinition shape)
    {
        buffer.Doc($"Build a {shape.ClassName} from a decoded JSON dictionary.");
        buffer.DocBlank();
        buffer.Doc("Missing and null keys are left at their defaults, so a partial payload is safe to parse.");
        buffer.Line($"static func from_json(source: Variant) -> {shape.ClassName}:");

        using (buffer.Indented())
        {
            buffer.Line($"var result: {shape.ClassName} = {shape.ClassName}.new()");
            buffer.Line("if source is not Dictionary:");
            using (buffer.Indented())
            {
                buffer.Line("return result");
            }

            buffer.Line("var data: Dictionary = source");
            buffer.Blank();

            foreach (var field in shape.Fields)
            {
                buffer.Line($"if data.get(\"{field.JsonName}\") != null:");
                using (buffer.Indented())
                {
                    if (field.Type.IsArray && field.Type.ElementIsShape)
                    {
                        buffer.Line($"result.{field.GdName} = {field.Type.ElementGdType}.list_from_json(data[\"{field.JsonName}\"])");
                    }
                    else if (field.Type.IsArray)
                    {
                        var elementCast = field.Type.ElementGdType switch
                        {
                            "int" => "int(entry)",
                            "float" => "float(entry)",
                            "bool" => "bool(entry)",
                            "String" => "str(entry)",
                            _ => "entry",
                        };

                        buffer.Line($"var entries: {field.Type.GdType} = []");
                        buffer.Line($"for entry: Variant in data[\"{field.JsonName}\"]:");
                        using (buffer.Indented())
                        {
                            buffer.Line($"entries.append({elementCast})");
                        }

                        buffer.Line($"result.{field.GdName} = entries");
                    }
                    else if (field.Type.ElementIsShape)
                    {
                        buffer.Line($"result.{field.GdName} = {field.Type.ElementGdType}.from_json(data[\"{field.JsonName}\"])");
                    }
                    else
                    {
                        var cast = field.Type.ElementGdType switch
                        {
                            "int" => $"int(data[\"{field.JsonName}\"])",
                            "float" => $"float(data[\"{field.JsonName}\"])",
                            "bool" => $"bool(data[\"{field.JsonName}\"])",
                            "String" => $"str(data[\"{field.JsonName}\"])",
                            _ => $"data[\"{field.JsonName}\"]",
                        };

                        buffer.Line($"result.{field.GdName} = {cast}");
                    }
                }
            }

            buffer.Blank();
            buffer.Line("return result");
        }
    }

    private static void EmitListFromJson(GdBuffer buffer, ShapeDefinition shape)
    {
        buffer.Doc($"Build an array of {shape.ClassName} from a decoded JSON array.");
        buffer.Line($"static func list_from_json(source: Variant) -> Array[{shape.ClassName}]:");

        using (buffer.Indented())
        {
            buffer.Line($"var result: Array[{shape.ClassName}] = []");
            buffer.Line("if source is not Array:");
            using (buffer.Indented())
            {
                buffer.Line("return result");
            }

            buffer.Blank();
            buffer.Line("for entry: Variant in source:");
            using (buffer.Indented())
            {
                buffer.Line($"result.append({shape.ClassName}.from_json(entry))");
            }

            buffer.Blank();
            buffer.Line("return result");
        }
    }

    private static void EmitCreate(GdBuffer buffer, ShapeDefinition shape, IReadOnlyList<FieldDefinition> required)
    {
        var parameters = string.Join(", ", required.Select(field => $"{field.GdName}: {field.Type.GdType}"));

        buffer.Doc("Construct with every required field set.");
        buffer.Line($"static func create({parameters}) -> {shape.ClassName}:");

        using (buffer.Indented())
        {
            buffer.Line($"var body: {shape.ClassName} = {shape.ClassName}.new()");
            foreach (var field in required)
            {
                buffer.Line($"body.{field.GdName} = {field.GdName}");
            }

            buffer.Line("return body");
        }
    }
}
