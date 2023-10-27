// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

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

    public override void ResolveTypes(IContext context)
    {
        base.ResolveTypes(context);
        if (Algebra is Algebra.Unresolved unresolved)
        {
            var resolution = context.Resolve(unresolved.Reference);
            if (resolution is Resolution.Definite definite)
            {
                Algebra = definite.Member as Algebra ?? new Algebra.Calculated { Member = definite.Member };
            }
            else if (resolution is Resolution.Ambiguous ambiguous)
            {
                Algebra = new Algebra.Overloaded { Candidates = ambiguous.Candidates };
            }
        }
        Members.ResolveTypes(context);
    }

    public override void ResolveFunctions(IContext context)
    {
        base.ResolveFunctions(context);
        Algebra.ResolveFunctions(context);
        Members.ResolveFunctions(context);
    }

    public override void ResolveData(IContext context)
    {
        base.ResolveData(context);
        Algebra.ResolveData(context);
        Members.ResolveData(context);
    }

    public class Definition : Aggregate<Definition, Open.Brace, Member, Terminal, Close.Brace>
    {
        public override void ResolveTypes(IContext context)
        {
            for (int i = 0; i != Count; ++i)
            {
                this[i].ResolveTypes(context);
            }
        }

        public override void ResolveFunctions(IContext context)
        {
            for (int i = 0; i != Count; ++i)
            {
                this[i].ResolveFunctions(context);
            }
        }

        public override void ResolveData(IContext context)
        {
            for (int i = 0; i != Count; ++i)
            {
                this[i].ResolveData(context);
            }
        }
    }

    public class Unresolved : Type
    {
        public Reference Reference { get; init; }

        public static new Type Parse(ref Parser current)
            => Reference.Parse(ref current) is not Reference reference 
                ? null
                : new Unresolved { Reference = reference };
    }

    public class Overloaded : Type
    {
        public List<Resolution> Candidates { get; init; }
    }

    [ExcludeFromCodeCoverage]
    internal class Calculated : Type
    {
        public Member Member { get; init; }

        public class CircularityError : Calculated, IError
        {
            public Stack<Statement> Statements { get; init; }
            public string Reason { get; } = "calculated type depends on itself";
            public System.ReadOnlyMemory<Token> Tokens { get; init; }
        }
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
        public Member Member { get; init; }
    }
}

internal class Calculation : Statement
{
    public Member Member { get; init; }
    public Type Owner { get; init; }
}