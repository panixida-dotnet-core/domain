using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace PANiXiDA.Core.Domain.Generators.Enumerations;

/// <summary>
/// Generates immutable value lists and lookup methods for partial enumeration types.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class EnumerationGenerator : IIncrementalGenerator
{
    private const string EnumerationTypeName = "PANiXiDA.Core.Domain.Enumerations.Enumeration<TEnumeration>";

    private static readonly DiagnosticDescriptor PartialRequired = new(
        "PANENUM001",
        "Enumeration generation requires partial types",
        "Type '{0}' must be partial to generate enumeration values",
        "Enumeration",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The enumeration and all containing types must be partial so generated code can access their fields.");

    private static readonly DiagnosticDescriptor FileLocalUnsupported = new(
        "PANENUM002",
        "File-local types cannot contain generated enumerations",
        "Type '{0}' must not be file-local to generate enumeration values",
        "Enumeration",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Generated source is emitted in another file and cannot extend file-local types.");

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var enumerations = context.SyntaxProvider.CreateSyntaxProvider(
            static (node, _) => node is ClassDeclarationSyntax { BaseList: not null },
            static (syntaxContext, cancellationToken) => CreateGeneration(syntaxContext, cancellationToken))
            .Where(static result => result.HasValue)
            .Select(static (result, _) => result!.Value);

        context.RegisterSourceOutput(enumerations, static (productionContext, generation) =>
        {
            if (generation.ErrorType is not null)
            {
                var descriptor = generation.DiagnosticId == FileLocalUnsupported.Id ? FileLocalUnsupported : PartialRequired;
                var location = Location.Create(generation.Path, generation.Span, generation.LineSpan);
                productionContext.ReportDiagnostic(Diagnostic.Create(descriptor, location, generation.ErrorType));
                return;
            }

            productionContext.AddSource(generation.HintName, SourceText.From(generation.Source, Encoding.UTF8));
        });
    }

    private static GenerationResult? CreateGeneration(
        GeneratorSyntaxContext context,
        CancellationToken cancellationToken)
    {
        var declaration = (ClassDeclarationSyntax)context.Node;
        if (context.SemanticModel.GetDeclaredSymbol(declaration, cancellationToken) is not INamedTypeSymbol type
            || !IsEnumeration(type))
        {
            return null;
        }

        var firstDeclaration = type.DeclaringSyntaxReferences
            .Select(reference => reference.GetSyntax(cancellationToken))
            .OfType<ClassDeclarationSyntax>()
            .First(item => item.BaseList is not null);

        if (firstDeclaration.SyntaxTree != declaration.SyntaxTree || firstDeclaration.Span != declaration.Span)
        {
            return null;
        }

        for (var current = type; current is not null; current = current.ContainingType)
        {
            foreach (var reference in current.DeclaringSyntaxReferences)
            {
                var syntax = (TypeDeclarationSyntax)reference.GetSyntax(cancellationToken);
                bool isFileLocal = syntax.Modifiers.Any(SyntaxKind.FileKeyword);
                if (isFileLocal || !syntax.Modifiers.Any(SyntaxKind.PartialKeyword))
                {
                    var location = syntax.Identifier.GetLocation();
                    return GenerationResult.Error(current.Name,
                        isFileLocal ? FileLocalUnsupported.Id : PartialRequired.Id, location);
                }
            }
        }

        string source = EnumerationSourceBuilder.Build(type);
        return GenerationResult.Success(TypeSourceBuilder.GetHintName(type, "Enumeration"), source);
    }

    private static bool IsEnumeration(INamedTypeSymbol type)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.OriginalDefinition.ToDisplayString() == EnumerationTypeName)
            {
                return SymbolEqualityComparer.Default.Equals(current.TypeArguments[0], type);
            }
        }

        return false;
    }
}
