using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace PANiXiDA.Core.Domain.Generators;

internal static class ValueObjectSourceBuilder
{
    public static string Build(
        INamedTypeSymbol type,
        IReadOnlyList<IPropertySymbol> properties,
        bool generateEquality,
        bool generateToString)
    {
        var (builder, depth) = TypeSourceBuilder.Begin(type);
        string indent = new(' ', (depth + 1) * 4);
        if (generateEquality)
        {
            builder.Append(indent).AppendLine("/// <inheritdoc />");
            builder.Append(indent).Append("[global::System.CodeDom.Compiler.GeneratedCode(")
                .Append(SymbolDisplay.FormatLiteral(ValueObjectGenerator.GeneratorName, quote: true)).AppendLine(", \"1.0\")]");
            builder.Append(indent).AppendLine("protected override global::System.Collections.Generic.IEnumerable<object?> GetEqualityComponents()");
            builder.Append(indent).AppendLine("{");
            foreach (var property in properties)
            {
                builder.Append(indent).Append("    yield return this.@").Append(property.Name).AppendLine(";");
            }

            builder.Append(indent).AppendLine("}");
        }

        if (generateToString)
        {
            if (generateEquality)
            {
                builder.AppendLine();
            }

            builder.Append(indent).AppendLine("/// <inheritdoc />");
            builder.Append(indent).Append("[global::System.CodeDom.Compiler.GeneratedCode(")
                .Append(SymbolDisplay.FormatLiteral(ValueObjectGenerator.GeneratorName, quote: true)).AppendLine(", \"1.0\")]");
            builder.Append(indent).AppendLine("public override string ToString()");
            builder.Append(indent).AppendLine("{");
            builder.Append(indent).Append("    return base.FormatEqualityComponents(")
                .Append(SymbolDisplay.FormatLiteral(type.Name, quote: true)).Append(", [")
                .Append(string.Join(", ", properties.Select(property => SymbolDisplay.FormatLiteral(property.Name, quote: true))))
                .AppendLine("]);");
            builder.Append(indent).AppendLine("}");
        }

        return TypeSourceBuilder.Complete(builder, depth);
    }
}
