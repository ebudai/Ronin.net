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

        // A lone anonymous value is a value and not a reference to one. Two of
        // them may be — «3..test» leads with a literal — so this is about the
        // count and not about the leading component's kind.
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

    /// <summary>
    ///     Whether this is an anonymous value and its indexer, which §4.7 admits
    ///     beside a run of words.
    /// </summary>
    ///
    /// <remarks>
    ///     Exactly two, and the second an indexer. Admitting any run of anonymous
    ///     values instead would make «{ 1 } { 2 }» one reference, and two values
    ///     with no separator between them is what the aggregate rule exists to
    ///     refuse.
    /// </remarks>
    public bool IsIndexed => Components.Count is 2
                          && Components[0].AsTemporary is not null
                          && Components[1].AsTemporary is Index;

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

        /// <remarks>
        ///     <para>
        ///     A name, UNLESS an arrow follows it, in which case it is a
        ///     delegate's parameter and not a component. «x =&gt; { … }» is a
        ///     delegate, and taking the «x» as a whole component orphaned the
        ///     arrow — so a bare delegate could never be a reference component,
        ///     and «x =&gt; { … } [0]» became the reference «x» with the rest left
        ///     to whatever came next.
        ///     </para>
        ///     <para>
        ///     One token of lookahead and not a speculative parse. Trying
        ///     <c>Temporary</c> first works — no ordinary name parses as one — but
        ///     it speculates the whole delegate production for every component of
        ///     every reference, and each speculative aggregate spends from the
        ///     group budget that bounds adversarial backtracking. A name followed
        ///     by an arrow is the only case that needs to give way, and that is a
        ///     question about the next token.
        ///     </para>
        ///     <para>
        ///     Symbolic goes last: no Temporary begins with a symbol that is not
        ///     punctuation, but if one ever does it should still win the value.
        ///     </para>
        /// </remarks>
        public static Component Parse(ref Parser current)
        {
            Parser ahead = current;

            if (Name.Parse(ref ahead) is Name leading && ahead.Token is not Returns)
            {
                current = ahead;
                return leading;
            }

            if (Temporary.Parse(ref current) is Temporary temporary) return temporary;

            // The arrow was there and the delegate did not want it — «x => Number»
            // has no body — so the words are an ordinary component after all.
            if (Name.Parse(ref current) is Name name) return name;
            if (Symbolic.Parse(ref current) is Symbolic symbolic) return symbolic;
            return null;
        }

        public Name AsName => value as Name;
        public Temporary AsTemporary => value as Temporary;
        public Symbolic AsSymbolic => value as Symbolic;

        private readonly object value;
    }
}
