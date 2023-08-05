// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Grammar;

/// <summary>
///     Represents a named indirection to a <see cref="Datum"/>, <see cref="Function.Declaration"/>, <see cref="Datatype.Declaration"/> or <see cref="Value"/>
/// </summary>
internal class Reference : Statement, IParsableSyntax<Reference>
{
    public List<Component> Components { get; init; }
    public Indexer Indexer { get; init; }

    public new static Reference Parse(ref Parser current)
    {
        Parser parser = current;

        var components = parser.ParseRepeating<Component>();
        if (components.Count is 0) return null;
        foreach (var component in components)
        {
            if (component.value is not AnonymousValue)
            {
                var indexer = Indexer.Parse(ref parser);

                return new Reference
                {
                    Components = components,
                    Indexer = indexer,
                    Source = parser.Commit(ref current)
                };
            }
        }

        return null;
    }

    [ExcludeFromCodeCoverage]
    public override bool Equals(object obj)
    {
        if (obj is not Reference reference) return false;
        return reference.Components.SequenceEqual(Components) && reference.Indexer.Equals(Indexer);
    }

    public override int GetHashCode() => Components.ToHashCode(Indexer);

    public class Component : CompositeSyntax<Component, Name, AnonymousValue> { }
}