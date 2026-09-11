using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace PANiXiDA.Core.Domain.Generators;

internal readonly record struct GenerationResult
{
    public string HintName { get; }
    public string Source { get; }
    public string? ErrorType { get; }
    public bool IsFileLocal { get; }
    public string Path { get; }
    public TextSpan Span { get; }
    public LinePositionSpan LineSpan { get; }

    private GenerationResult(
        string hintName, string source, string? errorType, bool isFileLocal,
        string path, TextSpan span, LinePositionSpan lineSpan)
    {
        HintName = hintName;
        Source = source;
        ErrorType = errorType;
        IsFileLocal = isFileLocal;
        Path = path;
        Span = span;
        LineSpan = lineSpan;
    }

    public static GenerationResult Success(string hintName, string source)
    {
        return new GenerationResult(hintName, source, null, false, string.Empty, default, default);
    }

    public static GenerationResult Error(string typeName, bool isFileLocal, Location location)
    {
        var lineSpan = location.GetLineSpan();
        return new GenerationResult(string.Empty, string.Empty, typeName, isFileLocal,
            lineSpan.Path, location.SourceSpan, lineSpan.Span);
    }
}
