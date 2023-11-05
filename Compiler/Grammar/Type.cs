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

        Algebra algebra = parser.TryAdvance<Assign>() ? Algebra.Unresolved.Parse(ref parser) : null;

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

    public new class Unresolved : Type
    {
        public Reference Reference { get; init; }

        public static new Type Parse(ref Parser current)
            => Reference.Parse(ref current) is not Reference reference 
                ? null
                : new Unresolved { Reference = reference };

        public Type Resolve(IContext context) => context.Resolve(Reference) switch
        {
            Resolution.Definite definite => definite.Member as Type ?? new Calculated { Resolution = definite },
            Resolution.Ambiguous ambiguous => new Overloaded { Candidates = ambiguous.Candidates },
            _ => null
        };
    }

    public class Overloaded : Type
    {
        public List<Resolution> Candidates { get; init; }
    }

    internal class Calculated : Type
    {
        public Resolution Resolution { get; init; }
    }
}

internal class Algebra : Type
{
    public List<Type> Bases { get; } = new();
    public List<Type> Unions { get; } = new();

    public new class Unresolved : Algebra
    {
        public Reference Reference { get; init; }

        public static new Algebra Parse(ref Parser current)
            => Reference.Parse(ref current) is not Reference reference
                ? null
                : new Unresolved { Reference = reference };
    }

    internal new class Overloaded : Algebra
    {
        public List<Resolution> Candidates { get; init; }
    }

    internal new class Calculated : Algebra
    {
        public Resolution Resolution { get; init; }
    }
}

internal class Calculation : Statement
{
    public Member Member { get; init; }
    public Type Owner { get; init; }
}