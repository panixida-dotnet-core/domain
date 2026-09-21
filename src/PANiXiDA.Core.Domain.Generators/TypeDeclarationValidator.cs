using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PANiXiDA.Core.Domain.Generators;

internal static class TypeDeclarationValidator
{
    public static GenerationResult? Validate(
        INamedTypeSymbol type,
        DiagnosticDescriptor partialRequired,
        DiagnosticDescriptor fileLocalUnsupported,
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
                        isFileLocal ? fileLocalUnsupported.Id : partialRequired.Id,
                        syntax.Identifier.GetLocation());
                }
            }
        }

        return null;
    }
}
