// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;
using System.Collections.Generic;

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
    public Identifier Identifier { get; init; }
    public Modifiers Modifiers { get; init; }
    public Type Type { get; set; }
    public Value Initializer { get; init; }

    public static new Datum Parse(ref Parser current)
    {
        Parser parser = current;

        var mutability = parser.Token as Mutability;
        if (mutability is not null) parser.Advance();

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
        if (parser.TryAdvance<Assign>()) initializer = Value.Parse(ref parser);

        if (type is null)
        {
            if (mutability is null || initializer is null) return null;
        }

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

        public Dictionary<string, object> Data { get; }
        public string Reason { get; } = "expected identifier";
        public System.ReadOnlyMemory<Token> Tokens { get; init; }
    }
}
