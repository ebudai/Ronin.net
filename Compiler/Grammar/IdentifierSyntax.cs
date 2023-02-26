// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Grammar.Aggregates;
using Ronin.Lexicon;

namespace Ronin.Grammar;

/// <summary>
///     A unique name for a <see cref="DatatypeDeclarationSyntax"/> or a <see cref="FunctionDeclarationSyntax"/>
///     which can contain multiple <see cref="Word"/>s and <see cref="Parameters"/>
/// </summary>
internal class IdentifierSyntax : Syntax, Compiler.IParsable<IdentifierSyntax>
{
    public List<Component> Components { get; init; }

    public static IdentifierSyntax Parse(ref Parser context)
    {
        Parser parser = context;

        var components = parser.ParseRepeating<Component>();
        if (components.Count is 0) return null;

        return new IdentifierSyntax { Components = components, Source = parser.Commit(ref context) };
    }

    public class Component : CompositeSyntax<Component, Name, Parameters> { }
}
