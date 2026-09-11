using System.Text;

namespace Down2Plugin.Emit;

/// <summary>
/// A tab indented text buffer. Godot's style guide is tabs, and GDScript is whitespace
/// sensitive, so indentation is tracked here rather than baked into template strings.
/// </summary>
public sealed class GdBuffer
{
    private readonly StringBuilder _builder = new();
    private int _indent;

    public IDisposable Indented()
    {
        _indent++;
        return new Dedent(this);
    }

    public GdBuffer Line(string text = "")
    {
        if (text.Length == 0)
        {
            _builder.Append('\n');
            return this;
        }

        foreach (var line in text.Split('\n'))
        {
            if (line.Length == 0)
            {
                _builder.Append('\n');
                continue;
            }

            _builder.Append('\t', _indent).Append(line).Append('\n');
        }

        return this;
    }

    /// <summary>Emits a `##` documentation comment, wrapping paragraphs at a readable width.</summary>
    public GdBuffer Doc(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return this;
        }

        foreach (var paragraph in text.Replace("\r\n", "\n").Split('\n'))
        {
            if (paragraph.Length == 0)
            {
                Line("##");
                continue;
            }

            foreach (var line in Wrap(paragraph, 96))
            {
                Line("## " + line);
            }
        }

        return this;
    }

    /// <summary>Emits a bare `##` line, the separator inside a documentation block.</summary>
    public GdBuffer DocBlank() => Line("##");

    public GdBuffer Blank(int count = 1)
    {
        for (var i = 0; i < count; i++)
        {
            _builder.Append('\n');
        }

        return this;
    }

    public override string ToString()
    {
        // Exactly one trailing newline, no trailing blank lines.
        return _builder.ToString().TrimEnd('\n') + "\n";
    }

    private static IEnumerable<string> Wrap(string text, int width)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            yield break;
        }

        var line = new StringBuilder();
        foreach (var word in words)
        {
            if (line.Length > 0 && line.Length + 1 + word.Length > width)
            {
                yield return line.ToString();
                line.Clear();
            }

            if (line.Length > 0)
            {
                line.Append(' ');
            }

            line.Append(word);
        }

        if (line.Length > 0)
        {
            yield return line.ToString();
        }
    }

    private sealed class Dedent(GdBuffer buffer) : IDisposable
    {
        public void Dispose() => buffer._indent--;
    }
}
