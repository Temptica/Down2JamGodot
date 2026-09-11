namespace Down2Plugin.Emit;

/// <summary>
/// Writes the emitted files and prunes generated files that are no longer produced, so a
/// renamed or dropped endpoint does not leave a stale class behind for Godot to load.
/// </summary>
public sealed class OutputWriter(string directory, bool dryRun)
{
    private readonly Dictionary<string, string> _files = [];

    public void Add(string fileName, string content) => _files[fileName] = content;

    public WriteReport Commit()
    {
        var report = new WriteReport();

        if (!dryRun)
        {
            Directory.CreateDirectory(directory);
        }

        foreach (var (fileName, content) in _files.OrderBy(entry => entry.Key, StringComparer.Ordinal))
        {
            var path = Path.Combine(directory, fileName);
            var existing = File.Exists(path) ? File.ReadAllText(path) : null;

            if (existing == content)
            {
                report.Unchanged.Add(fileName);
                continue;
            }

            if (existing is null)
            {
                report.Created.Add(fileName);
            }
            else
            {
                report.Updated.Add(fileName);
            }

            if (!dryRun)
            {
                File.WriteAllText(path, content);
            }
        }

        if (Directory.Exists(directory))
        {
            foreach (var path in Directory.EnumerateFiles(directory, "*.gd"))
            {
                var fileName = Path.GetFileName(path);
                if (_files.ContainsKey(fileName))
                {
                    continue;
                }

                report.Removed.Add(fileName);

                if (!dryRun)
                {
                    File.Delete(path);

                    // Godot writes a sibling .uid file for every script; it must go too.
                    var uid = path + ".uid";
                    if (File.Exists(uid))
                    {
                        File.Delete(uid);
                    }
                }
            }
        }

        return report;
    }
}

public sealed class WriteReport
{
    public List<string> Created { get; } = [];
    public List<string> Updated { get; } = [];
    public List<string> Unchanged { get; } = [];
    public List<string> Removed { get; } = [];

    public bool HasChanges => Created.Count > 0 || Updated.Count > 0 || Removed.Count > 0;
}
