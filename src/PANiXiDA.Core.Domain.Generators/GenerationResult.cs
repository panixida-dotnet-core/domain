using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace PANiXiDA.Core.Domain.Generators;

internal readonly record struct GenerationResult
{
    public string HintName { get; }
    public string Source { get; }
    public string? ErrorType { get; }
    public string? DiagnosticId { get; }
    public string Path { get; }
    public TextSpan Span { get; }
    public LinePositionSpan LineSpan { get; }

    private GenerationResult(
        string hintName,
        string source,
        string? errorType,
        string? diagnosticId,
        string path,
        TextSpan span,
        LinePositionSpan lineSpan)
    {
        HintName = hintName;
        Source = source;
        ErrorType = errorType;
        DiagnosticId = diagnosticId;
        Path = path;
        Span = span;
        LineSpan = lineSpan;
    }

    public static GenerationResult Success(
        string hintName,
        string source)
    {
        return new GenerationResult(
            hintName,
            source,
            null,
            null,
            string.Empty,
            default,
            default);
    }

    public static GenerationResult Error(
        string typeName,
        string diagnosticId,
        Location location)
    {
        var lineSpan = location.GetLineSpan();
        return new GenerationResult(
            string.Empty,
            string.Empty,
            typeName,
            diagnosticId,
            lineSpan.Path,
            location.SourceSpan,
            lineSpan.Span);
    }

    public void Emit(
        SourceProductionContext context,
        DiagnosticDescriptor descriptor)
    {
        if (ErrorType is not null)
        {
            var location = Location.Create(Path, Span, LineSpan);
            context.ReportDiagnostic(Diagnostic.Create(descriptor, location, ErrorType));
            return;
        }

        context.AddSource(HintName, SourceText.From(Source, Encoding.UTF8));
    }
}
