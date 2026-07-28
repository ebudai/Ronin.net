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
        if (components.Count is 1 && components[0].AsTemporary is not null) return null;

        // symbols punctuate a reference, they are never the whole of one
        if (components.TrueForAll(component => component.AsSymbolic is not null)) return null;

        current = parser;
        return new Reference { Components = components };
    }

    public IEnumerator<Component> GetEnumerator() => Components.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => Components.GetEnumerator();

    public Component this[int i] => Components[i];

    public class Component : Compiler.IParsable<Component>
    {
        private Component(Name name) => value = name;
        private Component(Temporary temporary) => value = temporary;
        private Component(Symbolic symbolic) => value = symbolic;

        public static implicit operator Component(Name name) => new(name);
        public static implicit operator Component(Temporary value) => new(value);
        public static implicit operator Component(Symbolic symbolic) => new(symbolic);

        // Symbolic goes last: no Temporary begins with a symbol that is not
        // punctuation, but if one ever does it should still win the value.
        public static Component Parse(ref Parser current)
        {
            if (Name.Parse(ref current) is Name name) return name;
            if (Temporary.Parse(ref current) is Temporary temporary) return temporary;
            if (Symbolic.Parse(ref current) is Symbolic symbolic) return symbolic;
            return null;
        }

        public Name AsName => value as Name;
        public Temporary AsTemporary => value as Temporary;
        public Symbolic AsSymbolic => value as Symbolic;

        private readonly object value;
    }
}