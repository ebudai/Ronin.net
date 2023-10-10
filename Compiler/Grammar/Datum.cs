// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

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
    public Value Initializer { get; init; }

    public static new Datum Parse(ref Parser current)
    {
        Parser parser = current;

        var mutability = parser.Token as Mutability;
        if (mutability is not null) parser.Advance();

        if (Name.Parse(ref parser) is not Name name)
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

        if (type is null)
        {
            if (mutability is null || initializer is null) return null;
        }

        current = parser;
        return new Datum
        {
            Mutability = mutability,
            Identifier = new() { Components = { name } },
            Modifiers = modifiers ?? new(),
            Type = type,
            Initializer = initializer
        };
    }

    [ExcludeFromCodeCoverage]
    public override void ResolveReferences(Scope context)
    {
        Identifier.ResolveReferences(context);
        if (Type is Type.Unresolved unresolved)
        {
            var member = context.Find(unresolved.Reference);
            Type = member as Type ?? new Type.Calculated { Member = member };
        }
        Initializer.ResolveReferences(context);
    }

    public new class Unresolved : Datum
    {
        public Reference Reference { get; set; }

        public static new Datum Parse(ref Parser parser) => Reference.Parse(ref parser) is Reference reference ? new Unresolved { Reference = reference } : null;
    }

    [ExcludeFromCodeCoverage]
    public class Calculated : Datum
    {
        public Member Member { get; init; }
    }

    public class ExpectedIdentifierError : Datum, IError
    {
        public ExpectedIdentifierError(ref Parser parser) => Tokens = Unknown.Parse(ref parser).Tokens;

        public string Reason { get; } = "expected identifier";
        public System.ReadOnlyMemory<Token> Tokens { get; init; }
    }
}
