using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace PANiXiDA.Core.Domain.Generators;

internal readonly struct GenerationResult : IEquatable<GenerationResult>
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

    public bool Equals(GenerationResult other)
    {
        return HintName == other.HintName && Source == other.Source
            && ErrorType == other.ErrorType && IsFileLocal == other.IsFileLocal
            && Path == other.Path && Span.Equals(other.Span) && LineSpan.Equals(other.LineSpan);
    }

    public override bool Equals(object? obj)
    {
        return obj is GenerationResult other && Equals(other);
    }

    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(HintName ?? string.Empty)
            ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
    }
}
