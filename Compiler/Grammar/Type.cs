// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;
using System.Collections.Generic;

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
            if (Algebra.Unresolved.Parse(ref parser) is not Algebra declared)
            {
                return new ExpectedAlgebraError { Tokens = Parser.Recover(ref current, parser) };
            }

            algebra = declared;
        }

        var definition = Definition.Parse(ref parser);

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
