// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Grammar.Compound;
using Ronin.Lexicon;

namespace Ronin.Grammar;

/// <summary>
///     A unique name for a <see cref="DatatypeDeclaration"/> or a <see cref="FunctionDeclaration"/>
///     which can contain multiple <see cref="Word"/>s and <see cref="Parameters"/>
/// </summary>
internal class Identifier : Syntax, IParsableSyntax<Identifier>
{
    public List<Component> Components { get; init; }

    public static Identifier Parse(ref Parser current)
    {
        Parser parser = current;

        var components = parser.ParseRepeating<Component>();
        if (components.Count is 0) return null;

        return new Identifier { Components = components, Source = parser.Commit(ref current) };
    }

    public class Component : CompositeSyntax<Component, Name, Parameters> { }
}
