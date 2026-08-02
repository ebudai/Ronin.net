// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ronin.Grammar;
/// <summary>
///     Restricts a <see cref="Datum"/> to a particular shape of data
///     resulting from evaluation of a <see cref="Function"/> or <see cref="Association"/>.
/// </summary>
/// 
/// <example>
///     datatype Car = Vehicle and { var speed => number; var price => money; }
/// </example>

internal class Type : Member
{
    public Algebra Algebra { get; set; }    
    public Definition Members { get; init; }
    
    public static new Type Parse(ref Parser current)
    {
        Parser parser = current;

        var modifiers = Modifiers.Parse(ref parser);

        if (parser.TryAdvance<Lexicon.Type>() is false) return null;

        if (Identifier.Parse(ref parser) is not Identifier name) return null;

        Algebra algebra = null;
        if (parser.TryAdvance<Assign>())
        {
            // A consumed «=» commits to an algebra, so «type T = ;» is a type
            // whose definition was started and abandoned rather than one of the
            // plain types the language also has.
            if (Heading.Of(ref parser, Algebra.Unresolved.Parse) is not Algebra declared)
            {
                return new ExpectedAlgebraError { Tokens = Parser.Recover(ref current, parser) };
            }

            algebra = declared;
        }

        var definition = Definition.Parse(ref parser);

        // Recognised in order to be REFUSED, which is diagnostics and not
        // semantics: it adds a message and no node, no lifetime, and no
        // instance. A «when» in a type is designed — it lives as long as the
        // instance does — and blocked on the instance binding model, which is
        // not built. For a language whose diagnostics are the teaching
        // mechanism, "designed and not implemented" and "I cannot read this"
        // must not look the same to someone who understood the design and wrote
        // it correctly.
        if (definition is null)
        {
            Parser reading = parser;

            // The FIRST element the member aggregate could not take, and only
            // then whether it is a «when». Asking whether the body holds one
            // anywhere blamed the «when» in «type Box { if ready { … } when
            // ready { … } }», where the «if» is the invalid member and comes
            // first — so removing the diagnosed «when» left the original failure
            // untouched.
            // A parse-error node for a «when» is a reactive Scope too, and has
            // no keyword to point at — «type Box { when { return 1; } }» reached
            // here, carried a null token into the finding, and took the compiler
            // out on it. Recognising a construct in order to refuse it well
            // requires having recognised one: a «when» nobody could parse is
            // ordinary malformed input and says so.
            if (Loose.Parse(ref reading) is Loose body
                && body.FirstOrDefault(element => element is not Member) is Scope reactive
                && reactive is not IError
                && reactive.Reacts
                && reactive.Opened is not null)
            {
                return new ReactiveMemberError
                {
                    Opened = reactive.Opened,
                    Tokens = Parser.Recover(ref current, reading),
                };
            }
        }

        current = parser;
        return new Type
        {
            Modifiers = modifiers,
            Identifier = name,
            Algebra = algebra,
            Members = definition
        };
    }

    public class Definition : Aggregate<Definition, Open.Brace, Member, Terminal, Close.Brace>
    {

    }

    /// <summary>
    ///     A type body read loosely, so that what it holds can be named.
    /// </summary>
    ///
    /// <remarks>
    ///     Never becomes part of the tree. <see cref="Definition"/> takes members
    ///     and a «when» is not one, so the body simply failed to parse and every
    ///     such type came back as unexpected input — the same message as a stray
    ///     bracket. This reads the same span again with the element loosened, for
    ///     the sole purpose of telling the two apart.
    /// </remarks>
    private class Loose : Aggregate<Loose, Open.Brace, Statement, Terminal, Close.Brace>
    {

    }

    /// <summary>A «when» declared inside a type, which is not built yet.</summary>
    public class ReactiveMemberError : Type, IError
    {
        /// <summary>The «when» keyword, which is what a message points at.</summary>
        public Token Opened { get; init; }

        public string Reason { get; } = "a «when» inside a type is not implemented";
        public ReadOnlyMemory<Token> Tokens { get; init; }
    }

    public class ExpectedAlgebraError : Type, IError
    {
        public string Reason { get; } = $"expected a type after '{Assign.symbol}'";
        public System.ReadOnlyMemory<Token> Tokens { get; init; }
    }

    public new class Unresolved : Type
    {
        public Reference Reference { get; init; }

        public static new Type Parse(ref Parser current) => Reference.Parse(ref current) is Reference reference ? new Unresolved { Reference = reference } : null;
    }
}

internal class Algebra : Type
{
    public List<Type> Bases { get; } = [];
    public List<Type> Unions { get; } = [];

    public new class Unresolved : Algebra
    {
        public Reference Reference { get; init; }

        public static new Algebra Parse(ref Parser current) => Reference.Parse(ref current) is Reference reference ? new Unresolved { Reference = reference } : null;
    }
}
