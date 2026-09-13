using System.Collections.Immutable;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace PANiXiDA.Core.Domain.Generators.ValueObjects;

/// <summary>
/// Generates equality components and string representations for partial value objects.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class ValueObjectGenerator : IIncrementalGenerator
{
    // Keep this identifier stable to recognize generated overrides in referenced assemblies.
    internal const string GeneratorName = "PANiXiDA.Core.Domain.Generators.ValueObjectGenerator";
    private const string ValueObjectName = "ValueObject";
    private const string ValueObjectMetadataName = "PANiXiDA.Core.Domain.ValueObjects.ValueObject";

    private static readonly DiagnosticDescriptor PartialRequired = new(
        "PANVO001", "Value object generation requires partial types",
        "Type '{0}' must be partial to generate value object methods",
        ValueObjectName, DiagnosticSeverity.Error, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor FileLocalUnsupported = new(
        "PANVO002", "File-local types cannot contain generated value objects",
        "Type '{0}' must not be file-local to generate value object methods",
        ValueObjectName, DiagnosticSeverity.Error, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor ComponentsRequired = new(
        "PANVO003", "Automatic equality requires stored properties",
        "Type '{0}' has no public read-only or init-only auto-properties; implement GetEqualityComponents explicitly",
        ValueObjectName, DiagnosticSeverity.Error, isEnabledByDefault: true);

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var valueObjects = context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => node is ClassDeclarationSyntax { BaseList: not null },
                static (syntaxContext, cancellationToken) => CreateGeneration(syntaxContext, cancellationToken))
            .Where(static result => result.HasValue)
            .Select(static (result, _) => result!.Value);

        context.RegisterSourceOutput(valueObjects, static (productionContext, generation) =>
        {
            if (generation.ErrorType is not null)
            {
                var descriptor = generation.DiagnosticId switch
                {
                    "PANVO002" => FileLocalUnsupported,
                    "PANVO003" => ComponentsRequired,
                    _ => PartialRequired
                };
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
        var type = context.SemanticModel.GetDeclaredSymbol(declaration, cancellationToken)!;
        var compilation = context.SemanticModel.Compilation;
        var valueObjectType = compilation.GetTypeByMetadataName(ValueObjectMetadataName);
        if (type.IsAbstract || valueObjectType is null || !IsValueObject(type, valueObjectType))
        {
            return null;
        }

        var generatedCodeAttributes = compilation.GetTypesByMetadataName("System.CodeDom.Compiler.GeneratedCodeAttribute");
        bool generateEquality = !HasMethod(
            type,
            valueObjectType,
            "GetEqualityComponents",
            generatedCodeAttributes);
        bool generateToString = !HasMethod(
            type,
            valueObjectType,
            "ToString",
            generatedCodeAttributes);
        if ((!generateEquality && !generateToString)
            || (!generateEquality && !declaration.Modifiers.Any(SyntaxKind.PartialKeyword)))
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

        var declarationError = ValidateDeclarations(type, cancellationToken);
        if (declarationError.HasValue)
        {
            return declarationError;
        }

        var properties = generateEquality
            ? GetProperties(
                type,
                valueObjectType,
                compilation.GetTypesByMetadataName("System.Runtime.CompilerServices.CompilerGeneratedAttribute"),
                cancellationToken)
            : [];
        if (generateEquality && properties.Count == 0)
        {
            return GenerationResult.Error(type.Name, ComponentsRequired.Id, declaration.Identifier.GetLocation());
        }

        string source = ValueObjectSourceBuilder.Build(type, properties, generateEquality, generateToString);
        return GenerationResult.Success(TypeSourceBuilder.GetHintName(type, ValueObjectName), source);
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

    private static bool IsValueObject(
        INamedTypeSymbol type,
        INamedTypeSymbol valueObjectType)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, valueObjectType))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasMethod(
        INamedTypeSymbol type,
        INamedTypeSymbol valueObjectType,
        string name,
        ImmutableArray<INamedTypeSymbol> generatedCodeAttributes)
    {
        // IsValueObject has already verified that this hierarchy reaches ValueObject.
        for (var current = type;
             !SymbolEqualityComparer.Default.Equals(current, valueObjectType);
             current = current.BaseType!)
        {
            var method = current.GetMembers(name).OfType<IMethodSymbol>()
                .FirstOrDefault(member => member.MethodKind == MethodKind.Ordinary
                    && member.Arity == 0 && member.Parameters.Length == 0);
            if (method is not null)
            {
                if (SymbolEqualityComparer.Default.Equals(current, type))
                {
                    return true;
                }

                bool isGenerated = method.GetAttributes().Any(attribute =>
                    generatedCodeAttributes.Any(attributeType => SymbolEqualityComparer.Default.Equals(
                        attribute.AttributeClass,
                        attributeType))
                    && attribute.ConstructorArguments.Length > 0
                    && attribute.ConstructorArguments[0].Value is string tool && tool == GeneratorName);
                if (!isGenerated)
                {
                    return !method.IsAbstract;
                }
            }
        }

        return false;
    }

    private static List<IPropertySymbol> GetProperties(
        INamedTypeSymbol type,
        INamedTypeSymbol valueObjectType,
        ImmutableArray<INamedTypeSymbol> compilerGeneratedAttributes,
        CancellationToken cancellationToken)
    {
        var hierarchy = new Stack<INamedTypeSymbol>();
        // IsValueObject has already verified that this hierarchy reaches ValueObject.
        for (var current = type;
             !SymbolEqualityComparer.Default.Equals(current, valueObjectType);
             current = current.BaseType!)
        {
            hierarchy.Push(current);
        }

        return
        [
            .. hierarchy.SelectMany(current => current.GetMembers().OfType<IPropertySymbol>()
                    .OrderBy(property => property.Locations[0].SourceTree?.FilePath, StringComparer.Ordinal)
                    .ThenBy(property => property.Locations[0].SourceSpan.Start)
                    .ThenBy(property => property.Name, StringComparer.Ordinal))
                .GroupBy(property => property.Name, StringComparer.Ordinal)
                .Select(group => group.Last())
                .Where(property => IsEqualityProperty(property, compilerGeneratedAttributes, cancellationToken))
        ];
    }

    private static bool IsEqualityProperty(
        IPropertySymbol property,
        ImmutableArray<INamedTypeSymbol> compilerGeneratedAttributes,
        CancellationToken cancellationToken)
    {
        if (property.IsStatic || property.IsIndexer || property.IsAbstract
            || property.GetMethod?.DeclaredAccessibility != Accessibility.Public
            || (property.SetMethod is not null && !property.SetMethod.IsInitOnly))
        {
            return false;
        }

        if (property.DeclaringSyntaxReferences.Length == 0)
        {
            return property.GetMethod.GetAttributes().Any(attribute =>
                compilerGeneratedAttributes.Any(attributeType => SymbolEqualityComparer.Default.Equals(
                    attribute.AttributeClass,
                    attributeType)));
        }

        return property.DeclaringSyntaxReferences.Any(reference =>
            reference.GetSyntax(cancellationToken) is PropertyDeclarationSyntax { AccessorList: not null } syntax
            && syntax.AccessorList.Accessors.All(accessor => accessor.Body is null && accessor.ExpressionBody is null));
    }
}
