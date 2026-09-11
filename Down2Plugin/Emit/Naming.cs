using System.Text;

namespace Down2Plugin.Emit;

/// <summary>Conversions between API spelling (camelCase) and GDScript spelling (snake_case / PascalCase).</summary>
public static class Naming
{
    /// <summary>"profilePicture" -> "profile_picture", "gamePageId" -> "game_page_id".</summary>
    public static string ToSnakeCase(string value)
    {
        var builder = new StringBuilder(value.Length + 8);

        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];

            if (c is '-' or ' ' or '.')
            {
                builder.Append('_');
                continue;
            }

            if (char.IsUpper(c))
            {
                // Deliberately not treating a preceding digit as a word boundary: the class
                // prefix "D2Jam" must snake-case to "d2jam", not "d2_jam".
                var previousIsLower = i > 0 && char.IsLower(value[i - 1]);
                var nextIsLower = i + 1 < value.Length && char.IsLower(value[i + 1]);
                var previousIsUpper = i > 0 && char.IsUpper(value[i - 1]);

                if (i > 0 && (previousIsLower || (previousIsUpper && nextIsLower)))
                {
                    builder.Append('_');
                }

                builder.Append(char.ToLowerInvariant(c));
                continue;
            }

            builder.Append(c);
        }

        return builder.ToString();
    }

    /// <summary>"list_games" -> "ListGames", "get /games/{gameSlug}" -> "GetGamesGameSlug".</summary>
    public static string ToPascalCase(string value)
    {
        var builder = new StringBuilder(value.Length);
        var capitalise = true;

        foreach (var c in value)
        {
            if (!char.IsLetterOrDigit(c))
            {
                capitalise = true;
                continue;
            }

            builder.Append(capitalise ? char.ToUpperInvariant(c) : c);
            capitalise = false;
        }

        return builder.ToString();
    }

    /// <summary>Class name to file name: "D2JamGamePage" -> "d2jam_game_page.gd".</summary>
    public static string ToFileName(string className) => ToSnakeCase(className) + ".gd";

    /// <summary>Class name to C# file name: "D2JamGamePage" -> "D2JamGamePage.cs".</summary>
    public static string ToCsFileName(string className) => className + ".cs";

    /// <summary>
    /// GDScript reserves a handful of words that also appear as API field names
    /// (`class`, `func`, `signal`, `match`, ...). Suffix those so the emitted code parses.
    /// </summary>
    public static string SafeIdentifier(string name) =>
        Reserved.Contains(name) ? name + "_" : name;

    /// <summary>
    /// A JSON field/parameter name is usually already valid as a C# camelCase identifier, but a
    /// handful collide with C# keywords ("class", "params", "in", ...). Escape those with '@'
    /// rather than renaming them, so the identifier still reads the same as the JSON key.
    /// </summary>
    public static string SafeCsIdentifier(string name) =>
        CsReserved.Contains(name) ? "@" + name : name;

    private static readonly HashSet<string> Reserved =
    [
        "if", "elif", "else", "for", "while", "match", "when", "break", "continue", "pass",
        "return", "class", "class_name", "extends", "is", "in", "as", "self", "signal", "func",
        "static", "const", "enum", "var", "breakpoint", "preload", "await", "yield", "assert",
        "void", "PI", "TAU", "INF", "NAN", "and", "or", "not", "true", "false", "null", "super",
        "trait", "namespace", "get", "set", "range",
    ];

    private static readonly HashSet<string> CsReserved =
    [
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
        "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
        "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
        "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
        "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
        "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed",
        "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw",
        "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using",
        "virtual", "void", "volatile", "while",
    ];
}
