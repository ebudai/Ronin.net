// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;
using System;

namespace Ronin.Grammar;

/// <summary>
///     A singular item of data residing in memory
/// </summary>
/// 
/// <example>
///     datatype Building
///     {
///         var floors => number;
///         ↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑
///     }
///     
///     function do stuff(x => number, y => date) { }
///                       ↑↑↑↑↑↑↑↑↑↑↑  ↑↑↑↑↑↑↑↑↑
/// </example>
internal class Datum : Member
{
    public Mutability Mutability { get; init; }
    public Type Type { get; set; }
    public Value Initializer { get; set; }

    /// <summary>
    ///     A datum in statement position, where a bare identifier is an
    ///     expression rather than a declaration.
    /// </summary>
    public static new Datum Parse(ref Parser current) => Parse(ref current, declaring: true);

    /// <summary>
    ///     A datum in parameter position, where the statement guard does not
    ///     apply.
    /// </summary>
    ///
    /// <remarks>
    ///     The guard below is not misplaced, it is position-specific: in a body
    ///     it is what keeps «order = 3» an assignment rather than a declaration,
    ///     so relaxing it there would reinterpret every assignment in the
    ///     language. A parameter has no such competition — «(the ball)» and
    ///     «(order = 3)» are declarations and nothing else — so it needs its own
    ///     path rather than a loosening of the shared one.
    /// </remarks>
    public static Datum Parameter(ref Parser current) => Parse(ref current, declaring: false);

    private static Datum Parse(ref Parser current, bool declaring)
    {
        Parser parser = current;

        parser.TryAdvance<Mutability>(out var mutability);

        if (Identifier.Parse(ref parser) is not Identifier identifier)
        {
            if (mutability is null) return null;

            // «var +;» consumed «var» and nothing else, and the error node was
            // built from the LOCAL parser — so the caller was left sitting on
            // «var» and Module.Parse asked for the same statement forever
            return new ExpectedIdentifierError { Tokens = Parser.Recover(ref current, parser) };
        }

        Modifiers modifiers = null;
        Type type = null;
        if (parser.TryAdvance<Returns>())
        {
            modifiers = Modifiers.Parse(ref parser);
            type = Type.Unresolved.Parse(ref parser);

            // A consumed «=>» commits to a type — but only once the statement
            // has announced what it is. «function» and «type» announce
            // themselves with a keyword of their own and so are committed
            // already; a datum does it with «var», «let» or «constant». Without
            // one, «reactive => 44.3» is a production that does not match and
            // has to let the next one try, while «var x => = 1» is a
            // declaration whose type was started and abandoned.
            if (type is null && mutability is not null)
            {
                return new ExpectedTypeError { Tokens = Parser.Recover(ref current, parser) };
            }
        }

        Value initializer = null;
        if (parser.TryAdvance<Assign>())
        {
            initializer = Value.Parse(ref parser);

            // and a consumed «=» commits to a value, on the same condition
            if (initializer is null && mutability is not null)
            {
                return new ExpectedValueError { Tokens = Parser.Recover(ref current, parser) };
            }
        }

        if (declaring && type is null)
        {
            if (mutability is null || initializer is null) return null;
        }

        current = parser;
        return new Datum
        {
            Mutability = mutability,
            Identifier = identifier,
            Modifiers = modifiers ?? new(),
            Type = type,
            Initializer = initializer
        };
    }

    public new class Unresolved : Datum
    {
        public Reference Reference { get; set; }

        public static new Datum Parse(ref Parser parser) => Reference.Parse(ref parser) is Reference reference ? new Unresolved { Reference = reference } : null;
    }

    public class ExpectedIdentifierError : Datum, IError
    {
        public string Reason { get; } = "expected identifier";
        public ReadOnlyMemory<Token> Tokens { get; init; }
    }

    public class ExpectedTypeError : Datum, IError
    {
        public string Reason { get; } = $"expected a type after '{Returns.symbol}'";
        public ReadOnlyMemory<Token> Tokens { get; init; }
    }

    public class ExpectedValueError : Datum, IError
    {
        public string Reason { get; } = $"expected a value after '{Assign.symbol}'";
        public ReadOnlyMemory<Token> Tokens { get; init; }
    }
}
