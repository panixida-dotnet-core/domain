using System.Text;

using Microsoft.CodeAnalysis;

namespace PANiXiDA.Core.Domain.Generators.Enumerations;

internal static class EnumerationSourceBuilder
{
    public static string Build(INamedTypeSymbol type)
    {
        var (builder, depth) = TypeSourceBuilder.Begin(type);
        string typeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        string baseTypeName = "global::PANiXiDA.Core.Domain.Enumerations.Enumeration<" + typeName + ">";
        string listTypeName = "global::System.Collections.Generic.IReadOnlyList<" + typeName + ">";
        var memberNames = new HashSet<string>(type.TypeParameters.Select(parameter => parameter.Name));
        for (var current = type; current is not null; current = current.BaseType)
        {
            memberNames.UnionWith(current.GetMembers().Select(member => member.Name));
        }

        string valuesFieldName = "__enumerationValues";
        while (!memberNames.Add(valuesFieldName))
        {
            valuesFieldName += "_";
        }

        string indent = new(' ', (depth + 1) * 4);
        AppendLookupMethods(builder, indent, typeName, baseTypeName, listTypeName, valuesFieldName);
        builder.AppendLine();
        builder.Append(indent).Append("private static readonly global::System.Lazy<").Append(listTypeName)
            .Append("> ").Append(valuesFieldName).AppendLine(" = new(static () =>");
        builder.Append(indent).AppendLine("{");
        builder.Append(indent).Append("    var items = new global::System.Collections.Generic.List<")
            .Append(typeName).AppendLine(">();");

        int index = 0;
        foreach (var field in type.GetMembers().OfType<IFieldSymbol>()
                     .Where(field => field.IsStatic && field.DeclaredAccessibility == Accessibility.Public
                         && !field.IsImplicitlyDeclared))
        {
            builder.Append(indent).Append("    if ((object?)").Append(typeName).Append(".@")
                .Append(field.Name).Append(" is ").Append(typeName).Append(" value").Append(index).AppendLine(")");
            builder.Append(indent).AppendLine("    {");
            builder.Append(indent).Append("        items.Add(value").Append(index).AppendLine(");");
            builder.Append(indent).AppendLine("    }");
            index++;
        }

        builder.AppendLine();
        string initialization = $$"""
            for (int index = 0; index < items.Count; index++)
            {
                {{baseTypeName}} item = items[index];
                for (int previous = 0; previous < index; previous++)
                {
                    if ((({{baseTypeName}})items[previous]).Id == item.Id)
                    {
                        throw new global::System.InvalidOperationException(
                            $"Duplicate id '{item.Id}' in {typeof({{typeName}}).Name}");
                    }
                }

                global::System.ArgumentNullException.ThrowIfNull(item.Name, "key");
                for (int previous = 0; previous < index; previous++)
                {
                    if (global::System.String.Equals((({{baseTypeName}})items[previous]).Name,
                        item.Name, global::System.StringComparison.Ordinal))
                    {
                        throw new global::System.InvalidOperationException(
                            $"Duplicate name '{item.Name}' in {typeof({{typeName}}).Name}");
                    }
                }
            }

            items.Sort(static (left, right) =>
                (({{baseTypeName}})left).Id.CompareTo((({{baseTypeName}})right).Id));
            return items.AsReadOnly();
            """;
        foreach (string line in initialization.Replace("\r\n", "\n").Split('\n'))
        {
            builder.Append(indent).Append("    ").AppendLine(line);
        }

        builder.Append(indent).AppendLine("});");
        return TypeSourceBuilder.Complete(builder, depth);
    }

    private static void AppendLookupMethods(
        StringBuilder builder,
        string indent,
        string typeName,
        string baseTypeName,
        string listTypeName,
        string valuesFieldName)
    {
        string methods = $$"""
            /// <summary>
            /// Gets all declared enumeration values ordered by identifier.
            /// </summary>
            /// <returns>The same immutable list of declared enumeration values on every call.</returns>
            public static {{listTypeName}} GetAll() => {{valuesFieldName}}.Value;

            /// <summary>
            /// Gets an enumeration value by its identifier.
            /// </summary>
            /// <param name="id">The enumeration value identifier.</param>
            /// <returns>The enumeration value with the specified identifier.</returns>
            /// <exception cref="global::System.InvalidOperationException">Thrown when the identifier is not declared.</exception>
            public static {{typeName}} FromId(int id) => {{baseTypeName}}.FromId(id, {{valuesFieldName}});

            /// <summary>
            /// Gets an enumeration value by name after trimming surrounding whitespace.
            /// </summary>
            /// <param name="name">The enumeration value name.</param>
            /// <returns>The enumeration value with the specified name.</returns>
            /// <exception cref="global::System.InvalidOperationException">Thrown when the name is not declared.</exception>
            public static {{typeName}} FromName(string name) => {{baseTypeName}}.FromName(name, {{valuesFieldName}});

            /// <summary>
            /// Tries to get an enumeration value by its identifier.
            /// </summary>
            /// <param name="id">The enumeration value identifier.</param>
            /// <param name="item">The matching enumeration value, or null if no value is found.</param>
            /// <returns>True if a matching value was found; otherwise, false.</returns>
            public static bool TryFromId(int id, out {{typeName}}? item) =>
                {{baseTypeName}}.TryFromId(id, {{valuesFieldName}}, out item);

            /// <summary>
            /// Tries to get an enumeration value by name after trimming surrounding whitespace.
            /// </summary>
            /// <param name="name">The enumeration value name.</param>
            /// <param name="item">The matching enumeration value, or null if no value is found.</param>
            /// <returns>True if a matching value was found; otherwise, false.</returns>
            public static bool TryFromName(string name, out {{typeName}}? item) =>
                {{baseTypeName}}.TryFromName(name, {{valuesFieldName}}, out item);
            """;
        foreach (string line in methods.Replace("\r\n", "\n").Split('\n'))
        {
            builder.Append(indent).AppendLine(line);
        }
    }
}
