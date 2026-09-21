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
        var memberNames = new HashSet<string>();
        for (var current = type; current is not null; current = current.BaseType)
        {
            memberNames.UnionWith(current.GetMembers().Select(member => member.Name));
        }

        for (var current = type; current is not null; current = current.ContainingType)
        {
            memberNames.Add(current.Name);
            memberNames.UnionWith(current.TypeParameters.Select(parameter => parameter.Name));
        }

        string valuesFieldName = GetAvailableMemberName(memberNames, "__enumerationValues");
        string valuesTypeName = GetAvailableMemberName(memberNames, "EnumerationValues");
        string indent = new(' ', (depth + 1) * 4);
        AppendLookupMethods(
            builder,
            indent,
            typeName,
            listTypeName,
            valuesFieldName);
        builder.AppendLine();
        AppendValuesField(
            builder,
            indent,
            type,
            typeName,
            valuesTypeName,
            valuesFieldName);
        builder.AppendLine();
        AppendValuesType(
            builder,
            indent,
            typeName,
            baseTypeName,
            listTypeName,
            valuesTypeName);
        return TypeSourceBuilder.Complete(builder, depth);
    }

    private static string GetAvailableMemberName(
        HashSet<string> memberNames,
        string name)
    {
        var nameBuilder = new StringBuilder(name);
        while (!memberNames.Add(nameBuilder.ToString()))
        {
            nameBuilder.Append('_');
        }

        return nameBuilder.ToString();
    }

    private static void AppendValuesField(
        StringBuilder builder,
        string indent,
        INamedTypeSymbol type,
        string typeName,
        string valuesTypeName,
        string valuesFieldName)
    {
        builder.Append(indent).Append("private static readonly global::System.Lazy<").Append(valuesTypeName)
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
        builder.Append(indent).Append("    return new ").Append(valuesTypeName).AppendLine("(items);");
        builder.Append(indent).AppendLine("});");
    }

    private static void AppendLookupMethods(
        StringBuilder builder,
        string indent,
        string typeName,
        string listTypeName,
        string valuesFieldName)
    {
        string methods = $$"""
            /// <summary>
            /// Gets all declared enumeration values ordered by identifier.
            /// </summary>
            /// <returns>The same immutable list of declared enumeration values on every call.</returns>
            public static {{listTypeName}} GetAll() => {{valuesFieldName}}.Value.All;

            /// <summary>
            /// Gets an enumeration value by its identifier.
            /// </summary>
            /// <param name="id">The enumeration value identifier.</param>
            /// <returns>The enumeration value with the specified identifier.</returns>
            /// <exception cref="global::System.InvalidOperationException">Thrown when the identifier is not declared.</exception>
            public static {{typeName}} FromId(int id)
            {
                if (TryFromId(id, out var item))
                {
                    return item!;
                }

                throw new global::System.InvalidOperationException(
                    $"'{id}' is not a valid id in {typeof({{typeName}}).Name}");
            }

            /// <summary>
            /// Gets an enumeration value by name after trimming surrounding whitespace.
            /// </summary>
            /// <param name="name">The enumeration value name.</param>
            /// <returns>The enumeration value with the specified name.</returns>
            /// <exception cref="global::System.InvalidOperationException">Thrown when the name is not declared.</exception>
            public static {{typeName}} FromName(string name)
            {
                if (TryFromName(name, out var item))
                {
                    return item!;
                }

                throw new global::System.InvalidOperationException(
                    $"'{name}' is not a valid name in {typeof({{typeName}}).Name}");
            }

            /// <summary>
            /// Tries to get an enumeration value by its identifier.
            /// </summary>
            /// <param name="id">The enumeration value identifier.</param>
            /// <param name="item">The matching enumeration value, or null if no value is found.</param>
            /// <returns>True if a matching value was found; otherwise, false.</returns>
            public static bool TryFromId(
                int id,
                out {{typeName}}? item)
            {
                return {{valuesFieldName}}.Value.ById.TryGetValue(id, out item);
            }

            /// <summary>
            /// Tries to get an enumeration value by name after trimming surrounding whitespace.
            /// </summary>
            /// <param name="name">The enumeration value name.</param>
            /// <param name="item">The matching enumeration value, or null if no value is found.</param>
            /// <returns>True if a matching value was found; otherwise, false.</returns>
            public static bool TryFromName(
                string name,
                out {{typeName}}? item)
            {
                item = null;

                if (string.IsNullOrWhiteSpace(name))
                {
                    return false;
                }

                string trimmedName = name.Trim();
                return {{valuesFieldName}}.Value.ByName.TryGetValue(trimmedName, out item);
            }
            """;
        AppendIndented(builder, indent, methods);
    }

    private static void AppendValuesType(
        StringBuilder builder,
        string indent,
        string typeName,
        string baseTypeName,
        string listTypeName,
        string valuesTypeName)
    {
        string source = $$"""
            private sealed class {{valuesTypeName}}
            {
                public {{valuesTypeName}}(global::System.Collections.Generic.List<{{typeName}}> items)
                {
                    var byId = new global::System.Collections.Generic.Dictionary<int, {{typeName}}>(items.Count);
                    var byName = new global::System.Collections.Generic.Dictionary<string, {{typeName}}>(
                        items.Count,
                        global::System.StringComparer.Ordinal);
                    foreach (var item in items)
                    {
                        {{baseTypeName}} value = item;
                        if (!byId.TryAdd(value.Id, item))
                        {
                            throw new global::System.InvalidOperationException(
                                $"Duplicate id '{value.Id}' in {typeof({{typeName}}).Name}");
                        }

                        if (!byName.TryAdd(value.Name, item))
                        {
                            throw new global::System.InvalidOperationException(
                                $"Duplicate name '{value.Name}' in {typeof({{typeName}}).Name}");
                        }
                    }

                    items.Sort(static (left, right) =>
                        (({{baseTypeName}})left).Id.CompareTo((({{baseTypeName}})right).Id));
                    All = items.AsReadOnly();
                    ById = global::System.Collections.Frozen.FrozenDictionary.ToFrozenDictionary(byId);
                    ByName = global::System.Collections.Frozen.FrozenDictionary.ToFrozenDictionary(
                        byName,
                        global::System.StringComparer.Ordinal);
                }

                public {{listTypeName}} All { get; }
                public global::System.Collections.Frozen.FrozenDictionary<int, {{typeName}}> ById { get; }
                public global::System.Collections.Frozen.FrozenDictionary<string, {{typeName}}> ByName { get; }
            }
            """;
        AppendIndented(builder, indent, source);
    }

    private static void AppendIndented(
        StringBuilder builder,
        string indent,
        string source)
    {
        foreach (string line in source.Replace("\r\n", "\n").Split('\n'))
        {
            builder.Append(indent).AppendLine(line);
        }
    }
}
