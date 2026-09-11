using Down2Plugin.Emit;

namespace Down2Plugin.Model;

/// <summary>Everything the emitters need, with the spec and the overlay already reconciled.</summary>
public sealed class ApiModel
{
    public required string SpecTitle { get; init; }
    public required string SpecVersion { get; init; }
    public required string ServerUrl { get; init; }
    public required string ClassPrefix { get; init; }
    public required IReadOnlyList<ShapeDefinition> Shapes { get; init; }
    public required IReadOnlyList<OperationDefinition> Operations { get; init; }

    /// <summary>Spec operations with no overlay entry, reported so coverage gaps stay visible.</summary>
    public required IReadOnlyList<string> Skipped { get; init; }

    public IEnumerable<ResultType> ResultTypes =>
        Operations
            .Select(operation => operation.Result)
            .DistinctBy(result => result.ClassName)
            .OrderBy(result => result.ClassName, StringComparer.Ordinal);

    public IEnumerable<OperationDefinition> OperationsWithOptions =>
        Operations.Where(operation => operation.OptionalQuery.Count > 0);
}

/// <summary>A generated data class: either a response model or a request body.</summary>
public sealed class ShapeDefinition
{
    public required string Name { get; init; }
    public required string ClassName { get; init; }
    public required string FileName { get; init; }
    public string? Doc { get; init; }
    public required bool IsBody { get; init; }
    public required IReadOnlyList<FieldDefinition> Fields { get; init; }

    public IEnumerable<FieldDefinition> RequiredFields => Fields.Where(shapeField => shapeField.Required);
}

public sealed class FieldDefinition
{
    /// <summary>The key as it appears in JSON.</summary>
    public required string JsonName { get; init; }

    /// <summary>The GDScript property name.</summary>
    public required string GdName { get; init; }

    /// <summary>The C# property name (PascalCase).</summary>
    public required string CsName { get; init; }

    public required TypeReference Type { get; init; }
    public string? Doc { get; init; }
    public required bool Required { get; init; }
}

/// <summary>A resolved overlay type, mapped onto both GDScript and C#.</summary>
public sealed class TypeReference
{
    public required string Raw { get; init; }
    public required bool IsArray { get; init; }

    /// <summary>For arrays this is the element type, otherwise the type itself.</summary>
    public required string ElementGdType { get; init; }

    /// <summary>The C# equivalent of <see cref="ElementGdType"/> ("string", "double", a class name, ...).</summary>
    public required string ElementCsType { get; init; }

    /// <summary>True when the element is a generated data class rather than a built-in.</summary>
    public required bool ElementIsShape { get; init; }

    public string GdType => IsArray ? $"Array[{ElementGdType}]" : ElementGdType;

    /// <summary>C# has real generics, so an array is just <c>List&lt;T&gt;</c>.</summary>
    public string CsType => IsArray ? $"List<{ElementCsType}>" : ElementCsType;

    /// <summary>
    /// The value a freshly constructed property holds. Typed arrays need their own instance,
    /// and object typed properties start as null rather than a default construction.
    /// </summary>
    public string DefaultValue => IsArray
        ? "[]"
        : ElementGdType switch
        {
            "int" => "0",
            "float" => "0.0",
            "bool" => "false",
            "String" => "\"\"",
            _ => "null",
        };

    /// <summary>The C# equivalent of <see cref="DefaultValue"/>.</summary>
    public string CsDefaultValue => IsArray
        ? "[]"
        : ElementCsType switch
        {
            "int" => "0",
            "double" => "0.0",
            "bool" => "false",
            "string" => "\"\"",
            _ => "null",
        };

    /// <summary>Object typed properties must be declared nullable-friendly, so they are untyped-but-hinted.</summary>
    public bool NeedsNullableDeclaration => !IsArray && ElementIsShape;

    /// <summary>
    /// Shapes and <c>Variant</c>/<c>object</c> fields need a '?' in C#'s nullable-reference mode;
    /// arrays are never null (they default to an empty list) and value types default in place.
    /// </summary>
    public bool NeedsCsNullable => !IsArray && (ElementIsShape || ElementCsType == "object");
}

public sealed class OperationDefinition
{
    public required string HttpMethod { get; init; }
    public required string Path { get; init; }
    public required string Name { get; init; }
    public string? Doc { get; init; }
    public string? Summary { get; init; }
    public required string AuthLabel { get; init; }
    public required bool RequiresAuth { get; init; }

    public required IReadOnlyList<ParameterDefinition> PathParams { get; init; }
    public required IReadOnlyList<ParameterDefinition> RequiredQuery { get; init; }
    public required IReadOnlyList<ParameterDefinition> OptionalQuery { get; init; }

    public ShapeDefinition? Body { get; init; }
    public string? MultipartFileField { get; init; }

    /// <summary>The payload is a server relative path that should be resolved against the API host.</summary>
    public required bool ResolveUrl { get; init; }
    public required ResultType Result { get; init; }

    /// <summary>Class holding the optional query parameters, when there are any.</summary>
    public string? OptionsClassName { get; init; }

    public string? OptionsFileName { get; init; }

    public string GdHttpMethod => "HTTPClient.METHOD_" + HttpMethod.ToUpperInvariant();

    /// <summary>"list_games" -> "System.Net.Http.HttpMethod.Get".</summary>
    public string CsHttpMethod => "HttpMethod." + Naming.ToPascalCase(HttpMethod);

    /// <summary>"list_games" -> "ListGamesAsync", following C#'s Task-suffix convention.</summary>
    public string CsName => Naming.ToPascalCase(Name) + "Async";
}

public sealed class ParameterDefinition
{
    public required string JsonName { get; init; }
    public required string GdName { get; init; }
    public required string GdType { get; init; }

    /// <summary>
    /// The C# parameter name. JSON parameter names are already camelCase, so this is the JSON
    /// name itself (escaped with '@' on the rare keyword collision) rather than a re-casing.
    /// </summary>
    public required string CsName { get; init; }

    public required string CsType { get; init; }
    public string? Doc { get; init; }
}

/// <summary>The typed wrapper a generated method returns.</summary>
public sealed class ResultType
{
    public required string ClassName { get; init; }
    public required string FileName { get; init; }

    /// <summary>Null for operations that return no payload.</summary>
    public TypeReference? Payload { get; init; }

    public bool IsVoid => Payload is null;
}
