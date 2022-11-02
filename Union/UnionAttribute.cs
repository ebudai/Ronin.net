using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Ronin.Union;

[Generator]
public class UnionAttribute : Attribute, ISourceGenerator
{
    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxReceiver is not UnionAttributedSyntaxReceiver receiver)
        {
            ReportBadSyntaxReceiver(context);
            return;
        }

        StringBuilder source = new();

        foreach (var union in receiver.Unions)
        {
            AttributeSyntax attribute = union.AttributeLists.SelectMany(lists => lists.Attributes).FirstOrDefault(IsUnionAttribute);

            if (attribute is null)
            {
                ReportNotAUnion(context, union);                
                return;
            }

            var types = (attribute.Name as GenericNameSyntax)?
                .TypeArgumentList
                .Arguments
                .ToList();

            var @namespace = GetNamespace(union);
            var outers = GetClassnameHierarchy(union);
            var properties = types.Select(GenerateProperty).ToList();
            var constructors = types.Select(type => GenerateConstructor(union.Identifier.Text, type)).ToList();
            var conversions = types.Select(type => GenerateConversions(union.Identifier.Text, type)).ToList();
            string closingBraces = new('}', outers.Count);

            source.Clear();

            source.Append("namespace ").Append(@namespace).AppendLine(";");

            foreach (var outer in outers) source.Append(outer).AppendLine(" {");
            foreach (var property in properties) source.Append(property);
            foreach (var constructor in constructors) source.Append(constructor);
            foreach (var conversion in conversions) source.Append(conversion);

            source.AppendLine("private object _storage;");

            source.Append(closingBraces);

            var classname = outers.Last().Split(' ').Last();

            context.AddSource($"{Guid.NewGuid()}.g.cs", source.ToString());
        }
    }

    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => s_receiver);
    }

    internal const string s_name = "Union<";

    internal static bool IsUnionAttribute(AttributeSyntax attribute) => attribute.Name.ToString().StartsWith(s_name);

    private static string GenerateProperty(TypeSyntax syntax)
    {
        const string template = """
            public {0} {1}
            {{
                get => _storage as {0};
                set => _storage = value;
            }}

            """;

        var propertyName = syntax.ToString();
        if (propertyName.EndsWith("?") is false) propertyName += '?';
        var typeName = GetName(syntax);
        return string.Format(template, propertyName, typeName);
    }

    private static string GenerateConstructor(string union, TypeSyntax type) => $"private {union}({type} value) => {GetName(type)} = value;\n";

    private static string GenerateConversions(string union, TypeSyntax type)
    {
        var name = type.ToString();
        if (name.EndsWith("?") is false) name += '?';

        var from = $"public static implicit operator {union}({type} value) => new(value);\n";
        var to = $"public static implicit operator {name}({union} value) => value.{GetName(type)};\n";

        return from + to + "\n";
    }

    private static string GetName(TypeSyntax syntax) => syntax switch
    {
        IdentifierNameSyntax name => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name.Identifier.Text),
        _ => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(syntax.ToString().Replace("?", "")),
    };

    private static string GetNamespace(ClassDeclarationSyntax @class)
    {
        var parent = @class.Parent;
        while (parent is not NamespaceDeclarationSyntax) parent = parent!.Parent;
        var @namespace = parent as NamespaceDeclarationSyntax;
        return @namespace!.Name.ToString();
    }

    private static List<string> GetClassnameHierarchy(ClassDeclarationSyntax @class)
    {
        List<string> declarations = new();
        SyntaxNode? parent = @class;
        while (parent is ClassDeclarationSyntax syntax)
        {
            var declaration = string.Empty;
            foreach (var modifier in syntax.Modifiers) declaration += modifier.Text + ' ';
            declaration += syntax.Keyword.Text + ' ';
            declaration += syntax.Identifier.Text;
            declarations.Add(declaration);
            parent = parent.Parent;
        }
        declarations.Reverse();
        return declarations;
    }

    private static Diagnostic ReportDeveloperMistake(string id, string message, string argument)
    {
        const string description = "This is the library author's fault.  Please report this to the maintainer.";
        const string url = "https://github.com/ebudai/Union/issues/new";
        DiagnosticDescriptor descriptor = new(id, message, argument, "Union.Setup", DiagnosticSeverity.Error, true, description, url);
        return Diagnostic.Create(descriptor, null);
    }

    private static void ReportBadSyntaxReceiver(GeneratorExecutionContext context)
    {
        const string message = $"Bad Setup - expected receiver to be a {nameof(UnionAttributedSyntaxReceiver)} but context.SyntaxReceiver is {{0}}";
        var diagnostic = ReportDeveloperMistake("UN0001", message, context.SyntaxReceiver?.GetType().Name ?? "null");
        context.ReportDiagnostic(diagnostic);
    }

    private static void ReportNotAUnion(GeneratorExecutionContext context, ClassDeclarationSyntax? union)
    {
        const string message = $"Bad Setup - processed a union that has no union attribute - {{0}}";
        var diagnostic = ReportDeveloperMistake("UN0002", message, union?.Identifier.Text ?? "null");
        context.ReportDiagnostic(diagnostic);
    }

    private static readonly UnionAttributedSyntaxReceiver s_receiver = new();
}

public class UnionAttribute<T0, T1> : UnionAttribute { }
public class UnionAttribute<T0, T1, T2> : UnionAttribute { }
public class UnionAttribute<T0, T1, T2, T3> : UnionAttribute { }
public class UnionAttribute<T0, T1, T2, T3, T4> : UnionAttribute { }
public class UnionAttribute<T0, T1, T2, T3, T4, T5> : UnionAttribute { }
public class UnionAttribute<T0, T1, T2, T3, T4, T5, T6> : UnionAttribute { }
public class UnionAttribute<T0, T1, T2, T3, T4, T5, T6, T7> : UnionAttribute { }
public class UnionAttribute<T0, T1, T2, T3, T4, T5, T6, T7, T8> : UnionAttribute { }
public class UnionAttribute<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> : UnionAttribute { }
public class UnionAttribute<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> : UnionAttribute { }
public class UnionAttribute<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> : UnionAttribute { }
public class UnionAttribute<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> : UnionAttribute { }
public class UnionAttribute<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> : UnionAttribute { }
public class UnionAttribute<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> : UnionAttribute { }
public class UnionAttribute<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> : UnionAttribute { }

internal class UnionAttributedSyntaxReceiver : ISyntaxReceiver
{
    internal List<ClassDeclarationSyntax> Unions { get; } = new();

    public void OnVisitSyntaxNode(SyntaxNode node)
    {
        if (node is not ClassDeclarationSyntax @class) return;
        var isUnion = @class.AttributeLists.SelectMany(list => list.Attributes).Any(UnionAttribute.IsUnionAttribute);
        if (isUnion) Unions.Add(@class);
    }
}