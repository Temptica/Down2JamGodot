using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Down2Jam.Client;

/// <summary>
/// Shared JSON options for the generated response models.
///
/// Jamcore's Prisma-backed models send an explicit JSON <c>null</c> for an unset optional field
/// rather than omitting the key (a profile's <c>bio</c>, for example). Plain
/// <c>System.Text.Json</c> would happily overwrite a property declared as a non-nullable
/// <c>string</c> with that <c>null</c>, silently breaking the "never null, defaults to empty"
/// contract the generated model's own type already promises.
///
/// This resolver skips the property setter whenever the incoming value is <c>null</c> and the
/// property is a non-nullable reference type, so the class's own field initializer -- already run
/// by the time <c>System.Text.Json</c> gets to setting properties -- is what a caller sees.
/// Nullable properties (a nested shape, <c>string?</c>, and so on) are untouched: <c>null</c>
/// there is a real, meaningful value.
/// </summary>
public static class D2JamJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers = { IgnoreNullForNonNullableReferenceProperties },
        },
    };

    private static readonly NullabilityInfoContext NullabilityContext = new();

    private static void IgnoreNullForNonNullableReferenceProperties(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object)
        {
            return;
        }

        foreach (var property in typeInfo.Properties)
        {
            if (property.PropertyType.IsValueType || property.Set is null)
            {
                continue;
            }

            if (property.AttributeProvider is not PropertyInfo reflected)
            {
                continue;
            }

            if (NullabilityContext.Create(reflected).WriteState != NullabilityState.NotNull)
            {
                continue;
            }

            var originalSet = property.Set;
            property.Set = (obj, value) =>
            {
                if (value is not null)
                {
                    originalSet(obj, value);
                }
            };
        }
    }
}
