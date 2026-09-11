namespace Down2Plugin.Cli;

/// <summary>
/// Command line configuration for a generator run.
/// </summary>
public sealed class GeneratorOptions
{
    public const string DefaultSpecUrl = "https://d2jam.com/api/v1/openapi";

    /// <summary>Where to read the OpenAPI document from. A URL is downloaded, a path is read from disk.</summary>
    public string Spec { get; init; } = DefaultSpecUrl;

    /// <summary>Path to the semantic overlay that supplies the models the spec omits.</summary>
    public string Overlay { get; init; } = Path.Combine("spec", "d2jam.overlay.json");

    /// <summary>Directory the generated .gd files are written to. Existing generated files not re-emitted are removed.</summary>
    public string Output { get; init; } = Path.Combine("addons", "d2jam", "generated");

    /// <summary>
    /// Directory the generated C# files are written to, for the Down2Jam.Client class library.
    /// Pass an empty string (--no-csharp) to skip the C# client entirely.
    /// </summary>
    public string CsharpOutput { get; init; } = Path.Combine("csharp", "Down2Jam.Client", "Generated");

    /// <summary>Namespace the generated C# types are declared in.</summary>
    public string CsharpNamespace { get; init; } = "Down2Jam.Client";

    /// <summary>When set, the downloaded spec is also written here so runs are reproducible offline.</summary>
    public string? CacheSpec { get; init; }

    /// <summary>Report what would change without touching the filesystem.</summary>
    public bool DryRun { get; init; }

    /// <summary>List every spec operation the overlay leaves out.</summary>
    public bool ShowSkipped { get; init; }

    public static GeneratorOptions Parse(string[] args)
    {
        var spec = DefaultSpecUrl;
        var overlay = Path.Combine("spec", "d2jam.overlay.json");
        var output = Path.Combine("addons", "d2jam", "generated");
        var csharpOutput = Path.Combine("csharp", "Down2Jam.Client", "Generated");
        var csharpNamespace = "Down2Jam.Client";
        string? cacheSpec = null;
        var dryRun = false;
        var showSkipped = false;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--spec":
                    spec = RequireValue(args, ref i);
                    break;
                case "--overlay":
                    overlay = RequireValue(args, ref i);
                    break;
                case "--output" or "-o":
                    output = RequireValue(args, ref i);
                    break;
                case "--csharp-output":
                    csharpOutput = RequireValue(args, ref i);
                    break;
                case "--csharp-namespace":
                    csharpNamespace = RequireValue(args, ref i);
                    break;
                case "--no-csharp":
                    csharpOutput = "";
                    break;
                case "--cache-spec":
                    cacheSpec = RequireValue(args, ref i);
                    break;
                case "--dry-run":
                    dryRun = true;
                    break;
                case "--show-skipped":
                    showSkipped = true;
                    break;
                case "--help" or "-h":
                    throw new HelpRequestedException();
                default:
                    throw new ArgumentException($"Unknown argument '{args[i]}'. Run with --help.");
            }
        }

        return new GeneratorOptions
        {
            Spec = spec,
            Overlay = overlay,
            Output = output,
            CsharpOutput = csharpOutput,
            CsharpNamespace = csharpNamespace,
            CacheSpec = cacheSpec,
            DryRun = dryRun,
            ShowSkipped = showSkipped,
        };
    }

    private static string RequireValue(string[] args, ref int index)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"'{args[index]}' expects a value.");
        }

        return args[++index];
    }

    public static string HelpText =>
        """
        Down2Jam client generator

        Reads the Jamcore OpenAPI document plus a semantic overlay and emits typed clients -
        a GDScript addon and a C# class library - from the same spec and overlay in one run,
        so the two never drift apart.

        Usage:
          dotnet run --project Down2Plugin -- [options]

        Options:
          --spec <url|path>          OpenAPI document to read.
                                     Default: https://d2jam.com/api/v1/openapi
          --overlay <path>           Semantic overlay describing bodies and response models.
                                     Default: spec/d2jam.overlay.json
          -o, --output <dir>         Directory for the generated .gd files.
                                     Default: addons/d2jam/generated
          --csharp-output <dir>      Directory for the generated .cs files.
                                     Default: csharp/Down2Jam.Client/Generated
          --csharp-namespace <name>  Namespace for the generated C# types.
                                     Default: Down2Jam.Client
          --no-csharp                Skip the C# client, emitting only GDScript.
          --cache-spec <path>        Also save the downloaded spec here.
          --dry-run                  Report changes without writing anything.
          --show-skipped             List spec operations the overlay does not cover.
          -h, --help                 Show this help.
        """;
}

public sealed class HelpRequestedException : Exception;
