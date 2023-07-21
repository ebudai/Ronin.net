// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Grammar.Compound;

namespace Ronin.Grammar;

/// <summary>
///     Represents a named indirection to a <see cref="Datum"/>, <see cref="FunctionDeclaration"/>, <see cref="DatatypeDeclaration"/> or <see cref="Value"/>
/// </summary>
internal class Reference : Value, IParsableSyntax<Reference>
{
    public List<Component> Components { get; init; }
    public Indexer Ordinal { get; init; }

    public new static Reference Parse(ref Parser current)
    {
        Parser parser = current;

        var components = parser.ParseRepeating<Component>();
        if (components.Count is 0) return null;
        foreach (var component in components)
        {
            if (component.value is not AnonymousValue)
            {
                var ordinal = Indexer.Parse(ref parser);

                return new Reference
                {
                    Components = components,
                    Ordinal = ordinal,
                    Source = parser.Commit(ref current)
                };
            }
        }

        return null;
    }

    public class Component : CompositeSyntax<Component, Name, AnonymousValue> { }
}