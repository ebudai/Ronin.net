// Copyright © 2023 Eric Budai

using OneOf;
using Ronin.Compiler;
using Ronin.Lexicon;
using System.Collections.Generic;

namespace Ronin.Grammar;
/// <summary>
///     Restricts a <see cref="Datum"/> to a particular shape of data
///     resulting from evaluation of a <see cref="Function.Declaration"/> or <see cref="Datum"/>
/// </summary>
/// 
/// <example>
///     datatype Car = Vehicle and { var speed => number; var price => money; }
/// </example>

internal class Type : Member
{
    public Modifiers Modifiers { get; init; }
    public Identifier Identifier { get; init; }
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

    public class Definition : Aggregate<Definition, OpenBrace, Member, Terminal, CloseBrace> { }

    public new class Unresolved : Type
    {
        public Reference Reference { get; init; }

        public static new Type Parse(ref Parser current)
            => Reference.Parse(ref current) is not Reference reference 
                ? null
                : new Unresolved { Reference = reference };
    }

    internal class Overloaded : Type
    {
        public List<Resolution> Overloads { get; init; }
    }

    internal class Calculated : Type
    {
        public Member Member { get; init; }
    }
}

internal class Algebra
{
    public List<Type> Bases { get; } = new();
    public List<Type> Unions { get; } = new();

    public class Unresolved : Algebra
    {
        public Reference Reference { get; init; }

        public static Algebra Parse(ref Parser current)
            => Reference.Parse(ref current) is not Reference reference
                ? null
                : new Unresolved { Reference = reference };
    }

    internal class Overloaded : Algebra
    {
        public List<Resolution> Overloads { get; init; }
    }

    internal class Calculated<T> : Algebra
    {
        public T Member { get; init; }
    }
}