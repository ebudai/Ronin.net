// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Grammar.Compound;

namespace Ronin.Grammar;

/// <summary>
///     A unique name for a <see cref="DatatypeDeclaration"/> or a <see cref="FunctionDeclaration"/>
///     which can contain multiple <see cref="Words"/> and <see cref="Parameters"/>
/// </summary>
internal class Name : Syntax, IParsableSyntax<Name>
{
    public List<Component> Components { get; init; } = new();

    public Name() { }

    public Name(Words words) => Components.Add(new Component { value = words });

    public static Name Parse(scoped ref Parser current)
    {
        Parser parser = current;

        var components = parser.ParseRepeating<Component>();
        if (components.Count is 0) return null;

        return new Name { Components = components, Source = parser.Commit(ref current) };
    }

    public class Component : CompositeSyntax<Component, Words, Parameters> { }
}
