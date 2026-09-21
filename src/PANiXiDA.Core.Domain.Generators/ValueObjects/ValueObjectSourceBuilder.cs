using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace PANiXiDA.Core.Domain.Generators.ValueObjects;

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
            string componentNames = string.Join(", ", properties.Select(property => SymbolDisplay.FormatLiteral(property.Name, quote: true)));
            string source = $$"""
                public override string ToString()
                {
                    global::System.ReadOnlySpan<string> componentNames = [{{componentNames}}];
                    var builder = new global::System.Text.StringBuilder({{SymbolDisplay.FormatLiteral(type.Name, quote: true)}}).Append(" {");
                    int index = 0;
                    foreach (var component in this.GetEqualityComponents())
                    {
                        builder.Append(index == 0 ? " " : ", ");
                        if (index < componentNames.Length)
                        {
                            builder.Append(componentNames[index]);
                        }
                        else
                        {
                            builder.Append('[').Append(index.ToString(global::System.Globalization.CultureInfo.InvariantCulture)).Append(']');
                        }

                        builder.Append(" = ").Append(component is null
                            ? "null"
                            : global::System.Convert.ToString(component, global::System.Globalization.CultureInfo.InvariantCulture));
                        index++;
                    }

                    return builder.Append(" }").ToString();
                }
                """;
            foreach (string line in source.Replace("\r\n", "\n").Split('\n'))
            {
                builder.Append(indent).AppendLine(line);
            }
        }

        return TypeSourceBuilder.Complete(builder, depth);
    }
}
