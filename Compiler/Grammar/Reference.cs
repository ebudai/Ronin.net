// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ronin.Grammar;

/// <summary>
///     Represents a named indirection to a <see cref="Datum"/>, <see cref="Function.Declaration"/>, <see cref="Datatype.Declaration"/> or <see cref="Value"/>
/// </summary>
internal class Reference : Syntax, IParsableSyntax<Reference>
{
    public class Component : CompositeSyntax<Component, Name, AnonymousValue>
    {
        public static implicit operator Component(Name name) => new() { value = name, Source = name.Source };
        public static implicit operator Component(AnonymousValue value) => new() { value = value, Source = value.Source };
    }

    public List<Component> Components { get; init; }
    public Indexer Indexer { get; init; }

    public static Reference Parse(ref Parser current)
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

    public override bool Equals(object obj)
    {
        if (obj is not Reference reference) return false;
        return reference.Components.SequenceEqual(Components) && reference.Indexer.Equals(Indexer);
    }

    public override int GetHashCode()
    {
        HashCode hashcode = new();
        foreach (var component in Components) hashcode.Add(component);
        hashcode.Add(Indexer);
        return hashcode.ToHashCode();
    }

    public class Unresolved : Statement, IParsableSyntax<Unresolved>
    {
        public Context.Member Member { get; set; }
        public List<Inputs> Inputs { get; } = new();

        public static new Unresolved Parse(ref Parser current)
        {
            Parser parser = current;

            if (Reference.Parse(ref parser) is not Reference reference) return null;

            return new Unresolved
            {
                Member = new Context.Member.Unresolved { Reference = reference },
                Source = parser.Commit(ref current)
            };
        }
    }
}