using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace PANiXiDA.Core.Domain.Generators.Identifiers;

/// <summary>
/// Generates string representations for partial strongly typed identifier structs.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class StronglyTypedIdGenerator : IIncrementalGenerator
{
    private const string IdentifierName = "StronglyTypedId";
    private const string IdentifierMetadataName = "PANiXiDA.Core.Domain.Identifiers.IStronglyTypedId";

    private static readonly DiagnosticDescriptor PartialRequired = new(
        "PANID001",
        "Identifier generation requires partial types",
        "Type '{0}' must be partial to generate the identifier string representation",
        IdentifierName,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor FileLocalUnsupported = new(
        "PANID002",
        "File-local types cannot contain generated identifiers",
        "Type '{0}' must not be file-local to generate the identifier string representation",
        IdentifierName,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var identifiers = context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => node is TypeDeclarationSyntax { BaseList: not null } declaration
                    && declaration.Modifiers.Any(SyntaxKind.PartialKeyword),
                static (syntaxContext, cancellationToken) => CreateGeneration(syntaxContext, cancellationToken))
            .Where(static result => result.HasValue)
            .Select(static (result, _) => result!.Value);

        context.RegisterSourceOutput(identifiers, static (productionContext, generation) =>
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
        var declaration = (TypeDeclarationSyntax)context.Node;
        var type = context.SemanticModel.GetDeclaredSymbol(declaration, cancellationToken)!;
        var identifierType = context.SemanticModel.Compilation.GetTypeByMetadataName(IdentifierMetadataName);
        if (type.TypeKind != TypeKind.Struct || identifierType is null
            || !type.AllInterfaces.Any(item => SymbolEqualityComparer.Default.Equals(item, identifierType))
            || HasDeclaredToString(type))
        {
            return null;
        }

        var firstDeclaration = type.DeclaringSyntaxReferences
            .Select(reference => reference.GetSyntax(cancellationToken))
            .OfType<TypeDeclarationSyntax>()
            .First(item => item.BaseList is not null);
        if (firstDeclaration.SyntaxTree != declaration.SyntaxTree || firstDeclaration.Span != declaration.Span)
        {
            return null;
        }

        var declarationError = ValidateDeclarations(type, cancellationToken);
        if (declarationError.HasValue)
        {
            return declarationError;
        }

        var valueProperty = identifierType.GetMembers("Value").OfType<IPropertySymbol>().Single();
        var implementation = type.FindImplementationForInterfaceMember(valueProperty) as IPropertySymbol;
        bool hasPublicValue = implementation is { DeclaredAccessibility: Accessibility.Public, ContainingType.TypeKind: TypeKind.Struct }
            && implementation.ExplicitInterfaceImplementations.IsEmpty;
        string source = StronglyTypedIdSourceBuilder.Build(type, identifierType, hasPublicValue);
        return GenerationResult.Success(TypeSourceBuilder.GetHintName(type, IdentifierName), source);
    }

    private static bool HasDeclaredToString(INamedTypeSymbol type)
    {
        return type.GetMembers("ToString").OfType<IMethodSymbol>().Any(method =>
            !method.IsImplicitlyDeclared && method.MethodKind == MethodKind.Ordinary
            && method.Arity == 0 && method.Parameters.IsEmpty);
    }

    private static GenerationResult? ValidateDeclarations(
        INamedTypeSymbol type,
        CancellationToken cancellationToken)
    {
        for (var current = type; current is not null; current = current.ContainingType)
        {
            foreach (var reference in current.DeclaringSyntaxReferences)
            {
                var syntax = (TypeDeclarationSyntax)reference.GetSyntax(cancellationToken);
                bool isFileLocal = syntax.Modifiers.Any(SyntaxKind.FileKeyword);
                if (isFileLocal || !syntax.Modifiers.Any(SyntaxKind.PartialKeyword))
                {
                    return GenerationResult.Error(
                        current.Name,
                        isFileLocal ? FileLocalUnsupported.Id : PartialRequired.Id,
                        syntax.Identifier.GetLocation());
                }
            }
        }

        return null;
    }
}
