// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Ronin.Grammar;

/// <summary>
///     Represents a named indirection to a <see cref="Datum"/>, <see cref="Function"/>, <see cref="Type"/> or <see cref="Value"/>
/// </summary>
internal class Reference : IEnumerable<Reference.Component>
{
    public ReadOnlySpan<Component> Span => CollectionsMarshal.AsSpan(Components);

    private List<Component> Components { get; init; }

    public static Reference Parse(ref Parser current)
    {
        Parser parser = current;

        if (current.Token is Keyword) return null;

        var components = parser.ParseRepeating<Component>();
        if (components.Count is 0) return null;
        if (components.Count is 1 && components[0].AsTemporary is Literal) return null;
        current = parser;
        return new Reference { Components = components };
    }

    public IEnumerator<Component> GetEnumerator() => Components.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => Components.GetEnumerator();

    public Component this[int i] => Components[i];

    public int Count => Components.Count;

    public void Add(Name name) => Components.Add(name);
    public void Add(IEnumerable<Component> components) => Components.AddRange(components);

    public System.Index[] IndicesOf(Name name)
    {
        if (name is null) return null;

        List<System.Index> indices = new();

        for (var i = 0; i != Count; ++i)
        {
            if (name.Equals(this[i].AsName))
            {
                indices.Add(i);
            }
        }

        return indices.ToArray();
    }

    public System.Index[] IndicesOf(Parameters parameters)
    {
        if (parameters.Mandatory.Length is 0 or 1) return null;

        List<System.Index> indices = new();

        for (var i = 0; i != Count; ++i)
        {
            if (this[i].AsTemporary is not Inputs inputs) continue;

            if (inputs.Count >= parameters.Mandatory.Length && inputs.Count <= parameters.Count)
            {
                indices.Add(i);
            }
        }

        return indices.ToArray();
    }

    public class Component : Compiler.IParsable<Component>
    {
        private Component(Name name) => value = name;
        private Component(Temporary temporary) => value = temporary;

        public static implicit operator Component(Name name) => new(name);
        public static implicit operator Component(Temporary value) => new(value);

        public static Component Parse(ref Parser current)
        {
            if (Name.Parse(ref current) is Name name) return name;
            if (Temporary.Parse(ref current) is Temporary temporary) return temporary;
            return null;
        }
        
        public Name AsName => value as Name;
        public Temporary AsTemporary => value as Temporary;

        private readonly object value;
    }
}