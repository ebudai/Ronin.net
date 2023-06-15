// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Grammar.Compound;

namespace Ronin.Grammar;

/// <summary>
///     Represents a named indirection to a <see cref="DatumDeclaration"/>, <see cref="FunctionDeclaration"/>, <see cref="DatatypeDeclaration"/> or <see cref="Value"/>
/// </summary>
internal class Reference : Value, IParsableSyntax<Reference>
{
    public List<Component> Components { get; init; }
    public Ordinal Ordinal { get; init; }

    public new static Reference Parse(scoped ref Parser current)
    {
        Parser parser = current;

        var components = parser.ParseRepeating<Component>();
        if (components.Count is 0) return null;
        if (components.All(component => component.value is Anonymous)) return null;        

        var ordinal = Ordinal.Parse(ref parser);

        return new Reference
        {
            Components = components,
            Ordinal = ordinal,
            Source = parser.Commit(ref current)
        };
    }

    public class Component : CompositeSyntax<Component, Words, Anonymous> { }
}