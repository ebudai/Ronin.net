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
            return mutability is null ? null : new ExpectedIdentifierError(ref parser);
        }

        Modifiers modifiers = null;
        Type type = null;
        if (parser.TryAdvance<Returns>())
        {
            modifiers = Modifiers.Parse(ref parser);
            type = Type.Unresolved.Parse(ref parser);
        }

        Value initializer = null;
        if (parser.TryAdvance<Assign>())
        {
            initializer = Value.Parse(ref parser);
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
        public ExpectedIdentifierError(ref Parser parser) => Tokens = Unknown.Parse(ref parser).Tokens;

        public string Reason { get; } = "expected identifier";
        public ReadOnlyMemory<Token> Tokens { get; init; }
    }    
}