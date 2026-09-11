using Down2Plugin.Emit;
using Down2Plugin.OpenApi;
using Down2Plugin.Overlay;

namespace Down2Plugin.Model;

/// <summary>Reconciles the OpenAPI document with the overlay into a single model for the emitters.</summary>
public static class ApiModelBuilder
{
    public static ApiModel Build(OpenApiDocument spec, OverlayDocument overlay)
    {
        var prefix = overlay.ClassPrefix;
        var shapes = BuildShapes(overlay, prefix);
        var shapesByName = shapes.ToDictionary(shape => shape.Name, StringComparer.Ordinal);

        var operations = new List<OperationDefinition>();
        var skipped = new List<string>();
        var matched = new HashSet<string>(StringComparer.Ordinal);

        foreach (var specOperation in spec.Operations)
        {
            if (!overlay.Operations.TryGetValue(specOperation.Key, out var overlayOperation))
            {
                skipped.Add($"{specOperation.Method.ToUpperInvariant(),-6} {specOperation.Path}"
                            + (specOperation.Summary is null ? "" : $"  ({specOperation.Summary})"));
                continue;
            }

            matched.Add(specOperation.Key);
            operations.Add(BuildOperation(specOperation, overlayOperation, overlay, shapesByName, prefix));
        }

        var missing = overlay.Operations.Keys.Where(key => !matched.Contains(key)).ToArray();
        if (missing.Length > 0)
        {
            throw new InvalidDataException(
                "The overlay describes operations the spec does not have. Either the API changed or "
                + "a key is misspelled:\n  " + string.Join("\n  ", missing));
        }

        return new ApiModel
        {
            SpecTitle = spec.Title,
            SpecVersion = spec.Version,
            ServerUrl = spec.ServerUrl,
            ClassPrefix = prefix,
            Shapes = shapes,
            Operations = operations.OrderBy(o => o.Name, StringComparer.Ordinal).ToArray(),
            Skipped = skipped,
        };
    }

    private static List<ShapeDefinition> BuildShapes(OverlayDocument overlay, string prefix)
    {
        var shapes = new List<ShapeDefinition>();

        foreach (var (name, model) in overlay.AllShapes)
        {
            var isBody = overlay.Bodies.ContainsKey(name);
            var className = prefix + name;

            shapes.Add(new ShapeDefinition
            {
                Name = name,
                ClassName = className,
                FileName = Naming.ToFileName(className),
                Doc = model.Doc,
                IsBody = isBody,
                Fields = model.Fields.Select(field => new FieldDefinition
                {
                    JsonName = field.Json,
                    GdName = Naming.SafeIdentifier(Naming.ToSnakeCase(field.Json)),
                    CsName = Naming.ToPascalCase(field.Json),
                    Type = ResolveType(field.Type, overlay, prefix),
                    Doc = field.Doc,
                    Required = field.Required,
                }).ToArray(),
            });
        }

        return shapes.OrderBy(shape => shape.ClassName, StringComparer.Ordinal).ToList();
    }

    private static OperationDefinition BuildOperation(
        OpenApiOperation spec,
        OverlayOperation overlayOperation,
        OverlayDocument overlay,
        IReadOnlyDictionary<string, ShapeDefinition> shapes,
        string prefix)
    {
        var pathParams = spec.Parameters
            .Where(parameter => parameter.In == "path")
            .Select(ToParameter)
            .ToArray();

        var query = spec.Parameters.Where(parameter => parameter.In == "query").ToArray();
        var requiredQuery = query.Where(parameter => parameter.Required).Select(ToParameter).ToArray();
        var optionalQuery = query.Where(parameter => !parameter.Required).Select(ToParameter).ToArray();

        string? optionsClass = null;
        string? optionsFile = null;
        if (optionalQuery.Length > 0)
        {
            optionsClass = prefix + Naming.ToPascalCase(overlayOperation.Name) + "Options";
            optionsFile = Naming.ToFileName(optionsClass);
        }

        ShapeDefinition? body = null;
        if (overlayOperation.Body is not null)
        {
            body = shapes[overlayOperation.Body];
        }

        return new OperationDefinition
        {
            HttpMethod = spec.Method,
            Path = spec.Path,
            Name = overlayOperation.Name,
            Doc = overlayOperation.Doc ?? spec.Summary,
            Summary = spec.Summary,
            AuthLabel = spec.AuthLabel,
            RequiresAuth = spec.RequiresAuth,
            PathParams = pathParams,
            RequiredQuery = requiredQuery,
            OptionalQuery = optionalQuery,
            Body = body,
            MultipartFileField = overlayOperation.Multipart?.FileField,
            ResolveUrl = overlayOperation.ResolveUrl,
            Result = BuildResult(overlayOperation.Returns, overlay, prefix),
            OptionsClassName = optionsClass,
            OptionsFileName = optionsFile,
        };
    }

    private static ParameterDefinition ToParameter(OpenApiParameter parameter) => new()
    {
        JsonName = parameter.Name,
        GdName = Naming.SafeIdentifier(Naming.ToSnakeCase(parameter.Name)),
        GdType = parameter.Type switch
        {
            "integer" => "int",
            "number" => "float",
            "boolean" => "bool",
            "array" => "Array",
            _ => "String",
        },
        CsName = Naming.SafeCsIdentifier(parameter.Name),
        CsType = parameter.Type switch
        {
            "integer" => "int",
            "number" => "double",
            "boolean" => "bool",
            "array" => "List<string>",
            _ => "string",
        },
        Doc = parameter.Description,
    };

    private static ResultType BuildResult(string returns, OverlayDocument overlay, string prefix)
    {
        if (string.IsNullOrWhiteSpace(returns) || returns == "void")
        {
            var voidClass = prefix + "VoidResult";
            return new ResultType { ClassName = voidClass, FileName = Naming.ToFileName(voidClass) };
        }

        var payload = ResolveType(returns, overlay, prefix);
        var bareName = payload.ElementGdType.StartsWith(prefix, StringComparison.Ordinal)
            ? payload.ElementGdType[prefix.Length..]
            : Naming.ToPascalCase(payload.ElementGdType);

        var className = prefix + bareName + (payload.IsArray ? "List" : string.Empty) + "Result";

        return new ResultType
        {
            ClassName = className,
            FileName = Naming.ToFileName(className),
            Payload = payload,
        };
    }

    private static TypeReference ResolveType(string raw, OverlayDocument overlay, string prefix)
    {
        var isArray = raw.StartsWith("Array[", StringComparison.Ordinal) && raw.EndsWith(']');
        var element = isArray ? raw[6..^1] : raw;

        var isShape = overlay.IsShape(element);
        var gdType = isShape
            ? prefix + element
            : element switch
            {
                "int" => "int",
                "float" => "float",
                "bool" => "bool",
                "String" => "String",
                "Variant" => "Variant",
                _ => throw new InvalidDataException(
                    $"Overlay type '{raw}' is neither a primitive nor a declared model."),
            };

        // Shape classes share one name across both targets; only the built-in primitives differ.
        var csType = isShape
            ? prefix + element
            : element switch
            {
                "int" => "int",
                "float" => "double",
                "bool" => "bool",
                "String" => "string",
                "Variant" => "object",
                _ => throw new InvalidDataException(
                    $"Overlay type '{raw}' is neither a primitive nor a declared model."),
            };

        return new TypeReference
        {
            Raw = raw,
            IsArray = isArray,
            ElementGdType = gdType,
            ElementCsType = csType,
            ElementIsShape = isShape,
        };
    }
}
