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

    /// <summary>The first token of the span, and the one parsing stopped at.</summary>
    private Token Start { get; init; }
    private Token End { get; init; }

    /// <summary>
    ///     The span as the resolver takes it.
    /// </summary>
    ///
    /// <remarks>
    ///     This is the join between the two halves of the frontend. The parser
    ///     decides where a statement's expression begins and ends — it knows
    ///     because punctuation bounds it — and hands the run over without having
    ///     decided anything about its shape. What the words mean is the resolver's
    ///     question, and it needs the whole span to answer it, since a name is a
    ///     span rather than a token.
    /// </remarks>
    public List<Lexeme> ToLexemes() => Start.ToLexemes(End);

    public static Reference Parse(ref Parser current)
    {
        Parser parser = current;

        if (current.Token is Keyword) return null;

        var components = parser.ParseRepeating<Component>();
        if (components.Count is 0) return null;
        if (components.Count is 1 && components[0].AsTemporary is not null) return null;

        // symbols punctuate a reference, they are never the whole of one
        if (components.TrueForAll(component => component.AsSymbolic is not null)) return null;

        Reference reference = new()
        {
            Components = components,
            Start = current.Token,
            End = parser.Token,
        };

        current = parser;
        return reference;
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
