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

    /// <remarks>
    ///     Every caller but one wants a reference or nothing, so this consumes
    ///     nothing when it finds a lone anonymous value — see the overload, which
    ///     hands that value back rather than making the caller build it again.
    /// </remarks>
    public static Reference Parse(ref Parser current)
    {
        Parser parser = current;

        var reference = Parse(ref parser, out var alone);

        if (alone is not null) return null;

        current = parser;
        return reference;
    }

    /// <param name="alone">
    ///     The single anonymous value this turned out to be, already parsed and
    ///     consumed.
    /// </param>
    ///
    /// <remarks>
    ///     A lone anonymous value is a value and not a reference to one, and the
    ///     caller wants it as a value — so it is handed over rather than
    ///     discarded. Discarding it meant the whole thing was parsed twice, once
    ///     inside the rejected reference and once as the value: every list,
    ///     lookup, input block and delegate body walked and built twice. That is
    ///     not only duplicate work — a speculative aggregate spends from the group
    ///     budget that bounds adversarial backtracking, and the budget
    ///     deliberately does not roll back.
    /// </remarks>
    public static Reference Parse(ref Parser current, out Temporary alone)
    {
        alone = null;

        Parser parser = current;

        if (current.Token is Keyword) return null;

        var components = parser.ParseRepeating<Component>();
        if (components.Count is 0) return null;

        if (components.Count is 1 && components[0].AsTemporary is Temporary only)
        {
            alone = only;
            current = parser;
            return null;
        }

        // What may follow a WORD is unconstrained: an anonymous value after a
        // word is an argument, which is why «thing 7 ("stuff")» has two of them
        // in a row and is one call.
        //
        // An anonymous value LEADING is the constrained case, and it was not
        // constrained at all — it was merely required to have a name somewhere
        // later, which any word supplied. So «{ 1 } { 2 }» was refused and
        // «{ 1 } { 2 } name» was one reference, and the missing separator §4.6
        // asks for could be bought with a trailing word.
        if (components[0].AsTemporary is not null && Leads(components) is false) return null;

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
    ///     Whether an anonymous value may lead these components.
    /// </summary>
    ///
    /// <remarks>
    ///     Two continuations and no others. An INDEXER attaches to the value
    ///     before it, so «{ 1, 2 } [0]» is one reference; a SYMBOL is an operator
    ///     with the value as its left operand, so «3..test» is one too. Anything
    ///     else after a leading value is a second value, and two of those side by
    ///     side need the separator §4.6 asks for.
    ///     <para>
    ///     Never asked of a lone value: that is not a reference at all, and the
    ///     caller has already been handed it as a value.
    ///     </para>
    /// </remarks>
    private static bool Leads(List<Component> components)
    {
        if (components[1].AsSymbolic is not null) return true;

        for (var at = 1; at < components.Count; ++at)
        {
            if (components[at].AsTemporary is not Index) return false;
        }

        return true;
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
