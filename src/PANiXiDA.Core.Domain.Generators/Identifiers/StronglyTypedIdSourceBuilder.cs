using System.Text;

using Microsoft.CodeAnalysis;

namespace PANiXiDA.Core.Domain.Generators.Identifiers;

internal static class StronglyTypedIdSourceBuilder
{
    public static string Build(
        INamedTypeSymbol type,
        INamedTypeSymbol identifierType,
        bool hasPublicValue)
    {
        var (builder, depth) = TypeSourceBuilder.Begin(type);
        string indent = new(' ', (depth + 1) * 4);
        builder.Append(indent).AppendLine("/// <inheritdoc />");
        builder.Append(indent).AppendLine("public override string ToString()");
        builder.Append(indent).AppendLine("{");
        if (hasPublicValue)
        {
            builder.Append(indent).AppendLine("    return this.Value.ToString();");
        }
        else
        {
            string parameterName = GetTypeParameterName(type);
            builder.Append(indent).AppendLine("    return FormatValue(this);");
            builder.AppendLine();
            builder.Append(indent).Append("    static string FormatValue<").Append(parameterName).Append(">(")
                .Append(parameterName).AppendLine(" id)");
            builder.Append(indent).Append("        where ").Append(parameterName).Append(" : ")
                .Append(identifierType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)).AppendLine(", allows ref struct");
            builder.Append(indent).AppendLine("    {");
            builder.Append(indent).AppendLine("        return id.Value.ToString();");
            builder.Append(indent).AppendLine("    }");
        }

        builder.Append(indent).AppendLine("}");
        return TypeSourceBuilder.Complete(builder, depth);
    }

    private static string GetTypeParameterName(INamedTypeSymbol type)
    {
        var names = new HashSet<string>();
        for (var current = type; current is not null; current = current.ContainingType)
        {
            names.Add(current.Name);
            names.UnionWith(current.TypeParameters.Select(parameter => parameter.Name));
        }

        var name = new StringBuilder("TIdentifier");
        while (names.Contains(name.ToString()))
        {
            name.Append('_');
        }

        return name.ToString();
    }
}
