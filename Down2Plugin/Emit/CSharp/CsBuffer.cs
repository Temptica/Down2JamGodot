using System.Text;

namespace Down2Plugin.Emit.CSharp;

/// <summary>
/// A space indented text buffer for emitted C#, mirroring <see cref="GdBuffer"/> but with C#'s
/// four space convention and <c>///</c> XML doc comments instead of GDScript's <c>##</c>.
/// </summary>
public sealed class CsBuffer
{
    private readonly StringBuilder _builder = new();
    private int _indent;

    public IDisposable Indented()
    {
        _indent++;
        return new Dedent(this);
    }

    /// <summary>Opens a brace block: writes <paramref name="header"/>, then "{" on its own line, indented.</summary>
    public IDisposable Block(string header)
    {
        Line(header);
        Line("{");
        _indent++;
        return new BlockClose(this);
    }

    public CsBuffer Line(string text = "")
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

            _builder.Append(' ', _indent * 4).Append(line).Append('\n');
        }

        return this;
    }

    /// <summary>Emits a <c>///</c> XML summary comment, wrapping paragraphs at a readable width.</summary>
    public CsBuffer Doc(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return this;
        }

        Line("/// <summary>");
        foreach (var paragraph in text.Replace("\r\n", "\n").Split('\n'))
        {
            if (paragraph.Length == 0)
            {
                Line("///");
                continue;
            }

            foreach (var line in Wrap(paragraph, 92))
            {
                Line("/// " + EscapeXml(line));
            }
        }

        Line("/// </summary>");
        return this;
    }

    public CsBuffer Blank(int count = 1)
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

    private static string EscapeXml(string text) =>
        text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

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

    private sealed class Dedent(CsBuffer buffer) : IDisposable
    {
        public void Dispose() => buffer._indent--;
    }

    private sealed class BlockClose(CsBuffer buffer) : IDisposable
    {
        public void Dispose()
        {
            buffer._indent--;
            buffer.Line("}");
        }
    }
}
