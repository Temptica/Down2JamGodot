using Down2Plugin.Cli;
using Down2Plugin.Emit;
using Down2Plugin.Emit.CSharp;
using Down2Plugin.Model;
using Down2Plugin.OpenApi;
using Down2Plugin.Overlay;

try
{
    var options = GeneratorOptions.Parse(args);
    return await RunAsync(options);
}
catch (HelpRequestedException)
{
    Console.WriteLine(GeneratorOptions.HelpText);
    return 0;
}
catch (Exception exception) when (exception is ArgumentException
                                      or FileNotFoundException
                                      or InvalidDataException
                                      or HttpRequestException)
{
    Console.Error.WriteLine($"error: {exception.Message}");
    return 1;
}

static async Task<int> RunAsync(GeneratorOptions options)
{
    using var cancellation = new CancellationTokenSource();
    Console.CancelKeyPress += (_, eventArgs) =>
    {
        eventArgs.Cancel = true;
        cancellation.Cancel();
    };

    var specJson = await SpecLoader.LoadAsync(options.Spec, options.CacheSpec, cancellation.Token);
    var spec = OpenApiDocument.Parse(specJson);
    var overlay = await OverlayDocument.LoadAsync(options.Overlay, cancellation.Token);
    var model = ApiModelBuilder.Build(spec, overlay);

    Console.WriteLine($"{model.SpecTitle} {model.SpecVersion} - {spec.Operations.Count} operations in the spec, "
                      + $"{model.Operations.Count} covered by the overlay.");

    var writer = new OutputWriter(options.Output, options.DryRun);

    foreach (var shape in model.Shapes)
    {
        writer.Add(shape.FileName, ShapeEmitter.Emit(shape));
    }

    foreach (var operation in model.OperationsWithOptions)
    {
        writer.Add(operation.OptionsFileName!, ShapeEmitter.EmitOptions(operation));
    }

    foreach (var result in model.ResultTypes)
    {
        writer.Add(result.FileName, ResultEmitter.Emit(result));
    }

    writer.Add("d2jam_api.gd", ApiEmitter.Emit(model));

    var report = writer.Commit();
    PrintReport(report, options, "GDScript", options.Output);

    if (options.CsharpOutput.Length > 0)
    {
        var csharpWriter = new OutputWriter(options.CsharpOutput, options.DryRun, "*.cs");

        foreach (var shape in model.Shapes)
        {
            csharpWriter.Add(
                Naming.ToCsFileName(shape.ClassName),
                CsShapeEmitter.Emit(shape, options.CsharpNamespace));
        }

        foreach (var operation in model.OperationsWithOptions)
        {
            csharpWriter.Add(
                Naming.ToCsFileName(operation.OptionsClassName!),
                CsShapeEmitter.EmitOptions(operation, options.CsharpNamespace));
        }

        csharpWriter.Add(
            Naming.ToCsFileName(model.ClassPrefix + "Api.g"),
            CsApiEmitter.Emit(model, options.CsharpNamespace));

        var csharpReport = csharpWriter.Commit();
        Console.WriteLine();
        PrintReport(csharpReport, options, "C#", options.CsharpOutput);
    }

    if (options.ShowSkipped && model.Skipped.Count > 0)
    {
        Console.WriteLine();
        Console.WriteLine($"Not generated ({model.Skipped.Count} operations outside the overlay's scope):");
        foreach (var line in model.Skipped)
        {
            Console.WriteLine($"  {line}");
        }
    }

    return 0;
}

static void PrintReport(WriteReport report, GeneratorOptions options, string label, string output)
{
    var verb = options.DryRun ? "would be" : "were";

    Console.WriteLine($"{label} output: {Path.GetFullPath(output)}");

    foreach (var file in report.Created)
    {
        Console.WriteLine($"  + {file}");
    }

    foreach (var file in report.Updated)
    {
        Console.WriteLine($"  ~ {file}");
    }

    foreach (var file in report.Removed)
    {
        Console.WriteLine($"  - {file} (stale, removed)");
    }

    Console.WriteLine();
    Console.WriteLine(report.HasChanges
        ? $"{report.Created.Count} created, {report.Updated.Count} updated, "
          + $"{report.Removed.Count} removed, {report.Unchanged.Count} unchanged - files {verb} written."
        : $"Already up to date ({report.Unchanged.Count} files).");
}
