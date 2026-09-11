namespace Down2Plugin.OpenApi;

/// <summary>Reads the OpenAPI document from a URL or a local file.</summary>
public static class SpecLoader
{
    public static async Task<string> LoadAsync(string source, string? cachePath, CancellationToken cancellationToken)
    {
        string json;

        if (Uri.TryCreate(source, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            http.DefaultRequestHeaders.Add("Accept", "application/json");
            http.DefaultRequestHeaders.Add("User-Agent", "Down2Plugin-generator/1.0");
            Console.WriteLine($"Downloading spec from {uri} ...");
            json = await http.GetStringAsync(uri, cancellationToken);
        }
        else
        {
            if (!File.Exists(source))
            {
                throw new FileNotFoundException($"Spec file not found: {source}");
            }

            Console.WriteLine($"Reading spec from {source} ...");
            json = await File.ReadAllTextAsync(source, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(cachePath))
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(cachePath));
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(cachePath, json, cancellationToken);
            Console.WriteLine($"Cached spec to {cachePath}");
        }

        return json;
    }
}
